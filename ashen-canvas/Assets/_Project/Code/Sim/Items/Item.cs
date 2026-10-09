using System;
using System.Collections.Generic;
using AshenCanvas.Sim.Stats;

namespace AshenCanvas.Sim.Items
{
    [Serializable]
    public sealed class StatRoll
    {
        public string affix;  // id свойства; пусто для собственных свойств основы и уникальных
        public int stat;      // (int)Stat
        public int value;
        public int tier;      // 1 — лучший
    }

    /// <summary>Предмет. Поля открыты для JsonUtility (сохранения).</summary>
    [Serializable]
    public sealed class Item
    {
        public long uid;
        public string baseId;
        public string uniqueId = "";
        public int rarity;     // (int)Rarity
        public int itemLevel;
        public string name;
        public List<StatRoll> implicits = new List<StatRoll>();
        public List<StatRoll> affixes = new List<StatRoll>();

        public Rarity Rarity => (Rarity)rarity;
        public ItemBase Base => ItemDb.GetBase(baseId);
        public bool IsEmpty => string.IsNullOrEmpty(baseId);

        public void AddStatsTo(StatBlock st)
        {
            var b = Base;
            if (b.Slot == ItemSlot.Brush)
            {
                st.Add(Stat.WeaponMin, b.DamageMin);
                st.Add(Stat.WeaponMax, b.DamageMax);
            }
            for (int i = 0; i < implicits.Count; i++) st.Add((Stat)implicits[i].stat, implicits[i].value);
            for (int i = 0; i < affixes.Count; i++) st.Add((Stat)affixes[i].stat, affixes[i].value);
        }

        public int Value()
        {
            int mult = Rarity == Rarity.Normal ? 1 : Rarity == Rarity.Magic ? 3 : Rarity == Rarity.Rare ? 8 : 20;
            return (2 + itemLevel) * mult + affixes.Count * 3;
        }

        public List<string> Describe()
        {
            var lines = new List<string>();
            var b = Base;
            if (b.Slot == ItemSlot.Brush) lines.Add("Урон кисти: " + b.DamageMin + "–" + b.DamageMax);
            for (int i = 0; i < implicits.Count; i++) lines.Add(ItemDb.StatText((Stat)implicits[i].stat, implicits[i].value));
            if (implicits.Count > 0 && affixes.Count > 0) lines.Add("—");
            for (int i = 0; i < affixes.Count; i++) lines.Add(ItemDb.StatText((Stat)affixes[i].stat, affixes[i].value));
            lines.Add("Требуемый уровень: " + b.Level);
            return lines;
        }

        public static string RarityName(Rarity r)
        {
            switch (r)
            {
                case Rarity.Magic: return "Волшебный";
                case Rarity.Rare: return "Редкий";
                case Rarity.Unique: return "Уникальный";
                default: return "Обычный";
            }
        }

        public static uint RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Magic: return 0x7A9CFFu;
                case Rarity.Rare: return 0xFFE35Au;
                case Rarity.Unique: return 0xFF8A2Au;
                default: return 0xE8E2D6u;
            }
        }
    }
}
