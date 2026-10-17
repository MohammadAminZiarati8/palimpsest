using System;
using System.Collections.Generic;

namespace Game.Shared.Content
{
    public sealed class ContentException : Exception
    {
        public ContentException(string message, Exception? inner = null) : base(message, inner) { }
    }

    /// <summary>All loaded, validated content. Build with <see cref="ContentLoader"/>.</summary>
    public sealed class ContentDb
    {
        public int Version { get; }
        public StatRules Stats { get; }
        public IReadOnlyDictionary<HeroClass, ClassDef> Classes { get; }
        public IReadOnlyDictionary<string, ChampionDef> Champions { get; }
        public CombatRules Combat { get; }
        /// <summary>Champion id → kit (S1..S3).</summary>
        public IReadOnlyDictionary<string, ChampionKit> Kits { get; }
        /// <summary>Enemy tiers, growth, Elite and crit defaults. Enemy list: <see cref="Enemies"/>.</summary>
        public EnemyFile EnemyRules { get; }
        public IReadOnlyDictionary<string, EnemyDef> Enemies { get; }

        internal ContentDb(int version, StatRules stats,
            IReadOnlyDictionary<HeroClass, ClassDef> classes, IReadOnlyDictionary<string, ChampionDef> champions,
            CombatRules combat, IReadOnlyDictionary<string, ChampionKit> kits,
            EnemyFile enemyRules, IReadOnlyDictionary<string, EnemyDef> enemies)
        {
            Version = version;
            Stats = stats;
            Classes = classes;
            Champions = champions;
            Combat = combat;
            Kits = kits;
            EnemyRules = enemyRules;
            Enemies = enemies;
        }

        public ChampionDef Champion(string id) =>
            Champions.TryGetValue(id, out var c) ? c : throw new ContentException($"Unknown champion '{id}'");

        public ClassDef Class(HeroClass id) =>
            Classes.TryGetValue(id, out var c) ? c : throw new ContentException($"Class '{id}' has no content");

        public ChampionKit Kit(string championId) =>
            Kits.TryGetValue(championId, out var k) ? k : throw new ContentException($"Champion '{championId}' has no kit");

        public EnemyDef Enemy(string id) =>
            Enemies.TryGetValue(id, out var e) ? e : throw new ContentException($"Unknown enemy '{id}'");
    }
}
