using System;
using System.Collections.Generic;
using AshenCanvas.Sim.Items;

namespace AshenCanvas.Sim.Run
{
    [Serializable]
    public sealed class SkillRank
    {
        public string id;
        public int rank;
    }

    [Serializable]
    public sealed class EquippedItem
    {
        public int slot;   // (int)EquipSlot
        public Item item;
    }

    [Serializable]
    public sealed class QuestState
    {
        public string id;
        public int status;  // (int)QuestStatus
        public int progress;
    }

    [Serializable]
    public sealed class ZoneVisit
    {
        public string zoneId;
        public int visits;
    }

    [Serializable]
    public sealed class HeroState
    {
        public string name = "Мирра";
        public int level = 1;
        public int xp;
        public int skillPoints;
        public int gold;
        public int potions = 3;
        public List<SkillRank> skills = new List<SkillRank>();
        public List<Item> inventory = new List<Item>();
        public List<EquippedItem> equipment = new List<EquippedItem>();
        // Хотбар: навык на каждой из 6 клавиш (ЛКМ, ПКМ, Q, E, R, F).
        public List<string> hotbar = new List<string> { "brush_stroke", "", "", "", "", "" };
    }

    /// <summary>Всё, что сохраняется между сессиями. Только поля и списки — для JsonUtility.</summary>
    [Serializable]
    public sealed class GameState
    {
        public const int CurrentVersion = 1;
        public const int InventorySize = 40;
        public const int MaxPotions = 5;

        public int version = CurrentVersion;
        public long worldSeed;
        public bool prologueDone;
        public string currentZone;
        public long itemCounter;
        public HeroState hero = new HeroState();
        public List<QuestState> quests = new List<QuestState>();
        public List<string> unlockedZones = new List<string>();
        public List<ZoneVisit> visits = new List<ZoneVisit>();

        public int VisitsOf(string zoneId)
        {
            foreach (var v in visits) if (v.zoneId == zoneId) return v.visits;
            return 0;
        }

        public void AddVisit(string zoneId)
        {
            foreach (var v in visits) if (v.zoneId == zoneId) { v.visits++; return; }
            visits.Add(new ZoneVisit { zoneId = zoneId, visits = 1 });
        }

        public bool IsUnlocked(string zoneId) => unlockedZones.Contains(zoneId);

        public void Unlock(string zoneId)
        {
            if (!string.IsNullOrEmpty(zoneId) && !unlockedZones.Contains(zoneId)) unlockedZones.Add(zoneId);
        }
    }
}
