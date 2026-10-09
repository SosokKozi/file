using System;
using System.Collections.Generic;
using AshenCanvas.Sim.Core;

namespace AshenCanvas.Sim.Items
{
    /// <summary>Генератор предметов. Один и тот же seed даёт один и тот же предмет.</summary>
    public static class ItemGen
    {
        static readonly string[] RareAdj = { "Пепельный", "Багряный", "Сумрачный", "Лазурный", "Безмолвный", "Чернильный", "Золотой", "Выцветший", "Яростный", "Звонкий", "Ржавый", "Колючий" };
        static readonly string[] RareNoun = { "Шёпот", "Мазок", "Холст", "Крик", "Рассвет", "Узор", "Штрих", "Сон", "Набросок", "Обет", "Блик", "Разлом" };

        /// <summary>Веса редкости: обычный, волшебный, редкий. Бонус редкости усиливает два последних.</summary>
        public static Rarity RollRarity(Rng rng, float itemFindPct, Rarity atLeast)
        {
            float k = 1f + Math.Max(0f, itemFindPct) / 100f;
            int normal = 70, magic = (int)(25 * k), rare = (int)(5 * k);
            if (atLeast >= Rarity.Magic) normal = 0;
            if (atLeast >= Rarity.Rare) magic = 0;
            int idx = rng.Weighted(new[] { normal, magic, rare });
            return (Rarity)idx;
        }

        public static List<ItemBase> BasesFor(int itemLevel, Func<ItemBase, bool> filter = null)
        {
            var list = new List<ItemBase>();
            foreach (var b in ItemDb.Bases)
                if (b.Level <= itemLevel && (filter == null || filter(b))) list.Add(b);
            return list;
        }

        public static Item Generate(Rng rng, int itemLevel, Rarity rarity, Func<ItemBase, bool> baseFilter = null)
        {
            itemLevel = Math.Max(1, itemLevel);
            if (rarity == Rarity.Unique)
            {
                var u = TryUnique(rng, itemLevel, baseFilter);
                if (u != null) return u;
                rarity = Rarity.Rare;
            }

            var bases = BasesFor(itemLevel, baseFilter);
            if (bases.Count == 0) bases = BasesFor(itemLevel);
            // Свежие основы чаще: вес растёт с уровнем основы, но старые не исчезают.
            var weights = new int[bases.Count];
            for (int i = 0; i < bases.Count; i++) weights[i] = 4 + bases[i].Level;
            var b = bases[rng.Weighted(weights)];

            var item = NewItem(rng, b, itemLevel, rarity);
            int prefixes = 0, suffixes = 0;
            if (rarity == Rarity.Magic)
            {
                int n = rng.Range(1, 2);
                if (n == 2) { prefixes = 1; suffixes = 1; }
                else if (rng.Chance(50)) prefixes = 1; else suffixes = 1;
            }
            else if (rarity == Rarity.Rare)
            {
                int n = rng.Range(3, 6);
                prefixes = Math.Min(3, rng.Range(n - 3, n));
                prefixes = Math.Max(prefixes, n - 3);
                suffixes = n - prefixes;
            }
            AddAffixes(rng, item, b, itemLevel, true, prefixes);
            AddAffixes(rng, item, b, itemLevel, false, suffixes);
            item.name = NameFor(rng, item, b);
            return item;
        }

        static Item NewItem(Rng rng, ItemBase b, int itemLevel, Rarity rarity)
        {
            var item = new Item
            {
                uid = (long)(rng.NextU64() >> 1),
                baseId = b.Id,
                rarity = (int)rarity,
                itemLevel = itemLevel,
            };
            foreach (var imp in b.Implicits)
                item.implicits.Add(new StatRoll { stat = (int)imp.Stat, value = rng.Range(imp.Min, imp.Max), tier = 0 });
            return item;
        }

        static Item TryUnique(Rng rng, int itemLevel, Func<ItemBase, bool> baseFilter)
        {
            var pool = new List<UniqueDef>();
            foreach (var u in ItemDb.Uniques)
            {
                var b = ItemDb.GetBase(u.BaseId);
                if (b.Level <= itemLevel && (baseFilter == null || baseFilter(b))) pool.Add(u);
            }
            if (pool.Count == 0) return null;
            var def = rng.Pick(pool);
            var item = NewItem(rng, ItemDb.GetBase(def.BaseId), itemLevel, Rarity.Unique);
            item.uniqueId = def.Id;
            item.name = def.Name;
            foreach (var s in def.Stats)
                item.affixes.Add(new StatRoll { affix = "", stat = (int)s.Stat, value = rng.Range(s.Min, s.Max), tier = 0 });
            return item;
        }

        static void AddAffixes(Rng rng, Item item, ItemBase b, int itemLevel, bool prefix, int count)
        {
            for (int k = 0; k < count; k++)
            {
                var pool = new List<AffixDef>();
                foreach (var a in ItemDb.Affixes)
                {
                    if (a.Prefix != prefix || !a.Allows(b.Slot) || a.Tiers[0].ItemLevel > itemLevel) continue;
                    bool taken = false;
                    foreach (var r in item.affixes) if (r.affix == a.Id) { taken = true; break; }
                    if (!taken) pool.Add(a);
                }
                if (pool.Count == 0) return;
                var def = rng.Pick(pool);

                int top = 0;
                for (int t = 0; t < def.Tiers.Length; t++) if (def.Tiers[t].ItemLevel <= itemLevel) top = t;
                // Лучший доступный ярус не гарантирован: берём один из двух верхних.
                int tierIdx = Math.Max(0, top - rng.NextInt(2));
                var tier = def.Tiers[tierIdx];
                item.affixes.Add(new StatRoll
                {
                    affix = def.Id,
                    stat = (int)def.Stat,
                    value = rng.Range(tier.Min, tier.Max),
                    tier = def.Tiers.Length - tierIdx,
                });
            }
        }

        static string NameFor(Rng rng, Item item, ItemBase b)
        {
            switch (item.Rarity)
            {
                case Rarity.Magic:
                    if (item.affixes.Count == 0) return b.Name;
                    return b.Name + " " + ItemDb.GetAffix(item.affixes[0].affix).Word;
                case Rarity.Rare:
                    return rng.Pick(RareAdj) + " " + rng.Pick(RareNoun);
                default:
                    return b.Name;
            }
        }
    }
}
