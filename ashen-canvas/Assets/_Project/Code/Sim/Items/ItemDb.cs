using System;
using System.Collections.Generic;
using AshenCanvas.Sim.Stats;

namespace AshenCanvas.Sim.Items
{
    public enum ItemSlot { Brush, Palette, Hat, Coat, Gloves, Boots, Ring, Amulet }

    /// <summary>Ячейки экипировки. Колец два.</summary>
    public enum EquipSlot { Brush, Palette, Hat, Coat, Gloves, Boots, Ring1, Ring2, Amulet, Count }

    public enum Rarity { Normal, Magic, Rare, Unique }

    public struct StatRange
    {
        public Stat Stat;
        public int Min, Max;
        public StatRange(Stat s, int min, int max) { Stat = s; Min = min; Max = max; }
    }

    public sealed class ItemBase
    {
        public string Id, Name;
        public ItemSlot Slot;
        public int Level;               // требуемый уровень и минимальный уровень предмета
        public int DamageMin, DamageMax; // только для кистей
        public StatRange[] Implicits = new StatRange[0];
        public string AssetId;
    }

    public sealed class AffixTier
    {
        public int ItemLevel, Min, Max;
        public AffixTier(int ilvl, int min, int max) { ItemLevel = ilvl; Min = min; Max = max; }
    }

    public sealed class AffixDef
    {
        public string Id;
        public bool Prefix;
        public Stat Stat;
        public string Text;    // "+{0} к здоровью"
        public string Word;    // слово для имени волшебного предмета: «Берет жизни»
        public ItemSlot[] Slots;
        public AffixTier[] Tiers;

        public bool Allows(ItemSlot s) => Array.IndexOf(Slots, s) >= 0;
    }

    public sealed class UniqueDef
    {
        public string Id, Name, BaseId, Flavor;
        public StatRange[] Stats;
    }

    public static class ItemDb
    {
        static readonly ItemSlot[] AllSlots = { ItemSlot.Brush, ItemSlot.Palette, ItemSlot.Hat, ItemSlot.Coat, ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Ring, ItemSlot.Amulet };
        static readonly ItemSlot[] NotBrush = { ItemSlot.Palette, ItemSlot.Hat, ItemSlot.Coat, ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Ring, ItemSlot.Amulet };
        static readonly ItemSlot[] Armour = { ItemSlot.Hat, ItemSlot.Coat, ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Palette };
        static readonly ItemSlot[] Offence = { ItemSlot.Brush, ItemSlot.Palette, ItemSlot.Amulet, ItemSlot.Ring, ItemSlot.Gloves };
        static readonly ItemSlot[] Pigment = { ItemSlot.Brush, ItemSlot.Palette, ItemSlot.Amulet, ItemSlot.Ring };

        public static readonly IReadOnlyList<ItemBase> Bases = new List<ItemBase>
        {
            new ItemBase { Id = "bristle_brush", Name = "Щетинная кисть", Slot = ItemSlot.Brush, Level = 1, DamageMin = 3, DamageMax = 7, AssetId = "item_brush" },
            new ItemBase { Id = "flat_brush", Name = "Плоская кисть", Slot = ItemSlot.Brush, Level = 4, DamageMin = 6, DamageMax = 11, AssetId = "item_brush" },
            new ItemBase { Id = "fan_brush", Name = "Веерная кисть", Slot = ItemSlot.Brush, Level = 8, DamageMin = 9, DamageMax = 16,
                Implicits = new[] { new StatRange(Stat.CritChance, 2, 3) }, AssetId = "item_brush" },
            new ItemBase { Id = "palette_knife", Name = "Мастихин", Slot = ItemSlot.Brush, Level = 12, DamageMin = 13, DamageMax = 22, AssetId = "item_brush" },
            new ItemBase { Id = "sable_brush", Name = "Колонковая кисть", Slot = ItemSlot.Brush, Level = 18, DamageMin = 18, DamageMax = 30,
                Implicits = new[] { new StatRange(Stat.CastSpeedPct, 5, 8) }, AssetId = "item_brush" },

            new ItemBase { Id = "wood_palette", Name = "Деревянная палитра", Slot = ItemSlot.Palette, Level = 1,
                Implicits = new[] { new StatRange(Stat.MaxMana, 8, 12) }, AssetId = "item_palette" },
            new ItemBase { Id = "porcelain_palette", Name = "Фарфоровая палитра", Slot = ItemSlot.Palette, Level = 7,
                Implicits = new[] { new StatRange(Stat.MaxMana, 15, 22), new StatRange(Stat.AreaPct, 4, 6) }, AssetId = "item_palette" },
            new ItemBase { Id = "glass_palette", Name = "Стеклянная палитра", Slot = ItemSlot.Palette, Level = 14,
                Implicits = new[] { new StatRange(Stat.MaxMana, 25, 35), new StatRange(Stat.CritChance, 2, 3) }, AssetId = "item_palette" },

            new ItemBase { Id = "straw_hat", Name = "Соломенная шляпа", Slot = ItemSlot.Hat, Level = 1,
                Implicits = new[] { new StatRange(Stat.Armor, 4, 8) }, AssetId = "item_hat" },
            new ItemBase { Id = "beret", Name = "Берет", Slot = ItemSlot.Hat, Level = 5,
                Implicits = new[] { new StatRange(Stat.Armor, 10, 16), new StatRange(Stat.MaxLife, 4, 8) }, AssetId = "item_hat" },
            new ItemBase { Id = "top_hat", Name = "Цилиндр", Slot = ItemSlot.Hat, Level = 12,
                Implicits = new[] { new StatRange(Stat.Armor, 22, 32) }, AssetId = "item_hat" },

            new ItemBase { Id = "apron", Name = "Фартук", Slot = ItemSlot.Coat, Level = 1,
                Implicits = new[] { new StatRange(Stat.Armor, 8, 14) }, AssetId = "item_coat" },
            new ItemBase { Id = "smock", Name = "Блуза художника", Slot = ItemSlot.Coat, Level = 6,
                Implicits = new[] { new StatRange(Stat.Armor, 18, 28) }, AssetId = "item_coat" },
            new ItemBase { Id = "velvet_coat", Name = "Бархатный сюртук", Slot = ItemSlot.Coat, Level = 13,
                Implicits = new[] { new StatRange(Stat.Armor, 35, 50) }, AssetId = "item_coat" },

            new ItemBase { Id = "cloth_gloves", Name = "Тканевые перчатки", Slot = ItemSlot.Gloves, Level = 1,
                Implicits = new[] { new StatRange(Stat.Armor, 3, 6) }, AssetId = "item_gloves" },
            new ItemBase { Id = "leather_gloves", Name = "Кожаные перчатки", Slot = ItemSlot.Gloves, Level = 7,
                Implicits = new[] { new StatRange(Stat.Armor, 8, 14), new StatRange(Stat.CastSpeedPct, 2, 4) }, AssetId = "item_gloves" },

            new ItemBase { Id = "sandals", Name = "Сандалии", Slot = ItemSlot.Boots, Level = 1,
                Implicits = new[] { new StatRange(Stat.Armor, 3, 6), new StatRange(Stat.MoveSpeedPct, 3, 3) }, AssetId = "item_boots" },
            new ItemBase { Id = "tall_boots", Name = "Высокие сапоги", Slot = ItemSlot.Boots, Level = 7,
                Implicits = new[] { new StatRange(Stat.Armor, 8, 14), new StatRange(Stat.MoveSpeedPct, 5, 5) }, AssetId = "item_boots" },

            new ItemBase { Id = "garnet_ring", Name = "Кольцо с гранатом", Slot = ItemSlot.Ring, Level = 2,
                Implicits = new[] { new StatRange(Stat.ResCrimson, 10, 15) }, AssetId = "item_ring" },
            new ItemBase { Id = "sapphire_ring", Name = "Кольцо с сапфиром", Slot = ItemSlot.Ring, Level = 2,
                Implicits = new[] { new StatRange(Stat.ResAzure, 10, 15) }, AssetId = "item_ring" },
            new ItemBase { Id = "topaz_ring", Name = "Кольцо с топазом", Slot = ItemSlot.Ring, Level = 2,
                Implicits = new[] { new StatRange(Stat.ResGolden, 10, 15) }, AssetId = "item_ring" },
            new ItemBase { Id = "obsidian_ring", Name = "Кольцо с обсидианом", Slot = ItemSlot.Ring, Level = 5,
                Implicits = new[] { new StatRange(Stat.ResInk, 8, 12) }, AssetId = "item_ring" },

            new ItemBase { Id = "tassel_amulet", Name = "Подвеска-кисточка", Slot = ItemSlot.Amulet, Level = 1,
                Implicits = new[] { new StatRange(Stat.MaxMana, 8, 12) }, AssetId = "item_amulet" },
            new ItemBase { Id = "amber_amulet", Name = "Янтарный амулет", Slot = ItemSlot.Amulet, Level = 6,
                Implicits = new[] { new StatRange(Stat.DamagePct, 6, 10) }, AssetId = "item_amulet" },
        };

        static AffixDef A(string id, bool prefix, Stat s, string text, string word, ItemSlot[] slots, params AffixTier[] tiers)
            => new AffixDef { Id = id, Prefix = prefix, Stat = s, Text = text, Word = word, Slots = slots, Tiers = tiers };

        static AffixTier T(int ilvl, int min, int max) => new AffixTier(ilvl, min, max);

        public static readonly IReadOnlyList<AffixDef> Affixes = new List<AffixDef>
        {
            A("life", true, Stat.MaxLife, "+{0} к здоровью", "жизни", NotBrush, T(1, 5, 12), T(8, 13, 25), T(16, 26, 40), T(24, 41, 60)),
            A("mana", true, Stat.MaxMana, "+{0} к мане", "вдохновения", AllSlots, T(1, 5, 10), T(9, 11, 20), T(18, 21, 35)),
            A("armor", true, Stat.Armor, "+{0} к броне", "стойкости", Armour, T(1, 5, 12), T(8, 13, 25), T(16, 26, 45)),
            A("dmg", true, Stat.DamagePct, "+{0}% к урону", "мастера", Offence, T(1, 5, 10), T(8, 11, 18), T(16, 19, 30), T(24, 31, 45)),
            A("weapon_flat", true, Stat.WeaponFlat, "+{0} к урону кисти", "тяжести", new[] { ItemSlot.Brush }, T(1, 1, 3), T(7, 4, 6), T(14, 7, 10), T(22, 11, 15)),
            A("crimson_dmg", true, Stat.CrimsonDamagePct, "+{0}% к урону алым", "пламени", Pigment, T(1, 8, 14), T(10, 15, 25), T(20, 26, 40)),
            A("azure_dmg", true, Stat.AzureDamagePct, "+{0}% к урону лазурью", "прилива", Pigment, T(1, 8, 14), T(10, 15, 25), T(20, 26, 40)),
            A("golden_dmg", true, Stat.GoldenDamagePct, "+{0}% к урону золотом", "зари", Pigment, T(1, 8, 14), T(10, 15, 25), T(20, 26, 40)),
            A("ink_dmg", true, Stat.InkDamagePct, "+{0}% к урону чернилами", "сумрака", Pigment, T(1, 8, 14), T(10, 15, 25), T(20, 26, 40)),

            A("res_crimson", false, Stat.ResCrimson, "+{0}% к защите от алого", "саламандры", NotBrush, T(1, 6, 12), T(10, 13, 20), T(20, 21, 30)),
            A("res_azure", false, Stat.ResAzure, "+{0}% к защите от лазури", "моржа", NotBrush, T(1, 6, 12), T(10, 13, 20), T(20, 21, 30)),
            A("res_golden", false, Stat.ResGolden, "+{0}% к защите от золота", "громоотвода", NotBrush, T(1, 6, 12), T(10, 13, 20), T(20, 21, 30)),
            A("res_ink", false, Stat.ResInk, "+{0}% к защите от чернил", "промокашки", NotBrush, T(3, 5, 10), T(12, 11, 18), T(22, 19, 25)),
            A("crit", false, Stat.CritChance, "+{0}% к шансу крит. удара", "точности", new[] { ItemSlot.Brush, ItemSlot.Amulet, ItemSlot.Gloves, ItemSlot.Ring }, T(1, 2, 3), T(10, 4, 5), T(20, 6, 8)),
            A("crit_multi", false, Stat.CritMulti, "+{0}% к силе крит. удара", "мощи", new[] { ItemSlot.Brush, ItemSlot.Amulet }, T(5, 10, 18), T(15, 19, 30)),
            A("cast", false, Stat.CastSpeedPct, "+{0}% к скорости навыков", "быстроты", new[] { ItemSlot.Brush, ItemSlot.Gloves, ItemSlot.Amulet, ItemSlot.Ring }, T(1, 3, 6), T(10, 7, 10), T(20, 11, 15)),
            A("move", false, Stat.MoveSpeedPct, "+{0}% к скорости бега", "ветра", new[] { ItemSlot.Boots }, T(1, 5, 9), T(10, 10, 15), T(20, 16, 22)),
            A("mana_regen", false, Stat.ManaRegen, "+{0} маны в секунду", "родника", new[] { ItemSlot.Palette, ItemSlot.Amulet, ItemSlot.Ring, ItemSlot.Hat }, T(1, 1, 2), T(10, 3, 4), T(20, 5, 7)),
            A("life_regen", false, Stat.LifeRegen, "+{0} здоровья в секунду", "тролля", new[] { ItemSlot.Coat, ItemSlot.Amulet, ItemSlot.Ring, ItemSlot.Hat }, T(1, 1, 2), T(10, 3, 5), T(20, 6, 9)),
            A("area", false, Stat.AreaPct, "+{0}% к области навыков", "размаха", new[] { ItemSlot.Palette, ItemSlot.Amulet }, T(4, 5, 9), T(14, 10, 16)),
            A("cooldown", false, Stat.CooldownPct, "+{0}% к скорости перезарядки", "вечности", new[] { ItemSlot.Palette, ItemSlot.Hat, ItemSlot.Amulet }, T(6, 5, 9), T(16, 10, 15)),
            A("life_on_kill", false, Stat.LifeOnKill, "+{0} здоровья за убийство", "вампира", new[] { ItemSlot.Brush, ItemSlot.Gloves, ItemSlot.Ring }, T(1, 1, 3), T(10, 4, 6), T(20, 7, 10)),
            A("item_find", false, Stat.ItemFindPct, "+{0}% к редкости находок", "сороки", new[] { ItemSlot.Hat, ItemSlot.Boots, ItemSlot.Ring, ItemSlot.Amulet }, T(1, 5, 10), T(12, 11, 18), T(22, 19, 26)),
        };

        public static readonly IReadOnlyList<UniqueDef> Uniques = new List<UniqueDef>
        {
            new UniqueDef { Id = "last_stroke", Name = "Последний мазок", BaseId = "flat_brush",
                Flavor = "«Картина закончена, когда враг упал».",
                Stats = new[] { new StatRange(Stat.DamagePct, 30, 40), new StatRange(Stat.CrimsonDamagePct, 20, 30),
                                new StatRange(Stat.LifeOnKill, 3, 5), new StatRange(Stat.CritChance, 3, 5) } },
            new UniqueDef { Id = "mad_beret", Name = "Берет безумного импрессиониста", BaseId = "beret",
                Flavor = "Видит мир пятнами. Пятна взрываются.",
                Stats = new[] { new StatRange(Stat.AreaPct, 15, 20), new StatRange(Stat.CastSpeedPct, 8, 12),
                                new StatRange(Stat.CrimsonDamagePct, 10, 15), new StatRange(Stat.AzureDamagePct, 10, 15),
                                new StatRange(Stat.GoldenDamagePct, 10, 15), new StatRange(Stat.InkDamagePct, 10, 15) } },
            new UniqueDef { Id = "all_colors", Name = "Палитра всех цветов", BaseId = "porcelain_palette",
                Flavor = "На ней нет пустого места.",
                Stats = new[] { new StatRange(Stat.CrimsonDamagePct, 12, 18), new StatRange(Stat.AzureDamagePct, 12, 18),
                                new StatRange(Stat.GoldenDamagePct, 12, 18), new StatRange(Stat.InkDamagePct, 12, 18),
                                new StatRange(Stat.ManaRegen, 2, 3) } },
        };

        public static ItemBase GetBase(string id)
        {
            for (int i = 0; i < Bases.Count; i++)
                if (Bases[i].Id == id) return Bases[i];
            throw new ArgumentException("Нет основы " + id);
        }

        public static AffixDef GetAffix(string id)
        {
            for (int i = 0; i < Affixes.Count; i++)
                if (Affixes[i].Id == id) return Affixes[i];
            throw new ArgumentException("Нет свойства " + id);
        }

        public static UniqueDef GetUnique(string id)
        {
            for (int i = 0; i < Uniques.Count; i++)
                if (Uniques[i].Id == id) return Uniques[i];
            throw new ArgumentException("Нет уникального " + id);
        }

        public static string StatText(Stat s, int v)
        {
            for (int i = 0; i < Affixes.Count; i++)
                if (Affixes[i].Stat == s) return string.Format(Affixes[i].Text, v);
            return "+" + v + " " + s;
        }

        public static EquipSlot DefaultEquipSlot(ItemSlot s)
        {
            switch (s)
            {
                case ItemSlot.Brush: return EquipSlot.Brush;
                case ItemSlot.Palette: return EquipSlot.Palette;
                case ItemSlot.Hat: return EquipSlot.Hat;
                case ItemSlot.Coat: return EquipSlot.Coat;
                case ItemSlot.Gloves: return EquipSlot.Gloves;
                case ItemSlot.Boots: return EquipSlot.Boots;
                case ItemSlot.Ring: return EquipSlot.Ring1;
                default: return EquipSlot.Amulet;
            }
        }
    }
}
