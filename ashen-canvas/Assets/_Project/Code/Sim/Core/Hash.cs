namespace AshenCanvas.Sim.Core
{
    /// <summary>Целочисленные хеши (SplitMix64). Одинаковы на всех платформах.</summary>
    public static class Hash
    {
        public static ulong Mix(ulong z)
        {
            unchecked
            {
                z += 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        public static ulong Combine(ulong seed, int a, int b)
        {
            unchecked
            {
                ulong h = Mix(seed);
                h = Mix(h ^ (ulong)(uint)a);
                h = Mix(h ^ ((ulong)(uint)b << 1));
                return h;
            }
        }
    }
}
