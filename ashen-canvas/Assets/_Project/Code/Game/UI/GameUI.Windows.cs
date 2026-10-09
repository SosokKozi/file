using AshenCanvas.Game.Art;
using AshenCanvas.Game.Boot;
using AshenCanvas.Game.Input;
using AshenCanvas.Sim.Items;
using AshenCanvas.Sim.Progression;
using AshenCanvas.Sim.Quests;
using AshenCanvas.Sim.Run;
using AshenCanvas.Sim.Skills;
using AshenCanvas.Sim.Stats;
using AshenCanvas.Sim.World;
using UnityEngine;

namespace AshenCanvas.Game.UI
{
    public sealed partial class GameUI
    {
        static readonly string[] SlotNames = { "Кисть", "Палитра", "Шляпа", "Одежда", "Перчатки", "Обувь", "Кольцо", "Кольцо", "Амулет" };

        static string SlotWord(ItemSlot s)
        {
            switch (s)
            {
                case ItemSlot.Brush: return "Кисть";
                case ItemSlot.Palette: return "Палитра";
                case ItemSlot.Hat: return "Шляпа";
                case ItemSlot.Coat: return "Одежда";
                case ItemSlot.Gloves: return "Перчатки";
                case ItemSlot.Boots: return "Обувь";
                case ItemSlot.Ring: return "Кольцо";
                default: return "Амулет";
            }
        }

        void SetTooltip(Item item, string prefix)
        {
            tooltipItem = item;
            tooltipPrefix = prefix;
            tooltipAt = Event.current.mousePosition;
        }

        // ---------- Сумка и экипировка ----------

        void DrawInventory(GameRoot root)
        {
            var h = root.State.hero;
            var inner = Window(new Rect(vw - 560, 40, 540, 900), vendor != null ? "Сумка — нажмите, чтобы продать" : "Сумка (I)");
            const float es = 100f, eh = 74f;
            EquipSlot[,] grid =
            {
                { EquipSlot.Brush, EquipSlot.Hat, EquipSlot.Palette },
                { EquipSlot.Gloves, EquipSlot.Coat, EquipSlot.Boots },
                { EquipSlot.Ring1, EquipSlot.Amulet, EquipSlot.Ring2 },
            };
            float gx = inner.x + (inner.width - 3 * (es + 10)) * 0.5f;
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                {
                    var slot = grid[row, col];
                    var r = new Rect(gx + col * (es + 10), inner.y + row * (eh + 10), es, eh);
                    var item = HeroOps.Equipped(h, slot);
                    if (ItemCell(r, item, SlotNames[(int)slot]) && item != null)
                    {
                        if (HeroOps.Unequip(h, slot, out var err)) root.OnEquipmentChanged();
                        else if (err != null) Message(err, Mats.Hex(0xFF7A7A));
                    }
                    if (item != null && r.Contains(Event.current.mousePosition)) SetTooltip(item, "Надето");
                }

            float iy = inner.y + 3 * (eh + 10) + 16;
            Text(new Rect(inner.x, iy, inner.width, 28), "Золото: " + h.gold + "     Зелья: " + h.potions + "/" + GameState.MaxPotions, label, Mats.Hex(0xFFC928));
            iy += 36;
            const int cols = 8;
            const float cs = 60f;
            float ix = inner.x + (inner.width - cols * (cs + 4)) * 0.5f;
            for (int i = 0; i < GameState.InventorySize; i++)
            {
                var r = new Rect(ix + (i % cols) * (cs + 4), iy + (i / cols) * (cs + 4), cs, cs);
                var item = i < h.inventory.Count ? h.inventory[i] : null;
                if (ItemCell(r, item, "") && item != null)
                {
                    if (vendor != null)
                    {
                        int price = Vendor.SellPrice(item);
                        Vendor.Sell(root.State, i);
                        Message("Продано: +" + price + " золота", Mats.Hex(0xFFC928));
                    }
                    else if (HeroOps.Equip(h, i, out var err)) root.OnEquipmentChanged();
                    else if (err != null) Message(err, Mats.Hex(0xFF7A7A));
                    break;
                }
                if (item != null && r.Contains(Event.current.mousePosition))
                    SetTooltip(item, vendor != null ? "Продать за " + Vendor.SellPrice(item) + " золота" : "Нажмите, чтобы надеть");
            }
        }

        /// <summary>Клетка предмета: рамка цвета редкости, слово слота. true — по клетке щёлкнули.</summary>
        bool ItemCell(Rect r, Item item, string emptyText)
        {
            Block(r);
            Fill(r, new Color(0.14f, 0.11f, 0.18f, 1f));
            if (item == null)
            {
                Frame(r, new Color(1, 1, 1, 0.12f), 1f);
                if (!string.IsNullOrEmpty(emptyText)) Text(r, emptyText, smallC, new Color(1, 1, 1, 0.3f));
                return false;
            }
            var c = Mats.Hex(Item.RarityColor(item.Rarity));
            Fill(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), new Color(c.r, c.g, c.b, 0.18f));
            Frame(r, c, 2f);
            bool tooHigh = item.Base.Level > GameRoot.I.State.hero.level;
            Text(r, SlotWord(item.Base.Slot) + "\n" + item.itemLevel, smallC, tooHigh ? Mats.Hex(0xFF7A7A) : c);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        void DrawTooltip(GameRoot root)
        {
            if (tooltipItem == null) return;
            var item = tooltipItem;
            var lines = item.Describe();
            float w = 380f, lineH = 24f;
            float hgt = 64 + lines.Count * lineH + (string.IsNullOrEmpty(tooltipPrefix) ? 0 : 30);
            string flavor = null;
            if (item.Rarity == Rarity.Unique) { flavor = ItemDb.GetUnique(item.uniqueId).Flavor; hgt += 50; }
            float x = tooltipAt.x + 24, y = tooltipAt.y + 10;
            if (x + w > vw) x = tooltipAt.x - w - 24;
            if (y + hgt > VH) y = VH - hgt;
            var r = new Rect(x, y, w, hgt);
            var c = Mats.Hex(Item.RarityColor(item.Rarity));
            Fill(r, new Color(0.05f, 0.04f, 0.07f, 0.96f));
            Frame(r, c);
            Text(new Rect(r.x, r.y + 6, w, 28), item.name, labelC, c);
            Text(new Rect(r.x, r.y + 32, w, 22), Item.RarityName(item.Rarity) + " · " + item.Base.Name + " · ур. " + item.itemLevel, smallC, new Color(1, 1, 1, 0.6f));
            float ly = r.y + 60;
            var me = root.State.hero;
            foreach (var l in lines)
            {
                var lc = l.StartsWith("Требуемый") && item.Base.Level > me.level ? Mats.Hex(0xFF7A7A) : Mats.Hex(0x9AB8FF);
                Text(new Rect(r.x + 14, ly, w - 28, lineH), l, small, lc);
                ly += lineH;
            }
            if (flavor != null) { Text(new Rect(r.x + 14, ly, w - 28, 46), flavor, small, Mats.Hex(0xFF9A4A)); ly += 50; }
            if (!string.IsNullOrEmpty(tooltipPrefix)) Text(new Rect(r.x + 14, ly + 2, w - 28, 26), tooltipPrefix, small, Mats.Hex(0xFFE08A));
        }

        // ---------- Героиня ----------

        void DrawCharacter(GameRoot root)
        {
            var h = root.State.hero;
            var st = root.Hero.Stats;
            var inner = Window(new Rect(20, 40, 460, 820), h.name + " — героиня (C)");
            float y = inner.y;
            void Row(string name, string value)
            {
                Text(new Rect(inner.x, y, inner.width - 120, 26), name, small, Mats.Hex(0xD8D0E0));
                Text(new Rect(inner.x + inner.width - 160, y, 160, 26), value, labelR, Color.white);
                y += 28;
            }
            Row("Уровень", h.level.ToString());
            Row("Опыт", h.level >= HeroOps.MaxLevel ? "макс." : h.xp + " / " + HeroOps.XpToNext(h.level));
            y += 8;
            Row("Здоровье", Mathf.RoundToInt(st[Stat.MaxLife]).ToString());
            Row("Мана", Mathf.RoundToInt(st[Stat.MaxMana]).ToString());
            Row("Здоровье в секунду", st[Stat.LifeRegen].ToString("0.0"));
            Row("Мана в секунду", st[Stat.ManaRegen].ToString("0.0"));
            Row("Броня", Mathf.RoundToInt(st[Stat.Armor]).ToString());
            y += 8;
            float wMin = st[Stat.WeaponMin] + st[Stat.WeaponFlat], wMax = st[Stat.WeaponMax] + st[Stat.WeaponFlat];
            Row("Урон кисти", Mathf.RoundToInt(wMin) + "–" + Mathf.RoundToInt(wMax));
            Row("Урон", "+" + st[Stat.DamagePct] + "%");
            Row("Алый / лазурь", "+" + st[Stat.CrimsonDamagePct] + "% / +" + st[Stat.AzureDamagePct] + "%");
            Row("Золото / чернила", "+" + st[Stat.GoldenDamagePct] + "% / +" + st[Stat.InkDamagePct] + "%");
            Row("Шанс крит. удара", st[Stat.CritChance] + "%");
            Row("Сила крит. удара", st[Stat.CritMulti] + "%");
            Row("Скорость навыков", "+" + st[Stat.CastSpeedPct] + "%");
            Row("Область навыков", "+" + st[Stat.AreaPct] + "%");
            Row("Перезарядка", "+" + st[Stat.CooldownPct] + "%");
            y += 8;
            Row("Защита от алого", Mathf.Min(75, st[Stat.ResCrimson]) + "%");
            Row("Защита от лазури", Mathf.Min(75, st[Stat.ResAzure]) + "%");
            Row("Защита от золота", Mathf.Min(75, st[Stat.ResGolden]) + "%");
            Row("Защита от чернил", Mathf.Min(75, st[Stat.ResInk]) + "%");
            y += 8;
            Row("Скорость бега", "+" + st[Stat.MoveSpeedPct] + "%");
            Row("Здоровье за убийство", st[Stat.LifeOnKill].ToString());
            Row("Редкость находок", "+" + st[Stat.ItemFindPct] + "%");
        }

        // ---------- Навыки ----------

        void DrawSkills(GameRoot root)
        {
            var h = root.State.hero;
            var inner = Window(new Rect(20, 40, 640, 940), "Навыки (K) — очков: " + h.skillPoints);
            float y = inner.y;
            foreach (var s in SkillDb.All)
            {
                int rank = HeroOps.RankOf(h, s.Id);
                bool open = rank > 0;
                var c = s.Element == Element.Raw ? Mats.Hex(0xF4EBDC) : Mats.Hex(Names.ColorOf(s.Element));
                var row = new Rect(inner.x, y, inner.width, 116);
                Fill(row, new Color(1, 1, 1, open ? 0.05f : 0.02f));
                Text(new Rect(row.x + 8, row.y + 4, 360, 28), s.Name + (open ? "  " + rank + "/" + s.MaxRank : ""), label, open ? c : new Color(c.r, c.g, c.b, 0.4f));
                Text(new Rect(row.x + 8, row.y + 32, row.width - 70, 26), s.Description, small, new Color(1, 1, 1, open ? 0.8f : 0.35f));
                Text(new Rect(row.x + 8, row.y + 56, row.width - 70, 24),
                    "Мана " + s.ManaCost + " · перезарядка " + s.Cooldown + " с · " + Names.Of(s.Element), small, new Color(1, 1, 1, 0.5f));
                if (!open)
                {
                    Text(new Rect(row.x + row.width - 220, row.y + 4, 210, 28), "с " + s.UnlockLevel + " уровня", labelR, new Color(1, 1, 1, 0.5f));
                }
                else
                {
                    GUI.enabled = h.skillPoints > 0 && rank < s.MaxRank;
                    if (Button(new Rect(row.x + row.width - 56, row.y + 6, 48, 48), "+"))
                    {
                        HeroOps.SpendPoint(h, s.Id);
                        Message(s.Name + ": ранг " + HeroOps.RankOf(h, s.Id), c);
                    }
                    GUI.enabled = true;
                    for (int k = 0; k < HeroOps.HotbarSize; k++)
                    {
                        bool here = k < h.hotbar.Count && h.hotbar[k] == s.Id;
                        var br = new Rect(row.x + 8 + k * 66, row.y + 82, 60, 28);
                        if (here) Fill(br, new Color(c.r, c.g, c.b, 0.5f));
                        if (Button(br, GameInput.SkillKeyNames[k])) Assign(h, k, s.Id);
                    }
                }
                y += 124;
            }
        }

        static void Assign(HeroState h, int slot, string id)
        {
            while (h.hotbar.Count < HeroOps.HotbarSize) h.hotbar.Add("");
            int old = h.hotbar.IndexOf(id);
            if (old >= 0) h.hotbar[old] = h.hotbar[slot];   // меняем местами
            h.hotbar[slot] = id;
        }

        // ---------- Лагерь ----------

        void DrawNpc(GameRoot root)
        {
            var info = NpcInfo.Get(npcId);
            var quests = QuestOps.ForGiver(root.State, npcId);
            float hgt = 260 + quests.Count * 170 + (info.Vendor != null ? 70 : 0);
            var inner = Window(new Rect(vw * 0.5f - 380, VH * 0.5f - hgt * 0.5f, 760, hgt), info.Name);
            float y = inner.y;
            Text(new Rect(inner.x, y, inner.width, 80), info.Greeting, label, Mats.Hex(0xF4EBDC));
            y += 90;
            foreach (var q in quests)
            {
                var st = QuestOps.Status(root.State, q.Id);
                Text(new Rect(inner.x, y, inner.width, 28), "«" + q.Title + "»", label, Mats.Hex(0xFFE08A));
                y += 30;
                string say = st == QuestStatus.Available ? q.Offer : st == QuestStatus.Active ? q.Progress : q.Complete;
                Text(new Rect(inner.x, y, inner.width, 80), say, small, Mats.Hex(0xD8D0E0));
                y += 84;
                if (st == QuestStatus.Available && Button(new Rect(inner.x, y, 220, 44), "Принять"))
                {
                    QuestOps.Accept(root.State, q.Id);
                    Message("Новое задание: " + q.Title, Mats.Hex(0xFFE08A));
                }
                if (st == QuestStatus.ReadyToTurnIn && Button(new Rect(inner.x, y, 220, 44), "Сдать задание")) root.TurnInQuest(q.Id);
                y += 54;
            }
            if (info.Vendor != null && Button(new Rect(inner.x, y, 220, 50), "Торговать"))
            {
                vendor = info.Vendor;
                npcId = null;
                showInventory = true;
                return;
            }
            if (Button(new Rect(inner.xMax - 160, inner.yMax - 44, 160, 44), "Уйти")) npcId = null;
        }

        void DrawVendor(GameRoot root)
        {
            var stock = vendor == VendorKind.Brushes ? root.StockBrushes : root.StockOutfit;
            var name = vendor == VendorKind.Brushes ? "Мастер Сангина" : "Лис Индиго";
            var inner = Window(new Rect(20, 40, 560, 860), name + " — товар");
            float y = inner.y;
            Text(new Rect(inner.x, y, inner.width, 28), "Золото: " + root.State.hero.gold, label, Mats.Hex(0xFFC928));
            y += 40;
            for (int i = 0; i < stock.Count; i++)
            {
                var item = stock[i];
                var r = new Rect(inner.x, y, inner.width, 64);
                var c = Mats.Hex(Item.RarityColor(item.Rarity));
                Fill(r, new Color(c.r, c.g, c.b, 0.1f));
                Frame(r, c, 1f);
                Text(new Rect(r.x + 10, r.y + 4, r.width - 140, 28), item.name, label, c);
                Text(new Rect(r.x + 10, r.y + 32, r.width - 140, 24), SlotWord(item.Base.Slot) + " · ур. " + item.Base.Level, small, new Color(1, 1, 1, 0.6f));
                Text(new Rect(r.xMax - 130, r.y, 120, r.height), Vendor.BuyPrice(item) + " з.", labelR, Mats.Hex(0xFFC928));
                Block(r);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    if (Vendor.Buy(root.State, stock, i, out var err)) Message("Куплено: " + item.name, c);
                    else if (err != null) Message(err, Mats.Hex(0xFF7A7A));
                    break;
                }
                if (r.Contains(Event.current.mousePosition)) SetTooltip(item, "Купить за " + Vendor.BuyPrice(item) + " золота");
                y += 70;
            }
            if (vendor == VendorKind.Outfit)
            {
                int price = Vendor.PotionPrice(root.State.hero.level);
                if (Button(new Rect(inner.x, y + 6, inner.width, 50), "Зелье здоровья — " + price + " золота"))
                {
                    if (Vendor.BuyPotion(root.State, out var err)) Message("Зелье куплено", Mats.Hex(0xFF9AA8));
                    else Message(err, Mats.Hex(0xFF7A7A));
                }
            }
            if (Button(new Rect(inner.xMax - 160, inner.yMax - 44, 160, 44), "Закрыть")) vendor = null;
        }

        void DrawWaypoint(GameRoot root)
        {
            var inner = Window(new Rect(vw * 0.5f - 340, 200, 680, 560), "Мольберт путей");
            float y = inner.y;
            Text(new Rect(inner.x, y, inner.width, 30), "Каждый путь рисуется заново: новая карта при каждом походе.", small, Mats.Hex(0xD8D0E0));
            y += 44;
            foreach (var z in ZoneDb.All)
            {
                if (z.Kind != ZoneKind.Wild) continue;
                bool open = root.State.IsUnlocked(z.Id);
                int lvl = ZoneDb.LevelForVisit(z, root.State.VisitsOf(z.Id));
                GUI.enabled = open;
                string text = open ? z.Name + "  ·  ур. " + lvl : z.Name + "  ·  закрыто";
                if (Button(new Rect(inner.x, y, inner.width, 64), text))
                {
                    showWaypoint = false;
                    root.EnterZone(z.Id);
                    GUI.enabled = true;
                    return;
                }
                GUI.enabled = true;
                y += 74;
            }
            if (Button(new Rect(inner.xMax - 160, inner.yMax - 44, 160, 44), "Закрыть")) showWaypoint = false;
        }

        // ---------- Пауза и смерть ----------

        void DrawPause(GameRoot root)
        {
            Fill(new Rect(0, 0, vw, VH), new Color(0, 0, 0, 0.5f));
            var inner = Window(new Rect(vw * 0.5f - 200, VH * 0.5f - 170, 400, 320), "Пауза");
            if (Button(new Rect(inner.x, inner.y, inner.width, 56), "Продолжить")) HandleEscape();
            if (Button(new Rect(inner.x, inner.y + 70, inner.width, 56), "Сохранить"))
            {
                SaveSystem.Save(root.State);
                Message("Сохранено", Mats.Hex(0x9BFF6A));
            }
            if (Button(new Rect(inner.x, inner.y + 140, inner.width, 56), "В главное меню"))
            {
                showPause = false;
                root.QuitToTitle();
            }
        }

        void DrawDeath(GameRoot root)
        {
            Fill(new Rect(0, 0, vw, VH), new Color(0.05f, 0.04f, 0.06f, 0.75f));
            Text(new Rect(0, VH * 0.5f - 140, vw, 90), "Краски поблекли…", big, Mats.Hex(0xBFB8C8));
            string where = root.State.prologueDone ? "Вернуться в лагерь" : "Очнуться в часовне";
            Block(new Rect(vw * 0.5f - 180, VH * 0.5f, 360, 64));
            if (GUI.Button(new Rect(vw * 0.5f - 180, VH * 0.5f, 360, 64), where, button))
            {
                showDeath = false;
                root.Respawn();
            }
        }
    }
}
