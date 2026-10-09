using System;

namespace AshenCanvas.Sim.Stats
{
    /// <summary>Пигменты — стихии мира. Raw — удар самой кистью.</summary>
    public enum Element { Raw, Crimson, Azure, Golden, Ink }

    public enum Stat
    {
        MaxLife, MaxMana, LifeRegen, ManaRegen, Armor,
        ResCrimson, ResAzure, ResGolden, ResInk,
        DamagePct, RawDamagePct, CrimsonDamagePct, AzureDamagePct, GoldenDamagePct, InkDamagePct,
        WeaponMin, WeaponMax, WeaponFlat,
        CastSpeedPct, CritChance, CritMulti, AreaPct, MoveSpeedPct, CooldownPct,
        LifeOnKill, ItemFindPct,
        Count
    }

    [Serializable]
    public sealed class StatBlock
    {
        readonly float[] v = new float[(int)Stat.Count];

        public float this[Stat s]
        {
            get => v[(int)s];
            set => v[(int)s] = value;
        }

        public void Add(Stat s, float amount) => v[(int)s] += amount;

        public void Add(StatBlock other)
        {
            for (int i = 0; i < v.Length; i++) v[i] += other.v[i];
        }

        public static Stat DamagePctFor(Element e)
        {
            switch (e)
            {
                case Element.Crimson: return Stat.CrimsonDamagePct;
                case Element.Azure: return Stat.AzureDamagePct;
                case Element.Golden: return Stat.GoldenDamagePct;
                case Element.Ink: return Stat.InkDamagePct;
                default: return Stat.RawDamagePct;
            }
        }

        public static Stat ResFor(Element e)
        {
            switch (e)
            {
                case Element.Crimson: return Stat.ResCrimson;
                case Element.Azure: return Stat.ResAzure;
                case Element.Golden: return Stat.ResGolden;
                case Element.Ink: return Stat.ResInk;
                default: return Stat.Armor;
            }
        }
    }

    public static class Names
    {
        public static string Of(Element e)
        {
            switch (e)
            {
                case Element.Crimson: return "алый";
                case Element.Azure: return "лазурь";
                case Element.Golden: return "золото";
                case Element.Ink: return "чернила";
                default: return "удар";
            }
        }

        /// <summary>Цвет пигмента в формате 0xRRGGBB — для эффектов и интерфейса.</summary>
        public static uint ColorOf(Element e)
        {
            switch (e)
            {
                case Element.Crimson: return 0xFF3B4Au;
                case Element.Azure: return 0x2FB8FFu;
                case Element.Golden: return 0xFFC928u;
                case Element.Ink: return 0x8A3CFFu;
                default: return 0xF4EBDCu;
            }
        }
    }
}
