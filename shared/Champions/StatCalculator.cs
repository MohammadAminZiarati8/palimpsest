using System;
using Game.Shared.Content;

namespace Game.Shared.Champions
{
    /// <summary>
    /// Champion stats. Decimal math + one rounding (half away from zero) per stat,
    /// so server and IL2CPP client always agree (A1).
    /// </summary>
    public static class StatCalculator
    {
        /// <summary>BAL-5 tier value x BAL-6 rarity (HP/Atk/Def only) x BAL-7 tweak.</summary>
        public static StatSet<int> Level1(ContentDb db, ChampionDef hero)
        {
            var result = new StatSet<int>();
            foreach (var stat in Stats.All)
            {
                decimal value = db.Stats.Tiers[stat].Get(hero.Shape[stat]);
                if (Stats.IsCore(stat))
                    value = value * db.Stats.RarityPercent[hero.Rarity] / 100m;
                value = value * (100 + hero.TweakPercent[stat]) / 100m;
                result[stat] = Round(value);
            }
            return result;
        }

        /// <summary>
        /// BAL-8 / CLS-3: core stat = Lv1 x (1 + (level-1) x class growth) x rankStatPercent^(rank-1).
        /// BAL-9 / CHM-8: other stats = Lv1 + class rank bonus per rank-up; they don't grow per level.
        /// </summary>
        public static StatSet<int> At(ContentDb db, ChampionDef hero, int level, int rank)
        {
            Progression.Validate(db, level, rank);
            var lv1 = Level1(db, hero);
            var cls = db.Class(hero.Class);

            decimal rankMul = 1m;
            for (int r = 1; r < rank; r++)
                rankMul = rankMul * db.Stats.RankStatPercent / 100m;

            var result = new StatSet<int>();
            foreach (var stat in Stats.All)
            {
                result[stat] = Stats.IsCore(stat)
                    ? Round(lv1[stat] * (1m + (level - 1) * cls.GrowthPercent[stat] / 100m) * rankMul)
                    : lv1[stat] + cls.RankBonus[stat] * (rank - 1);
            }
            return result;
        }

        private static int Round(decimal value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
