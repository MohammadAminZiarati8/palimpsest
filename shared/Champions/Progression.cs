using System;
using Game.Shared.Content;

namespace Game.Shared.Champions
{
    /// <summary>Level and rank limits and XP (CHM-7, BAL-10, BAL-11).</summary>
    public static class Progression
    {
        /// <summary>CHM-7: each rank adds levelsPerRank (Rank 1 = max 10).</summary>
        public static int MaxLevel(ContentDb db, int rank) => rank * db.Stats.LevelsPerRank;

        /// <summary>BAL-11: XP to next level = xpPerLevel x current level.</summary>
        public static int XpToNextLevel(ContentDb db, int level) => db.Stats.XpPerLevel * level;

        /// <summary>Total XP from Lv1 to reach <paramref name="level"/>.</summary>
        public static int TotalXpToReach(ContentDb db, int level) =>
            db.Stats.XpPerLevel * (level - 1) * level / 2;

        /// <summary>Throws if rank or level is outside the current caps (BAL-10).</summary>
        public static void Validate(ContentDb db, int level, int rank)
        {
            if (rank < 1 || rank > db.Stats.MaxRank)
                throw new ArgumentOutOfRangeException(nameof(rank), rank, $"Rank must be 1..{db.Stats.MaxRank}");
            int max = MaxLevel(db, rank);
            if (level < 1 || level > max)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"Level must be 1..{max} at rank {rank}");
        }
    }
}
