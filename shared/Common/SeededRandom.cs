using System;

namespace Game.Shared.Common
{
    /// <summary>
    /// Deterministic RNG (SplitMix64). Same seed gives the same sequence on server (.NET) and
    /// client (IL2CPP), unlike System.Random. Used for battle seeds and server rolls (A3).
    /// </summary>
    public sealed class SeededRandom
    {
        private ulong _state;

        public SeededRandom(ulong seed) => _state = seed;

        public ulong NextULong()
        {
            ulong z = _state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>Uniform int in [minInclusive, maxExclusive), no modulo bias.</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));

            ulong range = (ulong)((long)maxExclusive - minInclusive);
            ulong threshold = (0UL - range) % range;
            ulong r;
            do { r = NextULong(); } while (r < threshold);
            return (int)(minInclusive + (long)(r % range));
        }

        /// <summary>True with the given chance in percent (0..100).</summary>
        public bool RollPercent(int percent) => NextInt(0, 100) < percent;
    }
}
