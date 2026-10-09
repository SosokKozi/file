using System.Collections.Generic;
using NUnit.Framework;
using AshenCanvas.Sim.Core;
using AshenCanvas.Sim.Enemies;
using AshenCanvas.Sim.Items;

namespace AshenCanvas.Tests
{
    public class ItemTests
    {
        [Test]
        public void SameSeed_SameItem()
        {
            var a = ItemGen.Generate(new Rng(5), 10, Rarity.Rare);
            var b = ItemGen.Generate(new Rng(5), 10, Rarity.Rare);
            Assert.AreEqual(a.baseId, b.baseId);
            Assert.AreEqual(a.name, b.name);
            Assert.AreEqual(a.affixes.Count, b.affixes.Count);
            for (int i = 0; i < a.affixes.Count; i++) Assert.AreEqual(a.affixes[i].value, b.affixes[i].value);
        }

        [Test]
        public void AffixCounts_MatchRarity_AndRulesHold()
        {
            var rng = new Rng(99);
            for (int i = 0; i < 3000; i++)
            {
                int ilvl = 1 + i % 30;
                var rar = (Rarity)(i % 3);
                var item = ItemGen.Generate(rng, ilvl, rar);
                var b = item.Base;
                Assert.LessOrEqual(b.Level, ilvl);
                int pre = 0, suf = 0;
                var seen = new HashSet<string>();
                foreach (var r in item.affixes)
                {
                    var def = ItemDb.GetAffix(r.affix);
                    Assert.IsTrue(seen.Add(r.affix), "повтор " + r.affix);
                    Assert.IsTrue(def.Allows(b.Slot), def.Id + " на " + b.Slot);
                    var tier = def.Tiers[def.Tiers.Length - r.tier];
                    Assert.LessOrEqual(tier.ItemLevel, ilvl);
                    Assert.That(r.value, Is.InRange(tier.Min, tier.Max));
                    if (def.Prefix) pre++; else suf++;
                }
                Assert.LessOrEqual(pre, 3);
                Assert.LessOrEqual(suf, 3);
                if (rar == Rarity.Normal) Assert.AreEqual(0, item.affixes.Count);
                if (rar == Rarity.Magic) { Assert.That(item.affixes.Count, Is.InRange(1, 2)); Assert.LessOrEqual(pre, 1); Assert.LessOrEqual(suf, 1); }
                if (rar == Rarity.Rare) Assert.That(item.affixes.Count, Is.InRange(3, 6));
                Assert.IsFalse(string.IsNullOrEmpty(item.name));
            }
        }

        [Test]
        public void Unique_UsesItsBase()
        {
            var rng = new Rng(3);
            for (int i = 0; i < 50; i++)
            {
                var item = ItemGen.Generate(rng, 20, Rarity.Unique);
                Assert.AreEqual(Rarity.Unique, item.Rarity);
                var def = ItemDb.GetUnique(item.uniqueId);
                Assert.AreEqual(def.BaseId, item.baseId);
                Assert.AreEqual(def.Name, item.name);
            }
        }

        [Test]
        public void Unique_TooLowLevel_FallsBackToRare()
        {
            var item = ItemGen.Generate(new Rng(1), 1, Rarity.Unique);
            Assert.AreEqual(Rarity.Rare, item.Rarity);
        }

        [Test]
        public void Boss_DropsRareOrBetter()
        {
            for (ulong s = 1; s < 200; s++)
            {
                var drops = Loot.Roll(new Rng(s), 5, MonsterRarity.Boss, 0);
                var items = drops.FindAll(d => d.Kind == DropKind.Item);
                Assert.GreaterOrEqual(items.Count, 3);
                Assert.GreaterOrEqual((int)items[0].Item.Rarity, (int)Rarity.Rare);
            }
        }

        [Test]
        public void NormalMonsters_DropItemsSometimes()
        {
            var rng = new Rng(11);
            int items = 0;
            for (int i = 0; i < 2000; i++)
                foreach (var d in Loot.Roll(rng, 3, MonsterRarity.Normal, 0))
                    if (d.Kind == DropKind.Item) items++;
            Assert.That(items, Is.InRange(160, 320)); // около 12%
        }

        [Test]
        public void Describe_ListsEveryStat()
        {
            var item = ItemGen.Generate(new Rng(8), 15, Rarity.Rare);
            var lines = item.Describe();
            Assert.GreaterOrEqual(lines.Count, item.affixes.Count + item.implicits.Count + 1);
        }
    }
}
