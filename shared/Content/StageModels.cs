using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Shared.Content
{
    // JSON shapes of stage content (M2b-T1, D-95). Schema reference: docs/tech/data.md.

    /// <summary>CMB-5: Reading = unaware (stealth options open), Watching = alert.</summary>
    public enum Alert { Unset, Reading, Watching }

    /// <summary>content/stages.json: campaign stages in play order.</summary>
    public sealed class StageFile
    {
        [JsonProperty("_doc")] public string? Doc { get; set; }
        /// <summary>BAL-21: HP % of max a hero heals per scene waited at the door (CMB-9).</summary>
        public int DoorHealPercent { get; set; }
        /// <summary>BAL-22 room option %: base ± per level vs scene, class bonus, clamp.</summary>
        public int RoomBasePercent { get; set; }
        public int RoomPerLevelPercent { get; set; }
        public int RoomClassBonusPercent { get; set; }
        public int RoomMinPercent { get; set; }
        public int RoomMaxPercent { get; set; }
        /// <summary>BAL-23.</summary>
        public int MarginStrikeCritPercent { get; set; }
        /// <summary>CMB-23 Notice added per scene fought (Clash or failed stealth, D-108) and per Herald ring.</summary>
        public int NoticePerFight { get; set; }
        public int NoticePerRing { get; set; }
        /// <summary>D-106 Notice (CMB-23): at this Notice, scenes that would start Reading start Watching.</summary>
        public int NoticeWatchingAt { get; set; }
        /// <summary>D-106: at this Notice, every later scene gets <see cref="NoticePatrol"/> as an extra arrival.</summary>
        public int NoticePatrolAt { get; set; }
        public Arrival NoticePatrol { get; set; } = new Arrival();
        public List<StageDef> Stages { get; set; } = new List<StageDef>();
    }

    public sealed class StageDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Chapter { get; set; } = "";
        /// <summary>BAL-19: all enemies in the stage use this level.</summary>
        public int EnemyLevel { get; set; }
        /// <summary>Champions the story allows here (STY: Prologue = 2 starters).</summary>
        public List<string> Heroes { get; set; } = new List<string>();
        /// <summary>CMB-3: played in order.</summary>
        public List<SceneDef> Scenes { get; set; } = new List<SceneDef>();
    }

    public sealed class SceneDef
    {
        /// <summary>CMB-8: 1–3 champions may enter.</summary>
        public int Capacity { get; set; }
        public Alert Alert { get; set; }
        /// <summary>CMB-17: Margin entry possible.</summary>
        public bool MarginPath { get; set; }
        /// <summary>Front first (CMB-11).</summary>
        public List<string> Enemies { get; set; } = new List<string>();
        /// <summary>CMB-22 nearby enemies: join the end of their line after N rounds.</summary>
        public List<Arrival> Arrivals { get; set; } = new List<Arrival>();
        /// <summary>ENM-9: enemies a Herald's Ring writes in. Required if the scene has a Herald.</summary>
        public List<string> Reinforcements { get; set; } = new List<string>();
    }

    public sealed class Arrival
    {
        public string Enemy { get; set; } = "";
        public int InRounds { get; set; }
    }
}
