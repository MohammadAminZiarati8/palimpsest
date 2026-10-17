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
        /// <summary>CMB-33: HP carried from the last scene; null = full.</summary>
        public int? Hp { get; set; }
    }

    public sealed class EnemyEntry
    {
        public string EnemyId { get; set; } = "";
        public int Level { get; set; } = 1;
    }

    /// <summary>CMB-22 nearby enemy: joins the end of the enemy line after InRounds rounds.</summary>
    public sealed class ArrivalEntry
    {
        public string EnemyId { get; set; } = "";
        public int Level { get; set; } = 1;
        public int InRounds { get; set; }
    }

    /// <summary>One scene's fight. Lists are in line order, Front first (CMB-11).</summary>
    public sealed class FightSetup
    {
        public List<HeroEntry> Heroes { get; set; } = new List<HeroEntry>();
        public List<EnemyEntry> Enemies { get; set; } = new List<EnemyEntry>();
        /// <summary>CMB-10: champions waiting at the door who may Join.</summary>
        public List<HeroEntry> Bench { get; set; } = new List<HeroEntry>();
        /// <summary>CMB-8 scene capacity: most champions on the field at once (Join limit).</summary>
        public int HeroCapacity { get; set; } = Fight.MaxSlots;
        public List<ArrivalEntry> Arrivals { get; set; } = new List<ArrivalEntry>();
        /// <summary>ENM-9: what a Herald's Ring writes in.</summary>
        public List<EnemyEntry> Reinforcements { get; set; } = new List<EnemyEntry>();
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
        private readonly List<HeroEntry> _bench;
        private readonly int _heroCapacity;
        /// <summary>Enemies waiting to enter: (entry, last round before they may enter).</summary>
        private readonly List<(EnemyEntry Entry, int AfterRound)> _incoming = new List<(EnemyEntry, int)>();
        private readonly List<EnemyEntry> _reinforcements;
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

            if (setup.HeroCapacity < setup.Heroes.Count || setup.HeroCapacity > MaxSlots)
                throw new ArgumentException($"HeroCapacity {setup.HeroCapacity} must fit the heroes and be <= {MaxSlots}");
            _heroCapacity = setup.HeroCapacity;
            _bench = setup.Bench.ToList();
            _reinforcements = setup.Reinforcements.ToList();
            foreach (var a in setup.Arrivals)
            {
                if (a.InRounds < 1) throw new ArgumentException("Arrival inRounds must be >= 1");
                db.Enemy(a.EnemyId);
                _incoming.Add((new EnemyEntry { EnemyId = a.EnemyId, Level = a.Level }, a.InRounds));
            }

            foreach (var h in setup.Heroes) AddHero(h);
            foreach (var e in setup.Enemies) AddEnemy(e);
        }

        /// <summary>Champions still waiting at the door (CMB-10).</summary>
        public IReadOnlyList<HeroEntry> Bench => _bench;
        /// <summary>Enemies not yet on the field (arrivals + rung-in reinforcements).</summary>
        public int IncomingCount => _incoming.Count;
        /// <summary>Herald rings so far (Notice +2 each, CMB-23, counted by the stage).</summary>
        public int Rings { get; private set; }

        /// <summary>
        /// CMB-10 Join: a waiting champion enters at the Main Door end (our Back) if the scene's capacity
        /// allows. Free, between turns; it acts from the next round (CMB-43).
        /// </summary>
        public FightUnit Join(string championId)
        {
            if (IsOver) throw new InvalidOperationException("Fight is over");
            var entry = _bench.FirstOrDefault(b => b.ChampionId == championId)
                        ?? throw new ArgumentException($"{championId} is not waiting at the door");
            if (_lines[Side.Heroes].Count >= _heroCapacity)
                throw new InvalidOperationException($"Scene is full ({_heroCapacity}, CMB-8)");
            _bench.Remove(entry);
            return AddHero(entry);
        }

        /// <summary>
        /// CMB-10 Fall back: the champion leaves to the door, keeping its HP (costs its turn: use it as the
        /// turn's action). Units behind close the gap. It may Join again later.
        /// </summary>
        public void FallBack(FightUnit hero)
        {
            if (IsOver) throw new InvalidOperationException("Fight is over");
            if (hero.Side != Side.Heroes || hero.IsDown || hero.Line < 0)
                throw new ArgumentException($"{hero} can't fall back");
            var line = _lines[Side.Heroes];
            line.Remove(hero);
            hero.Line = -1;
            Reindex(line);
            _bench.Add(new HeroEntry { ChampionId = hero.DefId, Level = hero.Level, Rank = hero.Rank, Hp = hero.Hp });
            _fallenBack.Add(hero);
        }

        private readonly List<FightUnit> _fallenBack = new List<FightUnit>();
        /// <summary>Hero units that fell back (their HP is in the bench entry; for HP carry, CMB-33).</summary>
        public IReadOnlyList<FightUnit> FallenBack => _fallenBack;

        private FightUnit AddHero(HeroEntry h)
        {
            var def = Db.Champion(h.ChampionId);
            var unit = Add(Side.Heroes, def.Id, def.Name, StatCalculator.At(Db, def, h.Level, h.Rank), Db.Kit(def.Id).Skills);
            unit.Level = h.Level;
            unit.Rank = h.Rank;
            if (h.Hp is int hp)
            {
                if (hp < 1) throw new ArgumentException($"{def.Id} is Down and can't enter (CMB-34)");
                unit.Hp = Math.Min(hp, unit.MaxHp);
            }
            return unit;
        }

        private FightUnit AddEnemy(EnemyEntry e)
        {
            var def = Db.Enemy(e.EnemyId);
            var unit = Add(Side.Enemies, def.Id, def.Name, EnemyStatCalculator.At(Db, def, e.Level), def.Skills);
            unit.Level = e.Level;
            var ring = def.Skills.FirstOrDefault(s => s.Special?.Type == SpecialType.Ring);
            if (ring != null) unit.RingIn = ring.Special!.Rounds;
            return unit;
        }

        /// <summary>
        /// ENM-9 Herald Ring (D-97: once per fight). Each use counts down; at 0 the reinforcements are
        /// written in at the end of the enemy line from the next round.
        /// </summary>
        private void Ring(FightUnit herald)
        {
            if (herald.RingIn <= 0) return;
            if (--herald.RingIn > 0) return;
            Rings++;
            foreach (var r in _reinforcements) _incoming.Add((r, Round));
        }

        /// <summary>CMB-22: enemies due (after their rounds) enter while a slot is free (D-97).</summary>
        private void BringInArrivals()
        {
            var line = _lines[Side.Enemies];
            foreach (var a in _incoming.ToList())
            {
                if (Round <= a.AfterRound || line.Count >= MaxSlots) continue;
                _incoming.Remove(a);
                AddEnemy(a.Entry);
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
            if (skill.Special?.Type == SpecialType.Ring) Ring(actor);
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
            else if (_lines[Side.Heroes].Count == 0 && _bench.Count == 0) End(FightOutcome.Lost, LossReason.HeroesDown);
            return lost;
        }

        private void StartRound()
        {
            Round++;
            BringInArrivals();
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

        private FightUnit Add(Side side, string id, string name, StatSet<int> stats, IReadOnlyList<SkillDef> skills)
        {
            var unit = new FightUnit(_units.Count, side, id, name, stats, skills, Db.Combat.Effects);
            _units.Add(unit);
            var line = _lines[side];
            line.Add(unit);
            unit.Line = line.Count - 1;
            return unit;
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
