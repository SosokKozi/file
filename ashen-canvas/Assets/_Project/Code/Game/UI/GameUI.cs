using System.Collections.Generic;
using AshenCanvas.Game.Art;
using AshenCanvas.Game.Boot;
using AshenCanvas.Game.Cam;
using AshenCanvas.Game.Fx;
using AshenCanvas.Game.Input;
using AshenCanvas.Game.World;
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
    /// <summary>
    /// Весь интерфейс прототипа на IMGUI: HUD, окна, подписи лута, тексты урона.
    /// Виртуальный экран высотой 1080 точек; ширина зависит от пропорций.
    /// Позже переносится на UI Toolkit (см. навык ui-hud-inventory).
    /// </summary>
    public sealed partial class GameUI : MonoBehaviour
    {
        public enum Panel { Inventory, Character, Skills }

        const float VH = 1080f;

        bool showInventory, showCharacter, showSkills, showPause, showDeath, showWaypoint;
        string npcId;          // открыт разговор
        VendorKind? vendor;    // открыт торговец

        float scale = 1f, vw = 1920f;
        readonly List<Rect> blockers = new List<Rect>();
        readonly List<Rect> frameBlockers = new List<Rect>();

        sealed class Msg { public string Text; public Color Color; public float Time; public bool Big; }
        readonly List<Msg> messages = new List<Msg>();

        string zoneTitle, zoneIntro;
        float zoneTitleTime = -100f;
        Texture2D minimap;
        Item tooltipItem;
        string tooltipPrefix;
        Vector2 tooltipAt;

        GUIStyle label, labelC, labelR, small, smallC, slotText, title, big, button, window;
        bool stylesReady;

        public bool BlocksGameplay => showPause || showDeath || showWaypoint || npcId != null;

        public void Message(string text, Color c, bool bigText = false)
        {
            messages.Add(new Msg { Text = text, Color = c, Time = Time.unscaledTime, Big = bigText });
            if (messages.Count > 6) messages.RemoveAt(0);
        }

        public void OnZoneEntered(ZoneDef z, int level)
        {
            zoneTitle = z.Name + (z.Kind == ZoneKind.Hub ? "" : "  ·  ур. " + level);
            zoneIntro = z.Intro;
            zoneTitleTime = Time.unscaledTime;
            BuildMinimap(GameRoot.I.Map, z);
        }

        public void CloseAll()
        {
            npcId = null; vendor = null; showWaypoint = false; showDeath = false;
        }

        public void Toggle(Panel p)
        {
            if (showPause || showDeath) return;
            switch (p)
            {
                case Panel.Inventory: showInventory = !showInventory; if (!showInventory) vendor = null; break;
                case Panel.Character: showCharacter = !showCharacter; if (showCharacter) showSkills = false; break;
                case Panel.Skills: showSkills = !showSkills; if (showSkills) showCharacter = false; break;
            }
        }

        public void HandleEscape()
        {
            var root = GameRoot.I;
            if (showDeath) return;
            if (showPause) { showPause = false; root.SetMenuPause(false); return; }
            if (npcId != null || vendor != null || showWaypoint || showInventory || showCharacter || showSkills)
            {
                npcId = null; vendor = null; showWaypoint = false; showInventory = showCharacter = showSkills = false;
                return;
            }
            showPause = true;
            root.SetMenuPause(true);
        }

        public void OpenNpc(string id) { npcId = id; vendor = null; }
        public void OpenWaypoint() => showWaypoint = true;
        public void ShowDeath() { CloseAll(); showDeath = true; }

        /// <summary>Курсор над окном или подписью — клики мышью не превращаются в удары.</summary>
        public bool PointerOverUi(Vector2 screen)
        {
            var p = new Vector2(screen.x / scale, (Screen.height - screen.y) / scale);
            foreach (var r in blockers) if (r.Contains(p)) return true;
            return BlocksGameplay;
        }

        // ---------- Отрисовка ----------

        void OnGUI()
        {
            EnsureStyles();
            scale = Screen.height / VH;
            vw = Screen.width / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            frameBlockers.Clear();
            tooltipItem = null;

            var root = GameRoot.I;
            if (root == null) return;
            if (!root.InGame) { DrawTitle(); Commit(); return; }

            DrawWorldLabels(root);
            DrawHud(root);
            if (showCharacter) DrawCharacter(root);
            if (showSkills) DrawSkills(root);
            if (showInventory || vendor != null) DrawInventory(root);
            if (vendor != null) DrawVendor(root);
            if (npcId != null) DrawNpc(root);
            if (showWaypoint) DrawWaypoint(root);
            if (showPause) DrawPause(root);
            if (showDeath) DrawDeath(root);
            DrawTooltip(root);
            Commit();
        }

        void Commit()
        {
            if (Event.current.type == EventType.Repaint)
            {
                blockers.Clear();
                blockers.AddRange(frameBlockers);
            }
        }

        Rect Block(Rect r) { frameBlockers.Add(r); return r; }

        void EnsureStyles()
        {
            if (stylesReady) return;
            stylesReady = true;
            label = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true, richText = true };
            label.normal.textColor = Color.white;
            labelC = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            labelR = new GUIStyle(label) { alignment = TextAnchor.MiddleRight };
            small = new GUIStyle(label) { fontSize = 16 };
            smallC = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            slotText = new GUIStyle(small) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            title = new GUIStyle(labelC) { fontSize = 34, fontStyle = FontStyle.Bold };
            big = new GUIStyle(labelC) { fontSize = 76, fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUI.skin.button) { fontSize = 20, wordWrap = true };
            window = new GUIStyle(GUI.skin.box);
        }

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Frame(Rect r, Color c, float w = 2f)
        {
            Fill(new Rect(r.x, r.y, r.width, w), c);
            Fill(new Rect(r.x, r.yMax - w, r.width, w), c);
            Fill(new Rect(r.x, r.y, w, r.height), c);
            Fill(new Rect(r.xMax - w, r.y, w, r.height), c);
        }

        void Text(Rect r, string text, GUIStyle s, Color c)
        {
            var old = GUI.color;
            // Тень для читаемости поверх ярких эффектов.
            GUI.color = new Color(0, 0, 0, c.a * 0.8f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, s);
            GUI.color = c;
            GUI.Label(r, text, s);
            GUI.color = old;
        }

        Rect Window(Rect r, string caption)
        {
            Block(r);
            Fill(r, new Color(0.09f, 0.07f, 0.12f, 0.94f));
            Frame(r, new Color(1f, 0.6f, 0.3f, 0.8f));
            Fill(new Rect(r.x, r.y, r.width, 44), new Color(0.2f, 0.12f, 0.25f, 1f));
            Text(new Rect(r.x, r.y, r.width, 44), caption, labelC, Mats.Hex(0xFFD08A));
            return new Rect(r.x + 16, r.y + 54, r.width - 32, r.height - 70);
        }

        bool Button(Rect r, string text)
        {
            Block(r);
            return GUI.Button(r, text, button);
        }

        Vector2 ToGui(Vector3 world, out bool visible)
        {
            var cam = CameraRig.I != null ? CameraRig.I.Camera : Camera.main;
            var sp = cam.WorldToScreenPoint(world);
            visible = sp.z > 0f;
            return new Vector2(sp.x / scale, (Screen.height - sp.y) / scale);
        }

        // ---------- Титульный экран ----------

        void DrawTitle()
        {
            Fill(new Rect(0, 0, vw, VH), new Color(0.06f, 0.05f, 0.08f, 1f));
            for (int i = 0; i < Mats.Paint.Length; i++)
            {
                var c = Mats.Paint[i]; c.a = 0.85f;
                float x = vw * 0.5f - 420 + i * 120 + Mathf.Sin(Time.unscaledTime * 1.5f + i) * 10f;
                var old = GUI.color; GUI.color = c;
                GUI.DrawTexture(new Rect(x, 250 + Mathf.Cos(Time.unscaledTime + i) * 12f, 110, 110), Mats.Circle);
                GUI.color = old;
            }
            Text(new Rect(0, 380, vw, 100), "ПЕПЕЛЬНЫЙ ХОЛСТ", big, Mats.Hex(0xF4EBDC));
            Text(new Rect(0, 480, vw, 40), "Серый Куратор выпил краски мира. Верните их — кистью.", labelC, Mats.Hex(0xBFB8C8));

            var root = GameRoot.I;
            float bx = vw * 0.5f - 160;
            if (Button(new Rect(bx, 580, 320, 64), "Новая игра")) root.StartNewGame();
            GUI.enabled = SaveSystem.HasSave;
            if (Button(new Rect(bx, 660, 320, 64), "Продолжить")) root.ContinueGame();
            GUI.enabled = true;
            Text(new Rect(0, 960, vw, 80),
                "WASD — бег · мышь — прицел · ЛКМ, ПКМ, Q, E, R, F — навыки · 1 — зелье\nПробел — говорить и подбирать · I — сумка · C — героиня · K — навыки · Esc — меню",
                smallC, Mats.Hex(0x9A94A6));
        }

        // ---------- HUD ----------

        void DrawHud(GameRoot root)
        {
            var hero = root.Hero;
            var h = root.State.hero;
            var st = hero.Stats;

            Orb(new Vector2(130, VH - 130), 105, hero.Life / Mathf.Max(1f, st[Stat.MaxLife]), Mats.Hex(0xE8283A),
                Mathf.CeilToInt(hero.Life) + " / " + Mathf.RoundToInt(st[Stat.MaxLife]));
            Orb(new Vector2(vw - 130, VH - 130), 105, hero.Mana / Mathf.Max(1f, st[Stat.MaxMana]), Mats.Hex(0x2F6BFF),
                Mathf.FloorToInt(hero.Mana) + " / " + Mathf.RoundToInt(st[Stat.MaxMana]));

            // Опыт.
            var xpRect = new Rect(270, VH - 22, vw - 540, 10);
            Fill(xpRect, new Color(0, 0, 0, 0.6f));
            float xpk = h.level >= HeroOps.MaxLevel ? 1f : h.xp / (float)HeroOps.XpToNext(h.level);
            Fill(new Rect(xpRect.x, xpRect.y, xpRect.width * xpk, xpRect.height), Mats.Hex(0xFFC928));
            Text(new Rect(xpRect.x, xpRect.y - 26, xpRect.width, 24), "Уровень " + h.level + (h.skillPoints > 0 ? "   ·   очки навыков: " + h.skillPoints + " (K)" : ""), smallC, Mats.Hex(0xFFE08A));

            // Навыки.
            const float slot = 84f, gap = 8f;
            float total = HeroOps.HotbarSize * (slot + gap) + slot + gap;
            float x0 = vw * 0.5f - total * 0.5f, y0 = VH - 140;
            var potionRect = Block(new Rect(x0, y0, slot, slot));
            Fill(potionRect, new Color(0.1f, 0.06f, 0.1f, 0.85f));
            Frame(potionRect, Mats.Hex(0xFF7A8A));
            Text(new Rect(potionRect.x + 6, potionRect.y + 2, 30, 22), "1", small, Color.white);
            Text(potionRect, "Зелье\n" + h.potions + "/" + GameState.MaxPotions, smallC, Mats.Hex(0xFF9AA8));
            for (int i = 0; i < HeroOps.HotbarSize; i++)
            {
                var r = Block(new Rect(x0 + (i + 1) * (slot + gap), y0, slot, slot));
                string id = i < h.hotbar.Count ? h.hotbar[i] : "";
                var def = string.IsNullOrEmpty(id) ? null : SkillDb.Get(id);
                Fill(r, new Color(0.08f, 0.07f, 0.12f, 0.85f));
                if (def != null)
                {
                    var c = def.Element == Element.Raw ? Mats.Hex(0xF4EBDC) : Mats.Hex(Names.ColorOf(def.Element));
                    Fill(new Rect(r.x + 4, r.y + 4, r.width - 8, r.height - 8), new Color(c.r, c.g, c.b, 0.25f));
                    Text(new Rect(r.x + 2, r.y + 18, r.width - 4, r.height - 18), def.Name, slotText, c);
                    float left = hero.CooldownLeft(id);
                    float cdFull = SkillDb.CooldownFor(def, st);
                    if (left > 0f)
                    {
                        float k = Mathf.Clamp01(left / Mathf.Max(0.01f, cdFull));
                        Fill(new Rect(r.x, r.y + r.height * (1f - k), r.width, r.height * k), new Color(0, 0, 0, 0.6f));
                        if (left > 0.6f) Text(r, left.ToString("0.0"), labelC, Color.white);
                    }
                    if (hero.Mana < def.ManaCost) Fill(r, new Color(0.1f, 0.2f, 0.8f, 0.35f));
                    Frame(r, c);
                }
                else Frame(r, new Color(1, 1, 1, 0.2f));
                Text(new Rect(r.x + 6, r.y + 2, 60, 22), GameInput.SkillKeyNames[i], small, Mats.Hex(0xFFD08A));
            }

            DrawMinimap(root);
            DrawQuestTracker(root);
            DrawBossBar(root);
            DrawMessages();

            // Название зоны при входе.
            float zt = Time.unscaledTime - zoneTitleTime;
            if (zt < 6f)
            {
                float a = Mathf.Clamp01(zt * 2f) * Mathf.Clamp01((6f - zt) * 0.8f);
                Text(new Rect(0, 150, vw, 60), zoneTitle, title, new Color(1f, 0.92f, 0.8f, a));
                if (!string.IsNullOrEmpty(zoneIntro)) Text(new Rect(vw * 0.5f - 450, 210, 900, 70), zoneIntro, labelC, new Color(0.85f, 0.8f, 0.9f, a));
            }
            else Text(new Rect(0, 8, vw, 34), zoneTitle, labelC, new Color(1f, 0.92f, 0.8f, 0.85f));

            // Подсказка у предмета, с которым можно говорить.
            var it = root.NearestInteractable();
            if (it != null && !BlocksGameplay)
            {
                var p = ToGui(it.transform.position + Vector3.up * 3.2f, out bool vis);
                if (vis) Text(new Rect(p.x - 200, p.y - 20, 400, 40), "Пробел — " + it.Label, labelC, Mats.Hex(0xFFE08A));
            }

            if (root.State.currentZone == ZoneDb.PrologueId && root.State.hero.level <= 2)
                Text(new Rect(0, VH - 200, vw, 26), "WASD — бег · ЛКМ — мазок · Пробел — подобрать · I — сумка · K — навыки", smallC, new Color(1, 1, 1, 0.6f));
        }

        void Orb(Vector2 c, float r, float k, Color color, string text)
        {
            k = Mathf.Clamp01(k);
            var rect = Block(new Rect(c.x - r, c.y - r, r * 2, r * 2));
            var old = GUI.color;
            GUI.color = new Color(0.05f, 0.03f, 0.06f, 0.9f);
            GUI.DrawTexture(new Rect(rect.x - 6, rect.y - 6, rect.width + 12, rect.height + 12), Mats.Circle);
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y + rect.height * (1f - k), rect.width, rect.height * k), Mats.Circle, new Rect(0f, 0f, 1f, k));
            GUI.color = new Color(1, 1, 1, 0.18f);
            GUI.DrawTexture(new Rect(rect.x + r * 0.35f, rect.y + r * 0.2f, r * 0.6f, r * 0.45f), Mats.Circle);
            GUI.color = old;
            Text(new Rect(rect.x, c.y - 14, rect.width, 28), text, smallC, Color.white);
        }

        void BuildMinimap(DungeonLayout map, ZoneDef z)
        {
            if (minimap != null) Destroy(minimap);
            minimap = new Texture2D(map.W, map.H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var floor = Color.Lerp(Mats.Hex(z.Palette.Floor), Color.white, 0.35f); floor.a = 0.75f;
            var px = new Color32[map.W * map.H];
            for (int y = 0; y < map.H; y++)
                for (int x = 0; x < map.W; x++)
                    px[y * map.W + x] = map.IsFloor(x, y) ? (Color32)floor : new Color32(0, 0, 0, 0);
            minimap.SetPixels32(px);
            minimap.Apply();
        }

        void DrawMinimap(GameRoot root)
        {
            if (minimap == null) return;
            var map = root.Map;
            float size = 230f;
            float k = size / Mathf.Max(map.W, map.H);
            var r = Block(new Rect(vw - size - 20, 50, map.W * k, map.H * k));
            Fill(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color(0, 0, 0, 0.45f));
            GUI.DrawTexture(r, minimap);
            Dot(r, k, root.Hero.Pos, Mats.Hex(0xFFE08A), 8);
            foreach (var it in root.Interactables) if (it != null) Dot(r, k, it.transform.position, Mats.Hex(0x2FB8FF), 7);
            if (root.Boss != null) Dot(r, k, root.Boss.Pos, Mats.Hex(0xFF3B4A), 9);
        }

        void Dot(Rect r, float k, Vector3 world, Color c, float s)
        {
            float x = r.x + world.x / DungeonLayout.TileSize * k;
            float y = r.yMax - world.z / DungeonLayout.TileSize * k;
            var old = GUI.color; GUI.color = c;
            GUI.DrawTexture(new Rect(x - s * 0.5f, y - s * 0.5f, s, s), Mats.Circle);
            GUI.color = old;
        }

        void DrawQuestTracker(GameRoot root)
        {
            float y = 300f;
            foreach (var def in QuestDb.All)
            {
                var s = QuestOps.StateOf(root.State, def.Id);
                var st = (QuestStatus)s.status;
                if (st != QuestStatus.Active && st != QuestStatus.ReadyToTurnIn) continue;
                string line = def.Title;
                if (st == QuestStatus.ReadyToTurnIn) line += " — сдать";
                else if (def.Objective == Objective.KillCount) line += " — " + s.progress + "/" + def.Count;
                else line += " — " + EnemyName(ZoneDb.Get(def.ZoneId).BossId) + " (" + ZoneDb.Get(def.ZoneId).Name + ")";
                Text(new Rect(vw - 420, y, 400, 50), line, small, st == QuestStatus.ReadyToTurnIn ? Mats.Hex(0x9BFF6A) : Mats.Hex(0xFFE08A));
                y += 50;
            }
        }

        static string EnemyName(string id) => string.IsNullOrEmpty(id) ? "" : Sim.Enemies.EnemyDb.Get(id).Name;

        void DrawBossBar(GameRoot root)
        {
            var b = root.Boss;
            if (b == null || !b.Aggro) return;
            var r = new Rect(vw * 0.5f - 400, 60, 800, 26);
            Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(0, 0, 0, 0.7f));
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(b.Life / b.MaxLife), r.height), Mats.Hex(0xC8283A));
            Text(new Rect(r.x, r.y - 2, r.width, r.height + 4), b.Def.Name, smallC, Color.white);
        }

        void DrawMessages()
        {
            float y = 300f;
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                var m = messages[i];
                float age = Time.unscaledTime - m.Time;
                if (age > 4.5f) { messages.RemoveAt(i); continue; }
            }
            foreach (var m in messages)
            {
                float age = Time.unscaledTime - m.Time;
                var c = m.Color; c.a = Mathf.Clamp01(4.5f - age);
                Text(new Rect(0, y, vw, m.Big ? 44 : 32), m.Text, m.Big ? title : labelC, c);
                y += m.Big ? 48 : 34;
            }
        }

        // ---------- Подписи над миром ----------

        void DrawWorldLabels(GameRoot root)
        {
            foreach (var e in root.Enemies)
            {
                if (e == null || e.Dead || e.IsBoss) continue;
                bool elite = e.Rarity != Sim.Enemies.MonsterRarity.Normal;
                if (!elite && e.Life >= e.MaxLife) continue;
                var p = ToGui(e.Pos + Vector3.up * (2.2f * e.Def.Size + 0.3f), out bool vis);
                if (!vis) continue;
                float w = elite ? 110 : 70;
                Fill(new Rect(p.x - w * 0.5f - 1, p.y - 1, w + 2, 9), new Color(0, 0, 0, 0.7f));
                Fill(new Rect(p.x - w * 0.5f, p.y, w * Mathf.Clamp01(e.Life / e.MaxLife), 7), Mats.Hex(0xE8283A));
                if (elite)
                {
                    var c = e.Rarity == Sim.Enemies.MonsterRarity.Rare ? Mats.Hex(0xFFE35A) : Mats.Hex(0x7A9CFF);
                    Text(new Rect(p.x - 150, p.y - 26, 300, 24), e.DisplayName, smallC, c);
                }
            }

            // Лут: подписи как кнопки, без наложения друг на друга.
            var placed = new List<Rect>();
            foreach (var d in root.Drops)
            {
                if (d == null) continue;
                if ((d.transform.position - root.Hero.Pos).sqrMagnitude > 22f * 22f) continue;
                var p = ToGui(d.transform.position + Vector3.up * 0.6f, out bool vis);
                if (!vis) continue;
                string text = d.Label;
                float w = Mathf.Min(320f, 14f + text.Length * 10f);
                var r = new Rect(p.x - w * 0.5f, p.y - 26, w, 26);
                for (int guard = 0; guard < 12; guard++)
                {
                    bool hit = false;
                    foreach (var o in placed) if (o.Overlaps(r)) { hit = true; break; }
                    if (!hit) break;
                    r.y -= 28;
                }
                placed.Add(r);
                Block(r);
                Fill(r, new Color(0, 0, 0, 0.72f));
                Frame(r, d.LabelColor, 1f);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) root.TryPickUpByLabel(d);
                Text(r, text, smallC, d.LabelColor);
                if (d.Drop.Kind == DropKind.Item && r.Contains(Event.current.mousePosition)) SetTooltip(d.Drop.Item, null);
            }

            for (int i = 0; i < FxSystem.Texts.Count; i++)
            {
                var t = FxSystem.Texts[i];
                var p = ToGui(t.World, out bool vis);
                if (!vis) continue;
                var c = t.Color; c.a = Mathf.Clamp01((t.Life - t.Age) * 3f);
                Text(new Rect(p.x - 150, p.y - 20, 300, 40), t.Text, t.Big ? title : labelC, c);
            }
        }
    }
}
