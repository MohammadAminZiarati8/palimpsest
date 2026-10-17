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

        internal ContentDb(int version, StatRules stats,
            IReadOnlyDictionary<HeroClass, ClassDef> classes, IReadOnlyDictionary<string, ChampionDef> champions)
        {
            Version = version;
            Stats = stats;
            Classes = classes;
            Champions = champions;
        }

        public ChampionDef Champion(string id) =>
            Champions.TryGetValue(id, out var c) ? c : throw new ContentException($"Unknown champion '{id}'");

        public ClassDef Class(HeroClass id) =>
            Classes.TryGetValue(id, out var c) ? c : throw new ContentException($"Class '{id}' has no content");
    }
}
