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
        /// <summary>HP actually lost after Ward (Amount − absorbed, capped by HP left).</summary>
        public int HpLost { get; internal set; }
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
        private readonly Dictionary<FightUnit, Intent> _intents = new Dictionary<FightUnit, Intent>();
        private int _turn;
        private long _sequence;

        public ContentDb Db { get; }
        /// <summary>The only RNG the fight may use (A3).</summary>
        public SeededRandom Random { get; }
        public int RoundCap { get; }
        /// <summary>Turns started so far, across rounds (Stun-skipped turns count).</summary>
        public int Turn => _turn;
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

        /// <summary>Enemy intent for this round (CMB-26, CMB-41), fixed at round start. Null = none.</summary>
        public Intent? IntentOf(FightUnit unit) => _intents.TryGetValue(unit, out var i) ? i : null;

        /// <summary>Units on the field for a side, Front first.</summary>
        public IReadOnlyList<FightUnit> Line(Side side) => _lines[side];

        /// <summary>
        /// Plays one turn: starts a new round when the current one is used up (CMB-43 order rebuild),
        /// then lets the next unit act. Returns false once the fight is over.
        /// </summary>
        public bool Step(ActHook act)
        {
            var actor = Next();
            if (actor == null) return false;
            _queue.Dequeue();
            TakeTurn(actor, act);
            return !IsOver;
        }

        /// <summary>
        /// Who acts next, without taking the turn (UI waits for player input here). Starts the next
        /// round (order + intents) if the current one is used up; null once the fight is over.
        /// </summary>
        public FightUnit? Next()
        {
            if (IsOver) return null;
            while (true)
            {
                while (_queue.Count > 0 && _queue.Peek().IsDown) _queue.Dequeue();
                if (_queue.Count > 0) return _queue.Peek();
                // CMB-43: still going after the last allowed round = loss.
                if (Round >= RoundCap) { End(FightOutcome.Lost, LossReason.RoundCap); return null; }
                StartRound();
            }
        }

        /// <summary>
        /// One unit's turn: Bleed ticks first (Ward absorbs, CMB-43), Stun skips the action (CMB-40),
        /// then cooldowns and durations tick at the end of the turn (CMB-42, BAL-15).
        /// </summary>
        private void TakeTurn(FightUnit actor, ActHook act)
        {
            _turn++;
            var bleed = actor.Effect(StatusEffect.Bleed);
            if (bleed != null) Damage(actor, bleed.Amount);
            if (IsOver || actor.IsDown) return;
            if (!actor.Has(StatusEffect.Stun)) act(this, actor);
            if (!IsOver && !actor.IsDown) actor.EndTurn(_turn);
        }

        /// <summary>Plays turns until win or loss.</summary>
        public FightOutcome Run(ActHook act)
        {
            while (Step(act)) { }
            return Outcome;
        }

        /// <summary>
        /// Uses any skill: damaging (Reach Melee/Ranged) via <see cref="Attack"/>, else a support skill
        /// on own side (heal, cleanse, Ward, effects, Fade). <paramref name="primary"/> must be in
        /// <see cref="Targeting.Valid"/> (for Self / AllAllies any valid unit, e.g. the caster).
        /// </summary>
        public IReadOnlyList<Hit> UseSkill(FightUnit actor, SkillDef skill, FightUnit primary)
        {
            if (skill.Reach == Reach.Melee || skill.Reach == Reach.Ranged) return Attack(actor, skill, primary);
            Check(actor, skill, primary);

            var targets = Targeting.Hits(this, skill, primary);
            var fx = Db.Combat.Effects;
            foreach (var t in targets)
            {
                if (skill.HealPercent > 0)
                    Heal(t, Pct((skill.HealOf == HealBasis.Target ? t : actor).MaxHp, skill.HealPercent));
                if (skill.Cleanse > 0) Cleanse(t, skill.Cleanse);
                if (skill.WardPercent > 0)
                    ApplyEffect(t, StatusEffect.Ward, fx.WardRounds, actor, Pct(actor.MaxHp, skill.WardPercent));
            }
            ApplySkillEffects(actor, skill, targets);
            if (skill.Special?.Type == SpecialType.Fade) actor.Fade(skill.Special.Rounds, _turn);
            actor.StartCooldown(skill);
            return new List<Hit>();
        }

        /// <summary>Restores HP up to max. Returns HP gained. Down units can't be healed.</summary>
        public int Heal(FightUnit target, int amount)
        {
            if (target.IsDown || amount <= 0) return 0;
            int gained = Math.Min(amount, target.MaxHp - target.Hp);
            target.Hp += gained;
            return gained;
        }

        /// <summary>
        /// CMB-16 Shift: a champion swaps with an adjacent ally (costs its turn: call it as the turn's action).
        /// </summary>
        public void Shift(FightUnit actor, FightUnit ally)
        {
            if (IsOver) throw new InvalidOperationException("Fight is over");
            if (actor.Side != Side.Heroes) throw new InvalidOperationException("Only champions Shift");
            var line = _lines[actor.Side];
            if (ally == actor || ally.Side != actor.Side || ally.IsDown || Math.Abs(ally.Line - actor.Line) != 1)
                throw new ArgumentException($"{ally} is not next to {actor}");
            int a = actor.Line, b = ally.Line;
            line[a] = ally;
            line[b] = actor;
            Reindex(line);
        }

        /// <summary>
        /// Uses a damaging skill on <paramref name="primary"/> (must be in <see cref="Targeting.Valid"/>).
        /// Each unit hit rolls its own crit and spread (BAL-2), in hit order. Then skill effects land on the units hit (or Self) and the cooldown starts (CMB-42).
        /// </summary>
        public IReadOnlyList<Hit> Attack(FightUnit actor, SkillDef skill, FightUnit primary)
        {
            Check(actor, skill, primary);

            var hits = new List<Hit>();
            foreach (var target in Targeting.Hits(this, skill, primary))
            {
                var (amount, crit) = DamageCalc.Roll(Random, Db.Combat, actor, skill, target,
                    Targeting.IsExposed(actor, target));
                hits.Add(new Hit(target, amount, crit));
            }
            // Apply after all rolls so a Down primary doesn't shift who Splash hits.
            foreach (var h in hits) h.HpLost = Damage(h.Target, h.Amount);
            if (!IsOver) ApplySkillEffects(actor, skill, hits.Select(h => h.Target));
            actor.SureCrit = false; // Fade's sure crit is spent on this attack
            actor.StartCooldown(skill);
            return hits;
        }

        private void Check(FightUnit actor, SkillDef skill, FightUnit primary)
        {
            if (IsOver) throw new InvalidOperationException("Fight is over");
            if (actor.IsDown) throw new InvalidOperationException($"{actor} is Down");
            if (!actor.CanUse(skill)) throw new InvalidOperationException($"{skill.Name} is on cooldown or Sealed");
            if (!Targeting.Valid(this, actor, skill).Contains(primary))
                throw new ArgumentException($"{primary} is not a valid target for {skill.Name}");
        }

        private void ApplySkillEffects(FightUnit actor, SkillDef skill, IEnumerable<FightUnit> targets)
        {
            foreach (var fx in skill.Effects)
                foreach (var u in fx.On == EffectOn.Self ? new[] { actor } : targets)
                    if (!u.IsDown) ApplyEffect(u, fx.Type, fx.Rounds, actor);
        }

        private static int Pct(int value, int percent) =>
            (int)Math.Round(value * percent / 100m, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Puts a CMB-40 effect on <paramref name="target"/> (BAL-15: same type refreshes).
        /// Bleed size = source Attack × bleedAttackPercent, fixed at apply time. Ward needs <paramref name="amount"/>.
        /// </summary>
        public ActiveEffect ApplyEffect(FightUnit target, StatusEffect type, int rounds, FightUnit source,
            int amount = 0, SkillSlot sealedSlot = SkillSlot.Unset)
        {
            if (target.IsDown) throw new InvalidOperationException($"{target} is Down");
            if (rounds < 1) throw new ArgumentOutOfRangeException(nameof(rounds));
            if (type == StatusEffect.Bleed)
                amount = Math.Max(1, (int)Math.Round(source.Attack * Db.Combat.Effects.BleedAttackPercent / 100m,
                    MidpointRounding.AwayFromZero));
            if (type == StatusEffect.Seal && sealedSlot != SkillSlot.S2 && sealedSlot != SkillSlot.S3)
                throw new ArgumentException("Seal locks S2 or S3", nameof(sealedSlot));
            return target.Apply(type, rounds, amount, sealedSlot, _turn, ++_sequence);
        }

        /// <summary>Removes up to <paramref name="count"/> debuffs, newest first (CMB-43). Returns how many.</summary>
        public int Cleanse(FightUnit target, int count)
        {
            int removed = 0;
            for (; removed < count; removed++)
            {
                var e = target.NewestDebuff();
                if (e == null) break;
                target.Remove(e);
            }
            return removed;
        }

        /// <summary>
        /// Removes HP (used by <see cref="Attack"/>, later Bleed). At 0 the unit is Down: its slot empties and
        /// the units behind it close the gap toward the centre (CMB-14, CMB-43).
        /// </summary>
        /// <returns>HP actually lost.</returns>
        public int Damage(FightUnit target, int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (target.IsDown || IsOver) return 0;
            var ward = target.Effect(StatusEffect.Ward);
            if (ward != null)
            {
                int absorbed = Math.Min(ward.Amount, amount);
                ward.Amount -= absorbed;
                amount -= absorbed;
                if (ward.Amount == 0) target.Remove(ward); // broken (balancing.md Ward)
            }
            int lost = Math.Min(target.Hp, amount);
            target.Hp -= lost;
            if (!target.IsDown) return lost;

            var line = _lines[target.Side];
            line.Remove(target);
            target.Line = -1;
            Reindex(line);

            if (_lines[Side.Enemies].Count == 0) End(FightOutcome.Won, LossReason.None);
            else if (_lines[Side.Heroes].Count == 0) End(FightOutcome.Lost, LossReason.HeroesDown);
            return lost;
        }

        private void StartRound()
        {
            Round++;
            _queue.Clear();
            foreach (var u in _order.Build(_lines[Side.Heroes].Concat(_lines[Side.Enemies])))
                _queue.Enqueue(u);
            _intents.Clear();
            foreach (var e in _lines[Side.Enemies])
            {
                var plan = EnemyAi.Plan(this, e);
                if (plan != null) _intents[e] = plan;
            }
        }

        private void Add(Side side, string id, string name, StatSet<int> stats, IReadOnlyList<SkillDef> skills)
        {
            var unit = new FightUnit(_units.Count, side, id, name, stats, skills, Db.Combat.Effects);
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
