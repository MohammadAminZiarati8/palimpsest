using System;
using Game.Shared.Content;

namespace Game.Shared.Enemies
{
    /// <summary>
    /// Enemy stats. Decimal math + one rounding (half away from zero) per stat,
    /// so server and IL2CPP client always agree (A1).
    /// </summary>
    public static class EnemyStatCalculator
    {
        /// <summary>
        /// BAL-16 tier value. HP/Atk/Def x (1 + (level-1) x growth) (BAL-17, linear like BAL-8, D-81),
        /// then Elite HP/Atk multipliers. Speed doesn't grow. Crit = file default or per-enemy override (D-76).
        /// Accuracy 0 in the demo.
        /// </summary>
        public static StatSet<int> At(ContentDb db, EnemyDef enemy, int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level), level, "Level must be >= 1");
            var rules = db.EnemyRules;
            decimal growth = 1m + (level - 1) * rules.GrowthPercent / 100m;

            var result = new StatSet<int>();
            foreach (var stat in Stats.EnemyTiered)
            {
                decimal value = rules.Tiers[stat].Get(enemy.Shape[stat]);
                if (Stats.IsCore(stat)) value *= growth;
                if (enemy.Elite && stat == Stat.Hp) value = value * rules.EliteHpPercent / 100m;
                if (enemy.Elite && stat == Stat.Attack) value = value * rules.EliteAttackPercent / 100m;
                result[stat] = Round(value);
            }
            result.CritChance = enemy.CritChance ?? rules.CritChance;
            result.CritDamage = enemy.CritDamage ?? rules.CritDamage;
            return result;
        }

        private static int Round(decimal value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
