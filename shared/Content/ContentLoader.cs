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
            var combat = Parse<CombatRules>(source, "combat");
            var kits = Parse<SkillFile>(source, "skills").Kits;
            var enemyFile = Parse<EnemyFile>(source, "enemies");
            var stageFile = Parse<StageFile>(source, "stages");
            var stages = stageFile.Stages;

            var errors = new List<string>();
            ValidateStats(stats, errors);
            AddDuplicates(classes.Select(c => c.Id.ToString()), "class", errors);
            AddDuplicates(champions.Select(c => c.Id), "champion", errors);
            var classIds = new HashSet<HeroClass>(classes.Select(c => c.Id));
            foreach (var c in champions)
                ValidateChampion(c, stats, classIds, errors);
            ValidateCombat(combat, errors);
            ValidateKits(kits, champions, combat, errors);
            ValidateEnemies(enemyFile, combat, errors);
            ValidateStages(stages, champions, enemyFile, combat, errors);
            CheckRange(stageFile.DoorHealPercent, 0, 100, "stages: doorHealPercent", errors);
            CheckRange(stageFile.RoomMinPercent, 0, 100, "stages: roomMinPercent", errors);
            CheckRange(stageFile.RoomMaxPercent, stageFile.RoomMinPercent, 100, "stages: roomMaxPercent", errors);
            CheckRange(stageFile.RoomBasePercent, 0, 100, "stages: roomBasePercent", errors);
            CheckRange(stageFile.MarginStrikeCritPercent, 0, 100, "stages: marginStrikeCritPercent", errors);
            CheckRange(stageFile.NoticePerFight, 0, 99, "stages: noticePerFight", errors);
            CheckRange(stageFile.NoticePerRing, 0, 99, "stages: noticePerRing", errors);
            CheckRange(stageFile.NoticeWatchingAt, 1, 99, "stages: noticeWatchingAt", errors);
            CheckRange(stageFile.NoticePatrolAt, 1, 99, "stages: noticePatrolAt", errors);
            CheckRange(stageFile.NoticePatrol.InRounds, 1, 99, "stages: noticePatrol.inRounds", errors);
            if (!enemyFile.Enemies.Any(e => e.Id == stageFile.NoticePatrol.Enemy))
                errors.Add($"stages: noticePatrol.enemy '{stageFile.NoticePatrol.Enemy}' is not an enemy");
            if (errors.Count > 0)
                throw new ContentException("Invalid content:\n- " + string.Join("\n- ", errors));

            return new ContentDb(
                version.ContentVersion,
                stats,
                classes.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First()),
                champions.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First()),
                combat,
                kits.GroupBy(k => k.Champion).ToDictionary(g => g.Key, g => g.First()),
                enemyFile,
                enemyFile.Enemies.GroupBy(e => e.Id).ToDictionary(g => g.Key, g => g.First()),
                stageFile);
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

        private static void ValidateCombat(CombatRules c, List<string> errors)
        {
            if (c.DefenseK < 1) errors.Add("combat: defenseK must be >= 1 (BAL-2)");
            CheckRange(c.SpreadPercent, 0, 50, "combat: spreadPercent", errors);
            CheckRange(c.ExposedCritPercent, 0, 100, "combat: exposedCritPercent", errors);
            CheckRange(c.CritCapPercent, 1, 100, "combat: critCapPercent", errors);
            if (c.RoundCap < 1) errors.Add("combat: roundCap must be >= 1");
            var e = c.Effects;
            CheckRange(e.BleedAttackPercent, 1, 1000, "combat: effects.bleedAttackPercent", errors);
            CheckRange(e.AttackPercent, 1, 100, "combat: effects.attackPercent", errors);
            CheckRange(e.DefensePercent, 1, 100, "combat: effects.defensePercent", errors);
            if (e.Speed < 1) errors.Add("combat: effects.speed must be >= 1");
            if (e.Accuracy < 1) errors.Add("combat: effects.accuracy must be >= 1");
            if (e.WardRounds < 1) errors.Add("combat: effects.wardRounds must be >= 1");
        }

        private static void ValidateKits(List<ChampionKit> kits, List<ChampionDef> champions, CombatRules combat, List<string> errors)
        {
            AddDuplicates(kits.Select(k => k.Champion), "kit", errors);
            var championIds = new HashSet<string>(champions.Select(c => c.Id));
            foreach (var k in kits)
            {
                string who = $"kit '{k.Champion}'";
                if (!championIds.Contains(k.Champion)) errors.Add($"{who}: unknown champion");
                ValidateSkills(who, k.Skills, requireAllSlots: true, combat, errors);
            }
            foreach (var c in champions)
                if (kits.All(k => k.Champion != c.Id)) errors.Add($"champion '{c.Id}': missing kit");
        }

        private static void ValidateEnemies(EnemyFile f, CombatRules combat, List<string> errors)
        {
            foreach (var stat in Stats.EnemyTiered)
                if (f.Tiers[stat] is null) errors.Add($"enemies: missing tiers.{stat}");
            CheckRange(f.GrowthPercent, 0, 100, "enemies: growthPercent", errors);
            CheckRange(f.EliteHpPercent, 100, 1000, "enemies: eliteHpPercent", errors);
            CheckRange(f.EliteAttackPercent, 100, 1000, "enemies: eliteAttackPercent", errors);
            CheckRange(f.CritChance, 0, 100, "enemies: critChance", errors);
            CheckRange(f.CritDamage, 0, 1000, "enemies: critDamage", errors);
            AddDuplicates(f.Enemies.Select(e => e.Id), "enemy", errors);
            foreach (var e in f.Enemies)
            {
                string who = $"enemy '{e.Id}'";
                if (string.IsNullOrWhiteSpace(e.Id)) errors.Add("enemy with empty id");
                if (string.IsNullOrWhiteSpace(e.Name)) errors.Add($"{who}: empty name");
                if (e.Role == EnemyRole.Unset) errors.Add($"{who}: missing role");
                if (e.Slot == RowSlot.Unset) errors.Add($"{who}: missing slot");
                foreach (var stat in Stats.EnemyTiered)
                {
                    var tier = e.Shape[stat];
                    if (tier == StatTier.Unset) errors.Add($"{who}: missing shape.{stat}");
                    else if (f.Tiers[stat] is { } t && t.Get(tier) <= 0)
                        errors.Add($"{who}: shape.{stat} {tier} has no tier value (BAL-16)");
                }
                if (e.CritChance is { } cc) CheckRange(cc, 0, 100, $"{who}: critChance", errors);
                if (e.CritDamage is { } cd) CheckRange(cd, 0, 1000, $"{who}: critDamage", errors);
                ValidateSkills(who, e.Skills, requireAllSlots: false, combat, errors);
            }
        }

        /// <summary>Champions need S1..S3; enemies need S1 (S2/S3 optional). No slot twice.</summary>
        private static void ValidateSkills(string who, List<SkillDef> skills, bool requireAllSlots, CombatRules combat, List<string> errors)
        {
            foreach (var dup in skills.Where(s => s.Slot != SkillSlot.Unset).GroupBy(s => s.Slot).Where(g => g.Count() > 1))
                errors.Add($"{who}: duplicate skill {dup.Key}");
            var required = requireAllSlots ? new[] { SkillSlot.S1, SkillSlot.S2, SkillSlot.S3 } : new[] { SkillSlot.S1 };
            foreach (var slot in required)
                if (skills.All(s => s.Slot != slot)) errors.Add($"{who}: missing skill {slot}");
            foreach (var s in skills)
                ValidateSkill($"{who} {s.Slot}", s, combat, errors);
        }

        private static void ValidateSkill(string who, SkillDef s, CombatRules combat, List<string> errors)
        {
            if (s.Slot == SkillSlot.Unset) errors.Add($"{who}: missing slot");
            if (string.IsNullOrWhiteSpace(s.Name)) errors.Add($"{who}: empty name");

            // CMB-42: S1 has no cooldown; S2/S3 are active in the demo (no passives yet).
            if (s.Slot == SkillSlot.S1 && s.Cooldown != 0) errors.Add($"{who}: S1 must have cooldown 0");
            if (s.Slot != SkillSlot.S1 && (s.Cooldown < 1 || s.Cooldown > combat.RoundCap))
                errors.Add($"{who}: cooldown {s.Cooldown} outside 1..{combat.RoundCap} (CMB-42)");

            // CMB-13 / CMB-27: enemy-side areas need Melee/Ranged; self/ally skills have Reach None.
            bool hitsEnemies = s.Area == Area.Single || s.Area == Area.Splash || s.Area == Area.Row;
            if (s.Area == Area.Unset) errors.Add($"{who}: missing area");
            if (s.Reach == Reach.Unset) errors.Add($"{who}: missing reach");
            else if (s.Area != Area.Unset && hitsEnemies == (s.Reach == Reach.None))
                errors.Add($"{who}: reach {s.Reach} doesn't fit area {s.Area}");
            if (s.Dive && s.Reach != Reach.Melee) errors.Add($"{who}: dive needs Melee reach (CMB-16)");

            CheckRange(s.DamagePercent, 0, 1000, $"{who}: damagePercent", errors);
            if (s.DamagePercent > 0 && !hitsEnemies) errors.Add($"{who}: damage on a self/ally skill");
            CheckRange(s.CritChanceBonus, 0, 100, $"{who}: critChanceBonus", errors);
            CheckRange(s.HealPercent, 0, 100, $"{who}: healPercent", errors);
            if (s.HealPercent > 0 && s.HealOf == HealBasis.Unset) errors.Add($"{who}: heal needs healOf");
            CheckRange(s.WardPercent, 0, 100, $"{who}: wardPercent", errors);
            CheckRange(s.Cleanse, 0, 10, $"{who}: cleanse", errors);
            if ((s.HealPercent > 0 || s.WardPercent > 0 || s.Cleanse > 0) && hitsEnemies)
                errors.Add($"{who}: heal/ward/cleanse on an enemy-side skill");

            foreach (var e in s.Effects)
            {
                if (e.Type == StatusEffect.Unset) errors.Add($"{who}: effect missing type");
                if (e.Type == StatusEffect.Ward) errors.Add($"{who}: use wardPercent, not a Ward effect");
                CheckRange(e.Rounds, 1, 10, $"{who}: {e.Type} rounds", errors);
            }
            if (s.Special is { } sp)
            {
                if (sp.Type == SpecialType.None) errors.Add($"{who}: special missing type");
                CheckRange(sp.Rounds, 1, 10, $"{who}: special rounds", errors);
            }
            if (s.Ai is { When: AiWhen.AllyHpAtOrBelow } ai)
                CheckRange(ai.Percent, 1, 99, $"{who}: ai.percent", errors);

            bool doesSomething = s.DamagePercent > 0 || s.HealPercent > 0 || s.WardPercent > 0 || s.Cleanse > 0
                                 || s.Effects.Count > 0 || s.Special != null;
            if (!doesSomething) errors.Add($"{who}: skill does nothing");
        }

        /// <summary>D-95 stages: known ids, CMB-8 capacity, 1–3 enemies, Herald ⇔ reinforcements.</summary>
        private static void ValidateStages(List<StageDef> stages, List<ChampionDef> champions, EnemyFile enemyFile,
            CombatRules combat, List<string> errors)
        {
            AddDuplicates(stages.Select(s => s.Id), "stage", errors);
            var heroIds = new HashSet<string>(champions.Select(c => c.Id));
            var enemies = enemyFile.Enemies.GroupBy(e => e.Id).ToDictionary(g => g.Key, g => g.First());
            foreach (var st in stages)
            {
                string who = $"stage '{st.Id}'";
                if (string.IsNullOrWhiteSpace(st.Id)) errors.Add("stage with empty id");
                if (string.IsNullOrWhiteSpace(st.Name)) errors.Add($"{who}: empty name");
                if (string.IsNullOrWhiteSpace(st.Chapter)) errors.Add($"{who}: empty chapter");
                if (st.EnemyLevel < 1) errors.Add($"{who}: enemyLevel must be >= 1");
                if (st.Heroes.Count == 0) errors.Add($"{who}: no heroes");
                foreach (var h in st.Heroes.Where(h => !heroIds.Contains(h))) errors.Add($"{who}: unknown hero '{h}'");
                if (st.Scenes.Count == 0) errors.Add($"{who}: no scenes");
                for (int i = 0; i < st.Scenes.Count; i++)
                {
                    var sc = st.Scenes[i];
                    string where = $"{who} scene {i + 1}";
                    CheckRange(sc.Capacity, 1, 3, $"{where}: capacity", errors);
                    if (sc.Alert == Alert.Unset) errors.Add($"{where}: missing alert");
                    CheckRange(sc.Enemies.Count, 1, 3, $"{where}: enemy count", errors);
                    var ids = sc.Enemies.Concat(sc.Arrivals.Select(a => a.Enemy)).Concat(sc.Reinforcements);
                    foreach (var id in ids.Where(id => !enemies.ContainsKey(id)).Distinct())
                        errors.Add($"{where}: unknown enemy '{id}'");
                    foreach (var a in sc.Arrivals)
                        CheckRange(a.InRounds, 1, combat.RoundCap, $"{where}: arrival {a.Enemy} inRounds", errors);
                    bool herald = sc.Enemies.Any(id => enemies.TryGetValue(id, out var e) && e.Role == EnemyRole.Herald);
                    if (herald && sc.Reinforcements.Count == 0) errors.Add($"{where}: Herald needs reinforcements (ENM-9)");
                    if (!herald && sc.Reinforcements.Count > 0) errors.Add($"{where}: reinforcements without a Herald");
                }
            }
        }

        private static void CheckRange(int value, int min, int max, string what, List<string> errors)
        {
            if (value < min || value > max) errors.Add($"{what} {value} outside {min}..{max}");
        }

        private static void AddDuplicates(IEnumerable<string> ids, string kind, List<string> errors)
        {
            foreach (var dup in ids.GroupBy(i => i).Where(g => g.Count() > 1))
                errors.Add($"duplicate {kind} id '{dup.Key}'");
        }
    }
}
