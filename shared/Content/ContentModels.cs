using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Shared.Content
{
    // JSON shapes of content/*.json. Schema reference: docs/tech/data.md.

    /// <summary>content/version.json (A8).</summary>
    public sealed class VersionFile
    {
        public int ContentVersion { get; set; }
    }

    /// <summary>content/stats.json: BAL-5, 6, 9, 10, 11.</summary>
    public sealed class StatRules
    {
        [JsonProperty("_doc")] public string? Doc { get; set; }
        public StatSet<TierValues> Tiers { get; set; } = new StatSet<TierValues>();
        public Dictionary<Rarity, int> RarityPercent { get; set; } = new Dictionary<Rarity, int>();
        public int RankStatPercent { get; set; }
        public int LevelsPerRank { get; set; }
        public int MaxRank { get; set; }
        public int XpPerLevel { get; set; }
        public int MaxTweakPercent { get; set; }
        public List<RankUpCost> RankUps { get; set; } = new List<RankUpCost>();
    }

    public sealed class TierValues
    {
        public int Low { get; set; }
        public int Medium { get; set; }
        public int High { get; set; }

        public int Get(StatTier tier) => tier switch
        {
            StatTier.Low => Low,
            StatTier.Medium => Medium,
            StatTier.High => High,
            _ => throw new ContentException($"Stat tier '{tier}' has no value"),
        };
    }

    /// <summary>BAL-9, CHM-6: class badges + gold.</summary>
    public sealed class RankUpCost
    {
        public int ToRank { get; set; }
        public int Badges { get; set; }
        public int Gold { get; set; }
    }

    /// <summary>content/classes.json: BAL-8, BAL-9, CLS-3, CLS-4.</summary>
    public sealed class ClassFile
    {
        [JsonProperty("_doc")] public string? Doc { get; set; }
        public List<ClassDef> Classes { get; set; } = new List<ClassDef>();
    }

    public sealed class ClassDef
    {
        public HeroClass Id { get; set; }
        public string Badge { get; set; } = "";
        public StatSet<int> GrowthPercent { get; set; } = new StatSet<int>();
        public StatSet<int> RankBonus { get; set; } = new StatSet<int>();
    }

    /// <summary>content/champions.json: BAL-5, BAL-7, CHM-1..2.</summary>
    public sealed class ChampionFile
    {
        [JsonProperty("_doc")] public string? Doc { get; set; }
        public List<ChampionDef> Champions { get; set; } = new List<ChampionDef>();
    }

    public sealed class ChampionDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public HeroClass Class { get; set; }
        public Rarity Rarity { get; set; }
        public StatSet<StatTier> Shape { get; set; } = new StatSet<StatTier>();
        public StatSet<int> TweakPercent { get; set; } = new StatSet<int>();
    }
}
