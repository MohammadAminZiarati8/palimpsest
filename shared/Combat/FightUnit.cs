using System;
using System.Collections.Generic;
using System.Linq;
using Game.Shared.Content;

namespace Game.Shared.Combat
{
    public enum Side { Heroes, Enemies }

    /// <summary>
    /// CMB-17/18 where a unit stands. Main = own line (Main Door). Behind = behind the foe's Back
    /// (Margin for heroes, Bleed-through for enemies), facing the foes. Gutter = right behind the foe's
    /// Front, no safe side (D-103). A flanker turns Main once no foe stands between it and its side (D-104).
    /// </summary>
    public enum Spot { Main, Behind, Gutter }

    /// <summary>One status effect on a unit (CMB-40). Same type never stacks (BAL-15).</summary>
    public sealed class ActiveEffect
    {
        public StatusEffect Type { get; internal set; }
        /// <summary>Own turns left; ticks at the end of the unit's turn (BAL-15).</summary>
        public int Rounds { get; internal set; }
        /// <summary>Bleed: damage per tick (snapshot of applier Attack × bleed%). Ward: HP left to absorb.</summary>
        public int Amount { get; internal set; }
        /// <summary>Seal: the locked skill.</summary>
        public SkillSlot Slot { get; internal set; }
        /// <summary>Fight turn it was (re)applied in; newest first for cleanse (CMB-43).</summary>
        public int AppliedTurn { get; internal set; }
        internal long Sequence { get; set; }

        public bool IsDebuff => IsDebuffType(Type);

        public static bool IsDebuffType(StatusEffect t) =>
            t == StatusEffect.Stun || t == StatusEffect.Bleed || t == StatusEffect.Seal ||
            t == StatusEffect.AttackDown || t == StatusEffect.DefenseDown ||
            t == StatusEffect.SpeedDown || t == StatusEffect.AccuracyDown;
    }

    /// <summary>One champion or enemy in a fight. Position is owned by <see cref="Fight"/>.</summary>
    public sealed class FightUnit
    {
        private readonly List<ActiveEffect> _effects = new List<ActiveEffect>();
        private readonly Dictionary<SkillSlot, int> _cooldowns = new Dictionary<SkillSlot, int>();
        private readonly EffectStrengths _strength;

        /// <summary>Unique within the fight, in setup order (heroes first).</summary>
        public int Id { get; }
        public Side Side { get; }
        /// <summary>Champion or enemy content id.</summary>
        public string DefId { get; }
        public string Name { get; }
        /// <summary>Base stats from StatCalculator / EnemyStatCalculator. Effects apply on top (see Attack etc.).</summary>
        public StatSet<int> Stats { get; }
        public IReadOnlyList<SkillDef> Skills { get; }
        public int MaxHp { get; }
        public int Hp { get; internal set; }
        public bool IsDown => Hp <= 0;
        /// <summary>
        /// CMB-11: index in <see cref="Fight.Line"/>: 0 = Front (next to the centre), counting back; flankers
        /// after the Main units. -1 once Down (slot freed, CMB-14).
        /// </summary>
        public int Line { get; internal set; }
        /// <summary>Index in the whole row, left (our Main Door end) to right; -1 when off the field.</summary>
        public int Pos { get; internal set; } = -1;
        public Spot Spot { get; internal set; }
        /// <summary>+1 = faces right, −1 = left. Main heroes face right, Main enemies left; Behind turns round.</summary>
        public int Facing => (Side == Side.Heroes ? 1 : -1) * (Spot == Spot.Behind ? -1 : 1);
        public IReadOnlyList<ActiveEffect> Effects => _effects;
        public int Level { get; internal set; } = 1;
        public int Rank { get; internal set; } = 1;
        /// <summary>Herald: own Ring uses left until it rings (0 = rung or no Ring, ENM-9).</summary>
        public int RingIn { get; internal set; }

        /// <summary>Pell S3 Fade: own turns left untargetable by Single skills (ticks like effects, D-84).</summary>
        public int FadeRounds { get; private set; }
        private int _fadeTurn;
        public bool IsFaded => FadeRounds > 0;
        /// <summary>Fade: the next damaging skill crits on every hit, then this clears.</summary>
        public bool SureCrit { get; internal set; }

        // CMB-40 / balancing.md effect strengths. Up and Down are separate effects and can both be on.
        public int Attack => Percent(Stats.Attack, StatusEffect.AttackUp, StatusEffect.AttackDown, _strength.AttackPercent);
        public int Defense => Percent(Stats.Defense, StatusEffect.DefenseUp, StatusEffect.DefenseDown, _strength.DefensePercent);
        /// <summary>BAL-3: Speed only sets turn order (read at round start).</summary>
        public int Speed => Stats.Speed + Flat(StatusEffect.SpeedUp, StatusEffect.SpeedDown, _strength.Speed);
        public int Accuracy => Stats.Accuracy + Flat(StatusEffect.AccuracyUp, StatusEffect.AccuracyDown, _strength.Accuracy);

        internal FightUnit(int id, Side side, string defId, string name, StatSet<int> stats,
            IReadOnlyList<SkillDef> skills, EffectStrengths strength)
        {
            Id = id;
            Side = side;
            DefId = defId;
            Name = name;
            Stats = stats;
            Skills = skills;
            MaxHp = stats.Hp;
            Hp = stats.Hp;
            _strength = strength;
        }

        public bool Has(StatusEffect type) => _effects.Any(e => e.Type == type);
        public ActiveEffect? Effect(StatusEffect type) => _effects.FirstOrDefault(e => e.Type == type);

        public SkillDef Skill(SkillSlot slot) => Skills.First(s => s.Slot == slot);

        /// <summary>Own turns until the skill is ready; 0 = ready (CMB-42).</summary>
        public int Cooldown(SkillSlot slot) => _cooldowns.TryGetValue(slot, out var n) ? n : 0;

        /// <summary>Ready (CMB-42) and not Sealed (CMB-40).</summary>
        public bool CanUse(SkillDef skill) =>
            Cooldown(skill.Slot) == 0 && !_effects.Any(e => e.Type == StatusEffect.Seal && e.Slot == skill.Slot)
            && !(skill.Special?.Type == SpecialType.Ring && RingIn <= 0); // D-97: rings once

        internal void StartCooldown(SkillDef skill)
        {
            if (skill.Cooldown > 0) _cooldowns[skill.Slot] = skill.Cooldown;
        }

        /// <summary>BAL-15: same type replaces the old one (refreshes duration and size).</summary>
        internal ActiveEffect Apply(StatusEffect type, int rounds, int amount, SkillSlot slot, int turn, long sequence)
        {
            var e = Effect(type);
            if (e == null) { e = new ActiveEffect { Type = type }; _effects.Add(e); }
            e.Rounds = rounds;
            e.Amount = amount;
            e.Slot = slot;
            e.AppliedTurn = turn;
            e.Sequence = sequence;
            return e;
        }

        internal void Fade(int rounds, int turn)
        {
            FadeRounds = rounds;
            _fadeTurn = turn;
            SureCrit = true;
        }

        internal void Remove(ActiveEffect e) => _effects.Remove(e);

        /// <summary>Newest debuff first (CMB-43 cleanse).</summary>
        internal ActiveEffect? NewestDebuff() =>
            _effects.Where(e => e.IsDebuff).OrderByDescending(e => e.Sequence).FirstOrDefault();

        /// <summary>
        /// End of own turn: cooldowns −1 (CMB-42), effect durations −1 (BAL-15), except effects
        /// applied during this same turn (D-84: a self effect lasts through the unit's next turn).
        /// </summary>
        internal void EndTurn(int turn)
        {
            foreach (var slot in _cooldowns.Keys.ToList())
                if (_cooldowns[slot] > 0) _cooldowns[slot]--;
            if (FadeRounds > 0 && _fadeTurn != turn) FadeRounds--;
            foreach (var e in _effects.ToList())
            {
                if (e.AppliedTurn == turn) continue;
                if (--e.Rounds <= 0) _effects.Remove(e);
            }
        }

        private int Percent(int value, StatusEffect up, StatusEffect down, int percent)
        {
            int p = 100 + (Has(up) ? percent : 0) - (Has(down) ? percent : 0);
            return (int)Math.Round(value * p / 100m, MidpointRounding.AwayFromZero);
        }

        private int Flat(StatusEffect up, StatusEffect down, int amount) =>
            (Has(up) ? amount : 0) - (Has(down) ? amount : 0);

        public override string ToString() => $"{Name}#{Id}({Side} L{Line} {Hp}/{MaxHp})";
    }
}
