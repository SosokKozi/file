using System;
using System.Collections.Generic;
using AshenCanvas.Sim.Core;
using AshenCanvas.Sim.Enemies;

namespace AshenCanvas.Sim.Items
{
    public enum DropKind { Gold, Potion, Item }

    public sealed class Drop
    {
        public DropKind Kind;
        public int Amount;   // золото или зелья
        public Item Item;
    }

    public static class Loot
    {
        public const int UniqueChanceDivisor = 40;

        /// <summary>Что выпадет из врага. Только от seed — повторяемо для призраков и проверок.</summary>
        public static List<Drop> Roll(Rng rng, int monsterLevel, MonsterRarity r, float itemFindPct)
        {
            var drops = new List<Drop>();

            int goldChance = r == MonsterRarity.Normal ? 35 : 100;
            if (rng.Chance(goldChance))
            {
                int g = rng.Range(2, 5) + monsterLevel * rng.Range(1, 3);
                if (r >= MonsterRarity.Rare) g *= 3;
                drops.Add(new Drop { Kind = DropKind.Gold, Amount = g });
            }
            if (rng.Chance(r == MonsterRarity.Normal ? 6 : r == MonsterRarity.Magic ? 20 : 60))
                drops.Add(new Drop { Kind = DropKind.Potion, Amount = 1 });

            int items;
            switch (r)
            {
                case MonsterRarity.Normal: items = rng.Chance(12) ? 1 : 0; break;
                case MonsterRarity.Magic: items = rng.Chance(45) ? 1 : 0; break;
                case MonsterRarity.Rare: items = rng.Range(1, 2); break;
                default: items = rng.Range(3, 4); break;
            }

            for (int i = 0; i < items; i++)
            {
                Rarity atLeast = Rarity.Normal;
                if (r == MonsterRarity.Boss) atLeast = i == 0 ? Rarity.Rare : Rarity.Magic;
                else if (r == MonsterRarity.Rare && i == 0) atLeast = Rarity.Magic;

                Rarity rar = rng.NextInt(UniqueChanceDivisor) == 0 && r != MonsterRarity.Normal
                    ? Rarity.Unique
                    : ItemGen.RollRarity(rng, itemFindPct + (r == MonsterRarity.Rare ? 100 : 0), atLeast);
                int ilvl = monsterLevel + (r == MonsterRarity.Boss ? 2 : 0);
                drops.Add(new Drop { Kind = DropKind.Item, Item = ItemGen.Generate(rng, ilvl, rar) });
            }
            return drops;
        }
    }
}
