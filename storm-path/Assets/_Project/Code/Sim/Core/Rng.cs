namespace StormPath.Sim.Core
{
    /// <summary>Детерминированный генератор xorshift64*. Только целые числа.</summary>
    public sealed class Rng
    {
        ulong s;

        public Rng(ulong seed)
        {
            s = Hash.Mix(seed);
            if (s == 0) s = 0x9E3779B97F4A7C15UL;
        }

        public ulong NextU64()
        {
            unchecked
            {
                s ^= s >> 12;
                s ^= s << 25;
                s ^= s >> 27;
                return s * 0x2545F4914F6CDD1DUL;
            }
        }

        /// <summary>Число в [0, n).</summary>
        public int NextInt(int n) => (int)(((NextU64() >> 32) * (ulong)n) >> 32);

        /// <summary>Число в [min, maxInclusive].</summary>
        public int Range(int min, int maxInclusive) => min + NextInt(maxInclusive - min + 1);

        public bool Chance(int percent) => NextInt(100) < percent;
    }
}
