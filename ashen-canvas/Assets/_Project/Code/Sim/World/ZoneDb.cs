using System;
using System.Collections.Generic;

namespace AshenCanvas.Sim.World
{
    public enum ZoneKind { Prologue, Hub, Wild }

    public enum LayoutStyle { Chain, Branching, Hub }

    /// <summary>Цвета зоны в формате 0xRRGGBB.</summary>
    public sealed class ZonePalette
    {
        public uint Floor, Wall, Accent, Fog, Ambient, Light;
        public float LightIntensity = 1f;
        public float FogDensity = 0.02f;
        public bool HeroLantern;    // тёплый свет вокруг героини в тёмных зонах
    }

    public sealed class ZoneDef
    {
        public string Id, Name, Intro;
        public ZoneKind Kind;
        public LayoutStyle Style;
        public int Level;
        public int Size = 64;                 // для Branching: ширина и высота сетки
        public int Rooms, RoomMin, RoomMax, BossRoomSize = 13, Loops;
        public int PacksMin, PacksMax, PackSizeMin, PackSizeMax;
        public int MagicPct, RarePct;
        public int MinBossDistance = 20;      // клеток по полу от старта до босса
        public string[] Roster = new string[0];
        public string BossId;
        public string Unlocks;                // какая зона открывается после босса
        public PropKind[] PropKinds = new PropKind[0];
        public ZonePalette Palette;
    }

    public static class ZoneDb
    {
        public const string PrologueId = "ash_chapel";
        public const string HubId = "rainbow_camp";

        public static readonly IReadOnlyList<ZoneDef> All = new List<ZoneDef>
        {
            new ZoneDef {
                Id = PrologueId, Name = "Пепельная часовня", Kind = ZoneKind.Prologue, Style = LayoutStyle.Chain, Level = 1,
                Intro = "Серый Куратор выпил краски мира. Мирра просыпается в пепле с одной кистью в руке.",
                Rooms = 6, RoomMin = 6, RoomMax = 9, BossRoomSize = 13,
                PacksMin = 1, PacksMax = 1, PackSizeMin = 1, PackSizeMax = 3, MagicPct = 0, RarePct = 0, MinBossDistance = 30,
                Roster = new[] { "faded_husk", "faded_husk", "grey_spitter" }, BossId = "grey_herald", Unlocks = "rot_marsh",
                PropKinds = new[] { PropKind.Pillar, PropKind.Candle, PropKind.Rubble },
                Palette = new ZonePalette { Floor = 0x2A2730, Wall = 0x45404E, Accent = 0xFF7A3A, Fog = 0x0E0C12, Ambient = 0x1C1A24,
                    Light = 0x8C8AA8, LightIntensity = 0.25f, FogDensity = 0.045f, HeroLantern = true } },

            new ZoneDef {
                Id = HubId, Name = "Лагерь «Радуга»", Kind = ZoneKind.Hub, Style = LayoutStyle.Hub, Level = 1,
                Intro = "Последнее место, где ещё помнят цвета.",
                PropKinds = new[] { PropKind.Lamp, PropKind.Easel, PropKind.Bucket, PropKind.Crate },
                Palette = new ZonePalette { Floor = 0xC9A66B, Wall = 0x7A5C3E, Accent = 0xFF5FA2, Fog = 0xFFD9A8, Ambient = 0x8A7A6A,
                    Light = 0xFFF1D6, LightIntensity = 1.15f, FogDensity = 0.006f } },

            new ZoneDef {
                Id = "rot_marsh", Name = "Гнилые топи", Kind = ZoneKind.Wild, Style = LayoutStyle.Branching, Level = 3, Size = 60,
                Intro = "Тина глотает краску и пускает пузыри серого.",
                Rooms = 11, RoomMin = 7, RoomMax = 12, Loops = 2,
                PacksMin = 1, PacksMax = 3, PackSizeMin = 2, PackSizeMax = 5, MagicPct = 12, RarePct = 5,
                Roster = new[] { "faded_husk", "grey_spitter", "mud_charger" }, BossId = "mire_matron", Unlocks = "faded_forest",
                PropKinds = new[] { PropKind.Tree, PropKind.Rubble, PropKind.Bucket },
                Palette = new ZonePalette { Floor = 0x4E6B4A, Wall = 0x2F4A3A, Accent = 0x9BFF6A, Fog = 0x56705E, Ambient = 0x40584A,
                    Light = 0xD8F0C8, LightIntensity = 0.8f, FogDensity = 0.02f } },

            new ZoneDef {
                Id = "faded_forest", Name = "Выцветший лес", Kind = ZoneKind.Wild, Style = LayoutStyle.Branching, Level = 7, Size = 68,
                Intro = "Деревья нарисованы одним карандашом. Волки — тоже.",
                Rooms = 13, RoomMin = 7, RoomMax = 13, Loops = 3,
                PacksMin = 2, PacksMax = 3, PackSizeMin = 2, PackSizeMax = 6, MagicPct = 14, RarePct = 6,
                Roster = new[] { "faded_husk", "mud_charger", "ink_bomb", "sketch_wolf" }, BossId = "old_sepia", Unlocks = "ink_catacombs",
                PropKinds = new[] { PropKind.Tree, PropKind.Tree, PropKind.Rubble, PropKind.Easel },
                Palette = new ZonePalette { Floor = 0xB8A88A, Wall = 0x6E5E4A, Accent = 0xFF9A3A, Fog = 0xD8CDB4, Ambient = 0x8A7E6A,
                    Light = 0xFFF4DC, LightIntensity = 1f, FogDensity = 0.015f } },

            new ZoneDef {
                Id = "ink_catacombs", Name = "Чернильные катакомбы", Kind = ZoneKind.Wild, Style = LayoutStyle.Branching, Level = 11, Size = 72,
                Intro = "Здесь пролили всю чернильницу мира. Она всё ещё капает.",
                Rooms = 15, RoomMin = 6, RoomMax = 11, Loops = 4,
                PacksMin = 2, PacksMax = 4, PackSizeMin = 3, PackSizeMax = 6, MagicPct = 16, RarePct = 8,
                Roster = new[] { "ink_bomb", "grey_spitter", "sketch_wolf", "eraser_knight" }, BossId = "grand_eraser", Unlocks = null,
                PropKinds = new[] { PropKind.Pillar, PropKind.Candle, PropKind.Crate },
                Palette = new ZonePalette { Floor = 0x24203A, Wall = 0x3C2E5E, Accent = 0x8A3CFF, Fog = 0x140F24, Ambient = 0x2A2240,
                    Light = 0xB8A8FF, LightIntensity = 0.45f, FogDensity = 0.035f, HeroLantern = true } },
        };

        public static ZoneDef Get(string id)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Id == id) return All[i];
            throw new ArgumentException("Нет зоны " + id);
        }

        /// <summary>Уровень врагов при N-м заходе: зона понемногу растёт, но не больше чем на 3.</summary>
        public static int LevelForVisit(ZoneDef z, int visit) => z.Level + Math.Min(visit, 3);
    }
}
