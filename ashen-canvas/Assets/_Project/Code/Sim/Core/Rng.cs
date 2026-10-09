namespace AshenCanvas.Sim.Core
{
    /// <summary>Детерминированный генератор xorshift64*. Одинаковая последовательность на всех платформах.</summary>
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

        /// <summary>Число в [0, 1).</summary>
        public float NextFloat() => (NextU64() >> 40) * (1f / 16777216f);

        public float Range(float min, float max) => min + (max - min) * NextFloat();

        public T Pick<T>(System.Collections.Generic.IReadOnlyList<T> list) => list[NextInt(list.Count)];

        /// <summary>Индекс по весам. Все веса неотрицательны, сумма больше нуля.</summary>
        public int Weighted(System.Collections.Generic.IReadOnlyList<int> weights)
        {
            int total = 0;
            for (int i = 0; i < weights.Count; i++) total += weights[i];
            int r = NextInt(total);
            for (int i = 0; i < weights.Count; i++)
            {
                r -= weights[i];
                if (r < 0) return i;
            }
            return weights.Count - 1;
        }
    }
}
