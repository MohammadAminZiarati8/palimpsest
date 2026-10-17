using System;
using Game.Shared.Common;
using Game.Shared.Content;

namespace Game.Shared.Combat
{
    /// <summary>
    /// BAL-2 damage. Decimal math, one rounding (half away from zero, min 1, D-79) so server
    /// and IL2CPP client agree (A1). All randomness comes from the fight's SeededRandom (A3).
    /// </summary>
    public static class DamageCalc
    {
        /// <summary>Spread roll range: ± spreadPercent in 1/100 of a percent (uniform).</summary>
        private const int SpreadSteps = 100;

        /// <summary>BAL-2: Atk × skill% × K/(K+Def) × (crit ? 1 + CritDmg : 1) × (1 + spread).</summary>
        /// <param name="spreadBasisPoints">-spread×100 .. +spread×100 (e.g. -500..500 for ±5%).</param>
        public static int Compute(CombatRules rules, int attack, int skillPercent, int defense,
            bool crit, int critDamagePercent, int spreadBasisPoints)
        {
            decimal k = rules.DefenseK;
            decimal value = attack * skillPercent / 100m * k / (k + Math.Max(0, defense));
            if (crit) value = value * (100 + critDamagePercent) / 100m;
            value = value * (10_000 + spreadBasisPoints) / 10_000m;
            return Math.Max(1, (int)Math.Round(value, MidpointRounding.AwayFromZero));
        }

        /// <summary>Crit chance + skill bonus + Exposed (BAL-20, additive), capped (critCapPercent).</summary>
        public static int CritChance(CombatRules rules, int critChance, int skillBonus, bool exposed) =>
            Math.Min(rules.CritCapPercent, critChance + skillBonus + (exposed ? rules.ExposedCritPercent : 0));

        /// <summary>Rolls crit, then spread, then computes one hit (fixed roll order keeps replays equal).</summary>
        public static (int Amount, bool Crit) Roll(SeededRandom rng, CombatRules rules,
            FightUnit attacker, SkillDef skill, FightUnit target, bool exposed, int extraCrit = 0)
        {
            int chance = CritChance(rules, attacker.Stats.CritChance, skill.CritChanceBonus + extraCrit, exposed);
            bool crit = rng.RollPercent(chance) | attacker.SureCrit; // always roll: same RNG use either way
            int range = rules.SpreadPercent * SpreadSteps;
            int spread = rng.NextInt(-range, range + 1);
            int amount = Compute(rules, attacker.Attack, skill.DamagePercent, target.Defense,
                crit, attacker.Stats.CritDamage, spread);
            return (amount, crit);
        }
    }
}
