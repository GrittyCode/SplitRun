namespace SplitRun.Utility
{
    // Stateless hash rolls so host and client derive identical track layouts from one shared seed.
    public static class DeterministicRandom
    {
        // 24-bit mantissa keeps the [0, 1) result float-exact on every platform.
        private const float k_InverseTwoPow24 = 1f / 16777216f;

        /// <summary>Returns a stable value in [0, 1) for the seed/slot/salt triple.</summary>
        public static float NextFloat(int seed, int slot, int salt)
            => (Hash(seed, slot, salt) >> 8) * k_InverseTwoPow24;

        /// <summary>Returns a stable integer in [minInclusive, maxExclusive) for the seed/slot/salt triple.</summary>
        public static int NextInt(int seed, int slot, int salt, int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;

            uint span = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(Hash(seed, slot, salt) % span);
        }

        // SplitMix32-style avalanche — integer-only, so results match across platforms and runtimes.
        private static uint Hash(int seed, int slot, int salt)
        {
            uint h = (uint)seed ^ ((uint)slot * 0x9E3779B9u) ^ ((uint)salt * 0x85EBCA6Bu);
            h ^= h >> 16;
            h *= 0x21F0AAADu;
            h ^= h >> 15;
            h *= 0x735A2D97u;
            h ^= h >> 15;
            return h;
        }
    }
}
