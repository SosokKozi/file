using System.Collections.Generic;
using NUnit.Framework;
using AshenCanvas.Sim.Combat;
using AshenCanvas.Sim.Core;
using AshenCanvas.Sim.Items;
using AshenCanvas.Sim.Progression;
using AshenCanvas.Sim.Quests;
using AshenCanvas.Sim.Run;
using AshenCanvas.Sim.Skills;
using AshenCanvas.Sim.Stats;
using AshenCanvas.Sim.World;

namespace AshenCanvas.Tests
{
    public class HeroAndQuestTests
    {
        [Test]
        public void NewGame_StartsInPrologue_WithBrush()
        {
            var gs = GameOps.NewGame(42);
            Assert.AreEqual(ZoneDb.PrologueId, gs.currentZone);
            Assert.IsNotNull(HeroOps.Equipped(gs.hero, EquipSlot.Brush));
            Assert.AreEqual(1, HeroOps.RankOf(gs.hero, "brush_stroke"));
            Assert.AreEqual(QuestStatus.Active, QuestOps.Status(gs, "q_awakening"));
            Assert.AreEqual(QuestStatus.Locked, QuestOps.Status(gs, "q_marsh"));
        }

        [Test]
        public void Leveling_UnlocksSkills_IntoHotbar()
        {
            var h = new HeroState();
            HeroOps.UnlockSkills(h);
            var unlocked = new List<string>();
            int gained = HeroOps.GainXp(h, 100000, unlocked);
            Assert.Greater(gained, 9);
            Assert.AreEqual(gained, h.skillPoints);
            foreach (var s in SkillDb.All) Assert.AreEqual(1, HeroOps.RankOf(h, s.Id), s.Id);
            Assert.AreEqual(SkillDb.All.Count - 1, unlocked.Count);
            Assert.AreEqual(6, h.hotbar.Count);
            Assert.IsFalse(h.hotbar.Contains(""));
        }

        [Test]
        public void SpendPoint_RaisesRank_UntilMax()
        {
            var h = new HeroState { skillPoints = 50 };
            HeroOps.UnlockSkills(h);
            int spent = 0;
            while (HeroOps.SpendPoint(h, "brush_stroke")) spent++;
            Assert.AreEqual(SkillDb.Get("brush_stroke").MaxRank - 1, spent);
        }

        [Test]
        public void Equip_SwapsWithInventory_AndRingsFillBothSlots()
        {
            var gs = GameOps.NewGame(1);
            var h = gs.hero;
            h.level = 20;
            var rng = new Rng(4);
            var ring1 = ItemGen.Generate(rng, 10, Rarity.Magic, b => b.Slot == ItemSlot.Ring);
            var ring2 = ItemGen.Generate(rng, 10, Rarity.Magic, b => b.Slot == ItemSlot.Ring);
            var brush = ItemGen.Generate(rng, 10, Rarity.Rare, b => b.Slot == ItemSlot.Brush);
            h.inventory.Add(ring1); h.inventory.Add(ring2); h.inventory.Add(brush);

            Assert.IsTrue(HeroOps.Equip(h, 0, out _));
            Assert.IsTrue(HeroOps.Equip(h, 0, out _));
            Assert.AreSame(ring1, HeroOps.Equipped(h, EquipSlot.Ring1));
            Assert.AreSame(ring2, HeroOps.Equipped(h, EquipSlot.Ring2));

            var oldBrush = HeroOps.Equipped(h, EquipSlot.Brush);
            Assert.IsTrue(HeroOps.Equip(h, 0, out _));
            Assert.AreSame(brush, HeroOps.Equipped(h, EquipSlot.Brush));
            Assert.AreSame(oldBrush, h.inventory[0]);
        }

        [Test]
        public void Equip_RespectsLevelRequirement()
        {
            var gs = GameOps.NewGame(1);
            var item = ItemGen.Generate(new Rng(2), 20, Rarity.Normal, b => b.Id == "sable_brush");
            gs.hero.inventory.Add(item);
            Assert.IsFalse(HeroOps.Equip(gs.hero, 0, out var err));
            StringAssert.Contains("18", err);
        }

        [Test]
        public void Stats_IncludeEquipment()
        {
            var gs = GameOps.NewGame(1);
            var st = HeroOps.ComputeStats(gs.hero);
            Assert.AreEqual(3f, st[Stat.WeaponMin]);
            Assert.Greater(st[Stat.Armor], 0f);
            Assert.AreEqual(60f, st[Stat.MaxLife]);
        }

        [Test]
        public void Prologue_BossKill_OpensHub_AndMarshQuest()
        {
            var gs = GameOps.NewGame(7);
            var rng = new Rng(1);
            var rewards = QuestOps.OnKill(gs, ZoneDb.PrologueId, true, rng);
            Assert.AreEqual(1, rewards.Count);
            Assert.AreEqual(QuestStatus.Done, QuestOps.Status(gs, "q_awakening"));
            GameOps.OnZoneBossKilled(gs, ZoneDb.Get(ZoneDb.PrologueId));
            Assert.IsTrue(gs.prologueDone);
            Assert.IsTrue(gs.IsUnlocked(ZoneDb.HubId));
            Assert.IsTrue(gs.IsUnlocked("rot_marsh"));
            Assert.AreEqual(QuestStatus.Available, QuestOps.Status(gs, "q_marsh"));
            Assert.AreEqual(1, QuestOps.ForGiver(gs, HubLayout.Gouache).Count);
        }

        [Test]
        public void KillCountQuest_CompletesAndPaysOut()
        {
            var gs = GameOps.NewGame(7);
            gs.Unlock("faded_forest");
            QuestOps.Refresh(gs);
            Assert.IsTrue(QuestOps.Accept(gs, "q_forest_hunt"));
            var rng = new Rng(3);
            for (int i = 0; i < 39; i++) QuestOps.OnKill(gs, "faded_forest", false, rng);
            Assert.AreEqual(QuestStatus.Active, QuestOps.Status(gs, "q_forest_hunt"));
            QuestOps.OnKill(gs, "faded_forest", false, rng);
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, QuestOps.Status(gs, "q_forest_hunt"));
            int gold = gs.hero.gold;
            var reward = QuestOps.TurnIn(gs, "q_forest_hunt", rng);
            Assert.IsNotNull(reward.Item);
            Assert.AreEqual(gold + 150, gs.hero.gold);
            Assert.AreEqual(QuestStatus.Done, QuestOps.Status(gs, "q_forest_hunt"));
        }

        [Test]
        public void InkQuest_NeedsMarshQuestFirst()
        {
            var gs = GameOps.NewGame(7);
            gs.Unlock("ink_catacombs");
            QuestOps.Refresh(gs);
            Assert.AreEqual(QuestStatus.Locked, QuestOps.Status(gs, "q_ink"));
        }

        [Test]
        public void ZoneSeed_ChangesPerVisit_ButIsStable()
        {
            var gs = GameOps.NewGame(100);
            ulong a = GameOps.ZoneSeed(gs, "rot_marsh");
            Assert.AreEqual(a, GameOps.ZoneSeed(gs, "rot_marsh"));
            gs.AddVisit("rot_marsh");
            Assert.AreNotEqual(a, GameOps.ZoneSeed(gs, "rot_marsh"));
        }

        [Test]
        public void Vendor_BuyAndSell()
        {
            var gs = GameOps.NewGame(5);
            gs.hero.gold = 10000;
            var stock = Vendor.Stock(new Rng(1), 5, VendorKind.Brushes);
            Assert.AreEqual(Vendor.StockSize, stock.Count);
            foreach (var it in stock) Assert.IsTrue(it.Base.Slot == ItemSlot.Brush || it.Base.Slot == ItemSlot.Palette);
            Assert.IsTrue(Vendor.Buy(gs, stock, 0, out _));
            Assert.AreEqual(Vendor.StockSize - 1, stock.Count);
            int gold = gs.hero.gold;
            Assert.IsTrue(Vendor.Sell(gs, 0));
            Assert.Greater(gs.hero.gold, gold);
        }

        [Test]
        public void Mitigation_IsCapped()
        {
            Assert.AreEqual(25f, DamageCalc.Mitigate(100f, Element.Crimson, 0f, 500f), 0.001f);
            Assert.AreEqual(160f, DamageCalc.Mitigate(100f, Element.Azure, 0f, -200f), 0.001f);
            Assert.AreEqual(25f, DamageCalc.Mitigate(100f, Element.Raw, 1e9f, 0f), 0.001f);
            Assert.AreEqual(100f, DamageCalc.Mitigate(100f, Element.Raw, 0f, 0f), 0.001f);
        }

        [Test]
        public void SkillDamage_GrowsWithRank()
        {
            var gs = GameOps.NewGame(5);
            var st = HeroOps.ComputeStats(gs.hero);
            st[Stat.CritChance] = 0;
            var s = SkillDb.Get("splatter");
            float r1 = DamageCalc.RollSkill(s, 1, st, new Rng(1)).Amount;
            float r5 = DamageCalc.RollSkill(s, 5, st, new Rng(1)).Amount;
            Assert.AreEqual(r1 * (1f + 4 * SkillDb.RankDamagePct / 100f), r5, 0.01f);
        }
    }
}
