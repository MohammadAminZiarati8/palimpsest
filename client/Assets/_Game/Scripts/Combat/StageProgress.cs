#nullable enable
using System.Collections.Generic;
using System.Linq;
using Game.Shared.Combat;
using Game.Shared.Content;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// M2b-T7 placeholder progress (D-110): best stars per stage on this device (PlayerPrefs; stars never go
    /// down, CMP-2). A stage opens once the one before it is cleared; "Unlock all" is a test switch.
    /// Real progress moves to the server save later (server-authoritative rewards).
    /// </summary>
    public static class StageProgress
    {
        private const string StarsKey = "stage.stars.";
        private const string UnlockAllKey = "stage.unlockAll";

        public static int Stars(string stageId) => PlayerPrefs.GetInt(StarsKey + stageId, 0);

        /// <summary>Keeps the best result. Returns true if it improved.</summary>
        public static bool Record(string stageId, int stars)
        {
            if (stars <= Stars(stageId)) return false;
            PlayerPrefs.SetInt(StarsKey + stageId, stars);
            PlayerPrefs.Save();
            return true;
        }

        public static bool UnlockAll
        {
            get => PlayerPrefs.GetInt(UnlockAllKey, 0) == 1;
            set { PlayerPrefs.SetInt(UnlockAllKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool Unlocked(IReadOnlyList<StageDef> stages, int index) =>
            UnlockAll || index == 0 || Stars(stages[index - 1].Id) > 0;

        public static void Reset(IEnumerable<StageDef> stages)
        {
            foreach (var s in stages) PlayerPrefs.DeleteKey(StarsKey + s.Id);
            PlayerPrefs.DeleteKey(UnlockAllKey);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// D-111 placeholder team until player progress exists: every champion the stage allows, one level below
    /// the stage's enemy level (min 1, BAL-19), rank 2 from Seedbed 4 (BAL-13) — same rule as the D-88 test fights.
    /// </summary>
    public static class DemoTeam
    {
        private const string RankTwoFrom = "S4";

        public static List<HeroEntry> For(ContentDb db, StageDef stage)
        {
            var ids = db.Stages.Select(s => s.Id).ToList();
            int from = ids.IndexOf(RankTwoFrom);
            int rank = from >= 0 && ids.IndexOf(stage.Id) >= from ? 2 : 1;
            int level = Mathf.Max(1, stage.EnemyLevel - 1);
            return stage.Heroes.Take(Fight.MaxSlots)
                .Select(id => new HeroEntry { ChampionId = id, Level = level, Rank = rank }).ToList();
        }
    }
}
