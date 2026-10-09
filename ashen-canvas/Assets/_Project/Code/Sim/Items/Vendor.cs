using System.Collections.Generic;
using AshenCanvas.Sim.Core;
using AshenCanvas.Sim.Run;

namespace AshenCanvas.Sim.Items
{
    public enum VendorKind { Brushes, Outfit }

    public static class Vendor
    {
        public const int StockSize = 8;

        public static int BuyPrice(Item item) => item.Value() * 3;
        public static int SellPrice(Item item) => item.Value();
        public static int PotionPrice(int heroLevel) => 15 + 2 * heroLevel;

        /// <summary>Товар обновляется при каждом возвращении в лагерь (новый seed).</summary>
        public static List<Item> Stock(Rng rng, int heroLevel, VendorKind kind)
        {
            var list = new List<Item>();
            for (int i = 0; i < StockSize; i++)
            {
                Rarity r = i == 0 ? Rarity.Rare : i < 4 ? Rarity.Magic : Rarity.Normal;
                int ilvl = heroLevel + rng.Range(0, 2);
                System.Func<ItemBase, bool> filter = kind == VendorKind.Brushes
                    ? (System.Func<ItemBase, bool>)(b => b.Slot == ItemSlot.Brush || b.Slot == ItemSlot.Palette)
                    : (b => b.Slot != ItemSlot.Brush && b.Slot != ItemSlot.Palette);
                list.Add(ItemGen.Generate(rng, ilvl, r, filter));
            }
            return list;
        }

        public static bool Buy(GameState gs, List<Item> stock, int index, out string error)
        {
            error = null;
            if (index < 0 || index >= stock.Count) return false;
            var item = stock[index];
            int price = BuyPrice(item);
            if (gs.hero.gold < price) { error = "Не хватает золота"; return false; }
            if (gs.hero.inventory.Count >= GameState.InventorySize) { error = "Сумка полна"; return false; }
            gs.hero.gold -= price;
            gs.hero.inventory.Add(item);
            stock.RemoveAt(index);
            return true;
        }

        public static bool Sell(GameState gs, int invIndex)
        {
            if (invIndex < 0 || invIndex >= gs.hero.inventory.Count) return false;
            gs.hero.gold += SellPrice(gs.hero.inventory[invIndex]);
            gs.hero.inventory.RemoveAt(invIndex);
            return true;
        }

        public static bool BuyPotion(GameState gs, out string error)
        {
            error = null;
            int price = PotionPrice(gs.hero.level);
            if (gs.hero.potions >= GameState.MaxPotions) { error = "Больше не унести"; return false; }
            if (gs.hero.gold < price) { error = "Не хватает золота"; return false; }
            gs.hero.gold -= price;
            gs.hero.potions++;
            return true;
        }
    }
}
