using System;
using System.Collections.Generic;
using System.Linq;
using Game.Shared.Champions;
using Game.Shared.Common;
using Game.Shared.Content;
using Game.Shared.Enemies;

namespace Game.Shared.Combat
{
    public sealed class HeroEntry
    {
        public string ChampionId { get; set; } = "";
        public int Level { get; set; } = 1;
        public int Rank { get; set; } = 1;
    }

    public sealed class EnemyEntry
    {
        public string EnemyId { get; set; } = "";
        public int Level { get; set; } = 1;
    }

    /// <summary>One scene's fight. Lists are in line order, Front first (CMB-11).</summary>
    public sealed class FightSetup
    {
        public List<HeroEntry> Heroes { get; set; } = new List<HeroEntry>();
        public List<EnemyEntry> Enemies { get; set; } = new List<EnemyEntry>();
        /// <summary>Battle seed from the server (A3).</summary>
        public ulong Seed { get; set; }
    }

    public enum FightOutcome { Ongoing, Won, Lost }
    public enum LossReason { None, HeroesDown, RoundCap }

    public sealed class Hit
    {
        public FightUnit Target { get; }
        public int Amount { get; }
        public bool Crit { get; }
        public Hit(FightUnit target, int amount, bool crit) { Target = target; Amount = amount; Crit = crit; }
    }

    /// <summary>Called once per turn with the acting unit. Real skills/AI arrive in T3..T6.</summary>
    public delegate void ActHook(Fight fight, FightUnit actor);

    /// <summary>
    /// Fight core (M2a-T2): row of slots, rounds in turn order, Down + gap close, win/lose, round cap.
    /// Pure and deterministic: same content + setup + inputs = same result (A3).
    /// </summary>
    public sealed class Fight
    {
        /// <summary>CMB-11 default row: Front, Middle, Back per side. Bosses taking 2 slots come later.</summary>
        public const int MaxSlots = 3;

        private readonly Dictionary<Side, List<FightUnit>> _lines = new Dictionary<Side, List<FightUnit>>
        {
            [Side.Heroes] = new List<FightUnit>(),
            [Side.Enemies] = new List<FightUnit>(),
        };
        private readonly List<FightUnit> _units = new List<FightUnit>();
        private readonly Queue<FightUnit> _queue = new Queue<FightUnit>();
        private readonly ITurnOrder _order;

        public ContentDb Db { get; }
        /// <summary>The only RNG the fight may use (A3).</summary>
        public SeededRandom Random { get; }
        public int RoundCap { get; }
        /// <summary>0 before the first round starts.</summary>
        public int Round { get; private set; }
        public FightOutcome Outcome { get; private set; }
        public LossReason LossReason { get; private set; }
        public bool IsOver => Outcome != FightOutcome.Ongoing;
        /// <summary>Everyone who started the fight, Down or not.</summary>
        public IReadOnlyList<FightUnit> Units => _units;
        /// <summary>Units still to act this round, in order (Down units are skipped when reached).</summary>
        public IEnumerable<FightUnit> Pending => _queue.Where(u => !u.IsDown);

        public Fight(ContentDb db, FightSetup setup, ITurnOrder? order = null)
        {
            Db = db;
            Random = new SeededRandom(setup.Seed);
            RoundCap = db.Combat.RoundCap;
            _order = order ?? SpeedTurnOrder.Instance;
            CheckSize(setup.Heroes.Count, "heroes");
            CheckSize(setup.Enemies.Count, "enemies");

            foreach (var h in setup.Heroes)
            {
                var def = db.Champion(h.ChampionId);
                Add(Side.Heroes, def.Id, def.Name, StatCalculator.At(db, def, h.Level, h.Rank), db.Kit(def.Id).Skills);
            }
            foreach (var e in setup.Enemies)
            {
                var def = db.Enemy(e.EnemyId);
                Add(Side.Enemies, def.Id, def.Name, EnemyStatCalculator.At(db, def, e.Level), def.Skills);
            }
        }

        /// <summary>Units on the field for a side, Front first.</summary>
        public IReadOnlyList<FightUnit> Line(Side side) => _lines[side];

        /// <summary>
        /// Plays one turn: starts a new round when the current one is used up (CMB-43 order rebuild),
        /// then lets the next unit act. Returns false once the fight is over.
        /// </summary>
        public bool Step(ActHook act)
        {
            if (IsOver) return false;
            FightUnit? actor;
            while ((actor = NextActor()) == null)
            {
                // CMB-43: still going after the last allowed round = loss.
                if (Round >= RoundCap) { End(FightOutcome.Lost, LossReason.RoundCap); return false; }
                StartRound();
            }
            act(this, actor);
            return !IsOver;
        }

        /// <summary>Plays turns until win or loss.</summary>
        public FightOutcome Run(ActHook act)
        {
            while (Step(act)) { }
            return Outcome;
        }

        /// <summary>
        /// Uses a damaging skill on <paramref name="primary"/> (must be in <see cref="Targeting.Valid"/>).
        /// Each unit hit rolls its own crit and spread (BAL-2), in hit order. Cooldowns/effects: T4.
        /// </summary>
        public IReadOnlyList<Hit> Attack(FightUnit actor, SkillDef skill, FightUnit primary)
        {
            if (IsOver) throw new InvalidOperationException("Fight is over");
            if (actor.IsDown) throw new InvalidOperationException($"{actor} is Down");
            if (!Targeting.Valid(this, actor, skill).Contains(primary))
                throw new ArgumentException($"{primary} is not a valid target for {skill.Name}");

            var hits = new List<Hit>();
            foreach (var target in Targeting.Hits(this, skill, primary))
            {
                var (amount, crit) = DamageCalc.Roll(Random, Db.Combat, actor, skill, target,
                    Targeting.IsExposed(actor, target));
                hits.Add(new Hit(target, amount, crit));
            }
            // Apply after all rolls so a Down primary doesn't shift who Splash hits.
            foreach (var h in hits) Damage(h.Target, h.Amount);
            return hits;
        }

        /// <summary>
        /// Removes HP (used by <see cref="Attack"/>, later Bleed). At 0 the unit is Down: its slot empties and
        /// the units behind it close the gap toward the centre (CMB-14, CMB-43).
        /// </summary>
        public void Damage(FightUnit target, int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (target.IsDown || IsOver) return;
            target.Hp = Math.Max(0, target.Hp - amount);
            if (!target.IsDown) return;

            var line = _lines[target.Side];
            line.Remove(target);
            target.Line = -1;
            Reindex(line);

            if (_lines[Side.Enemies].Count == 0) End(FightOutcome.Won, LossReason.None);
            else if (_lines[Side.Heroes].Count == 0) End(FightOutcome.Lost, LossReason.HeroesDown);
        }

        private void StartRound()
        {
            Round++;
            _queue.Clear();
            foreach (var u in _order.Build(_lines[Side.Heroes].Concat(_lines[Side.Enemies])))
                _queue.Enqueue(u);
        }

        private FightUnit? NextActor()
        {
            while (_queue.Count > 0)
            {
                var u = _queue.Dequeue();
                if (!u.IsDown) return u;
            }
            return null;
        }

        private void Add(Side side, string id, string name, StatSet<int> stats, IReadOnlyList<SkillDef> skills)
        {
            var unit = new FightUnit(_units.Count, side, id, name, stats, skills);
            _units.Add(unit);
            var line = _lines[side];
            line.Add(unit);
            unit.Line = line.Count - 1;
        }

        private void End(FightOutcome outcome, LossReason reason)
        {
            Outcome = outcome;
            LossReason = reason;
            _queue.Clear();
        }

        private static void Reindex(List<FightUnit> line)
        {
            for (int i = 0; i < line.Count; i++) line[i].Line = i;
        }

        private static void CheckSize(int count, string what)
        {
            if (count < 1 || count > MaxSlots)
                throw new ArgumentException($"A fight needs 1..{MaxSlots} {what}, got {count}");
        }
    }
}
