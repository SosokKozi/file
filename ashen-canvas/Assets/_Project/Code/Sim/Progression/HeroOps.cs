using System;
using System.Collections.Generic;
using AshenCanvas.Sim.Items;
using AshenCanvas.Sim.Run;
using AshenCanvas.Sim.Skills;
using AshenCanvas.Sim.Stats;

namespace AshenCanvas.Sim.Progression
{
    /// <summary>Уровни, навыки, характеристики и экипировка героини.</summary>
    public static class HeroOps
    {
        public const int MaxLevel = 40;
        public const int HotbarSize = 6;

        public static int XpToNext(int level) => (int)(30 + 25 * Math.Pow(level, 1.6));

        public static StatBlock ComputeStats(HeroState h)
        {
            var st = new StatBlock();
            int l = h.level;
            st[Stat.MaxLife] = 60 + 12 * (l - 1);
            st[Stat.MaxMana] = 40 + 6 * (l - 1);
            st[Stat.LifeRegen] = 1f + 0.1f * l;
            st[Stat.ManaRegen] = 4f + 0.25f * l;
            st[Stat.CritChance] = 5f;
            st[Stat.CritMulti] = 150f;
            bool hasBrush = false;
            foreach (var e in h.equipment)
            {
                if (e.item == null || e.item.IsEmpty) continue;
                e.item.AddStatsTo(st);
                if ((EquipSlot)e.slot == EquipSlot.Brush) hasBrush = true;
            }
            if (!hasBrush) { st[Stat.WeaponMin] += 2; st[Stat.WeaponMax] += 4; }
            return st;
        }

        /// <summary>Начисляет опыт. Возвращает число новых уровней; открытые навыки — в <paramref name="unlocked"/>.</summary>
        public static int GainXp(HeroState h, int amount, List<string> unlocked = null)
        {
            if (h.level >= MaxLevel) return 0;
            h.xp += amount;
            int gained = 0;
            while (h.level < MaxLevel && h.xp >= XpToNext(h.level))
            {
                h.xp -= XpToNext(h.level);
                h.level++;
                h.skillPoints++;
                gained++;
            }
            if (h.level >= MaxLevel) h.xp = 0;
            UnlockSkills(h, unlocked);
            return gained;
        }

        public static void UnlockSkills(HeroState h, List<string> unlocked = null)
        {
            foreach (var s in SkillDb.All)
            {
                if (s.UnlockLevel > h.level || RankOf(h, s.Id) > 0) continue;
                h.skills.Add(new SkillRank { id = s.Id, rank = 1 });
                unlocked?.Add(s.Id);
                while (h.hotbar.Count < HotbarSize) h.hotbar.Add("");
                int free = h.hotbar.IndexOf("");
                if (free >= 0) h.hotbar[free] = s.Id;
            }
        }

        public static int RankOf(HeroState h, string skillId)
        {
            foreach (var r in h.skills) if (r.id == skillId) return r.rank;
            return 0;
        }

        public static bool SpendPoint(HeroState h, string skillId)
        {
            var def = SkillDb.Get(skillId);
            if (def == null || h.skillPoints <= 0) return false;
            foreach (var r in h.skills)
            {
                if (r.id != skillId) continue;
                if (r.rank >= def.MaxRank) return false;
                r.rank++;
                h.skillPoints--;
                return true;
            }
            return false;
        }

        public static Item Equipped(HeroState h, EquipSlot slot)
        {
            foreach (var e in h.equipment)
                if (e.slot == (int)slot && e.item != null && !e.item.IsEmpty) return e.item;
            return null;
        }

        static void SetEquipped(HeroState h, EquipSlot slot, Item item)
        {
            h.equipment.RemoveAll(e => e.slot == (int)slot);
            if (item != null) h.equipment.Add(new EquippedItem { slot = (int)slot, item = item });
        }

        public static bool AddToInventory(HeroState h, Item item)
        {
            if (h.inventory.Count >= GameState.InventorySize) return false;
            h.inventory.Add(item);
            return true;
        }

        /// <summary>Надевает предмет из сумки. Снятый предмет встаёт на его место.</summary>
        public static bool Equip(HeroState h, int invIndex, out string error)
        {
            error = null;
            if (invIndex < 0 || invIndex >= h.inventory.Count) { error = "Нет предмета"; return false; }
            var item = h.inventory[invIndex];
            if (item.Base.Level > h.level) { error = "Нужен уровень " + item.Base.Level; return false; }

            var slot = ItemDb.DefaultEquipSlot(item.Base.Slot);
            if (item.Base.Slot == ItemSlot.Ring && Equipped(h, EquipSlot.Ring1) != null && Equipped(h, EquipSlot.Ring2) == null)
                slot = EquipSlot.Ring2;

            var old = Equipped(h, slot);
            SetEquipped(h, slot, item);
            if (old != null) h.inventory[invIndex] = old;
            else h.inventory.RemoveAt(invIndex);
            return true;
        }

        public static bool Unequip(HeroState h, EquipSlot slot, out string error)
        {
            error = null;
            var item = Equipped(h, slot);
            if (item == null) return false;
            if (h.inventory.Count >= GameState.InventorySize) { error = "Сумка полна"; return false; }
            SetEquipped(h, slot, null);
            h.inventory.Add(item);
            return true;
        }
    }
}
