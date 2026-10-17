using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Game.Shared.Content
{
    /// <summary>Reads one content file by name without extension, e.g. "champions".</summary>
    public interface IContentSource
    {
        string Read(string name);
    }

    /// <summary>Reads content/*.json from a folder (server, tests, editor tools).</summary>
    public sealed class DirectoryContentSource : IContentSource
    {
        private readonly string _dir;
        public DirectoryContentSource(string dir) => _dir = dir;
        public string Read(string name) => File.ReadAllText(Path.Combine(_dir, name + ".json"));
    }

    public static class ContentLoader
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            // Typos in content must fail loudly, not load as 0.
            MissingMemberHandling = MissingMemberHandling.Error,
        };

        public static ContentDb Load(IContentSource source)
        {
            var version = Parse<VersionFile>(source, "version");
            var stats = Parse<StatRules>(source, "stats");
            var classes = Parse<ClassFile>(source, "classes").Classes;
            var champions = Parse<ChampionFile>(source, "champions").Champions;

            var errors = new List<string>();
            ValidateStats(stats, errors);
            AddDuplicates(classes.Select(c => c.Id.ToString()), "class", errors);
            AddDuplicates(champions.Select(c => c.Id), "champion", errors);
            var classIds = new HashSet<HeroClass>(classes.Select(c => c.Id));
            foreach (var c in champions)
                ValidateChampion(c, stats, classIds, errors);
            if (errors.Count > 0)
                throw new ContentException("Invalid content:\n- " + string.Join("\n- ", errors));

            return new ContentDb(
                version.ContentVersion,
                stats,
                classes.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First()),
                champions.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First()));
        }

        private static T Parse<T>(IContentSource source, string name) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(source.Read(name), Settings)
                       ?? throw new ContentException($"{name}.json is empty");
            }
            catch (JsonException e)
            {
                throw new ContentException($"{name}.json: {e.Message}", e);
            }
        }

        private static void ValidateStats(StatRules s, List<string> errors)
        {
            foreach (var stat in Stats.All)
                if (s.Tiers[stat] is null) errors.Add($"stats: missing tiers.{stat}");
            foreach (Rarity r in Enum.GetValues(typeof(Rarity)))
                if (!s.RarityPercent.ContainsKey(r)) errors.Add($"stats: missing rarityPercent.{r}");
            if (s.LevelsPerRank < 1) errors.Add("stats: levelsPerRank must be >= 1");
            if (s.MaxRank < 1) errors.Add("stats: maxRank must be >= 1");
            if (s.RankStatPercent < 100) errors.Add("stats: rankStatPercent must be >= 100");
            for (int rank = 2; rank <= s.MaxRank; rank++)
                if (s.RankUps.All(r => r.ToRank != rank)) errors.Add($"stats: missing rankUps toRank {rank}");
        }

        private static void ValidateChampion(ChampionDef c, StatRules s, HashSet<HeroClass> classIds, List<string> errors)
        {
            string who = $"champion '{c.Id}'";
            if (string.IsNullOrWhiteSpace(c.Id)) errors.Add("champion with empty id");
            if (string.IsNullOrWhiteSpace(c.Name)) errors.Add($"{who}: empty name");
            if (!classIds.Contains(c.Class)) errors.Add($"{who}: class {c.Class} has no content");
            foreach (var stat in Stats.All)
            {
                if (c.Shape[stat] == StatTier.Unset) errors.Add($"{who}: missing shape.{stat}");
                if (Math.Abs(c.TweakPercent[stat]) > s.MaxTweakPercent)
                    errors.Add($"{who}: tweakPercent.{stat} beyond +-{s.MaxTweakPercent} (BAL-7)");
            }
        }

        private static void AddDuplicates(IEnumerable<string> ids, string kind, List<string> errors)
        {
            foreach (var dup in ids.GroupBy(i => i).Where(g => g.Count() > 1))
                errors.Add($"duplicate {kind} id '{dup.Key}'");
        }
    }
}
