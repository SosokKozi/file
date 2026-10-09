using System.Collections.Generic;
using AshenCanvas.Game.Art;
using AshenCanvas.Game.Cam;
using AshenCanvas.Game.Enemies;
using AshenCanvas.Game.Fx;
using AshenCanvas.Game.Input;
using AshenCanvas.Game.Player;
using AshenCanvas.Game.UI;
using AshenCanvas.Game.World;
using AshenCanvas.Sim.Combat;
using AshenCanvas.Sim.Core;
using AshenCanvas.Sim.Enemies;
using AshenCanvas.Sim.Items;
using AshenCanvas.Sim.Progression;
using AshenCanvas.Sim.Quests;
using AshenCanvas.Sim.Run;
using AshenCanvas.Sim.Skills;
using AshenCanvas.Sim.Stats;
using AshenCanvas.Sim.World;
using UnityEngine;

namespace AshenCanvas.Game.Boot
{
    /// <summary>
    /// Корень игры: хранит состояние, собирает зоны, раздаёт урон, опыт и лут.
    /// Создаётся сам при нажатии Play в любой сцене (см. <see cref="AutoBoot"/>).
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot I { get; private set; }

        const float AutoPickupRadius = 1.8f;
        const float ItemPickupRadius = 2.6f;
        const float LabelPickupRadius = 7f;

        public GameState State;
        public ZoneDef Zone;
        public DungeonLayout Map;
        public FlowField Flow;
        public int ZoneLevel;
        public ulong ZoneSeed;
        public Transform ZoneRoot;
        public Hero Hero;
        public GameUI UI;
        public Enemy Boss;
        public Rng CombatRng, LootRng;
        public List<Item> StockBrushes = new List<Item>(), StockOutfit = new List<Item>();

        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<LootDrop> Drops = new List<LootDrop>();
        public readonly List<Interactable> Interactables = new List<Interactable>();

        public bool InGame { get; private set; }
        public bool MenuPaused { get; private set; }
        public bool Paused => !InGame || MenuPaused || (Hero != null && Hero.Dead);

        Light lantern;
        Cell flowCell = new Cell(-1, -1);
        float flowTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (FindAnyObjectByType<GameRoot>() == null) new GameObject("GameRoot").AddComponent<GameRoot>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            // Свет и камера из шаблона сцены не нужны: зона ставит свой свет.
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) l.enabled = false;
            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.gameObject.AddComponent<AudioListener>();
            }
            if (cam.GetComponent<CameraRig>() == null) cam.gameObject.AddComponent<CameraRig>();
            cam.backgroundColor = Mats.Hex(0x0E0C12);

            new GameObject("Fx").AddComponent<FxSystem>().transform.SetParent(transform, false);
            UI = gameObject.AddComponent<GameUI>();
        }

        // ---------- Начало игры ----------

        public void StartNewGame()
        {
            // Seed мира выбирается один раз при создании игры; дальше всё выводится из него.
            State = GameOps.NewGame(System.DateTime.UtcNow.Ticks & 0x7FFFFFFFFFFF);
            BeginSession();
            EnterZone(ZoneDb.PrologueId);
        }

        public void ContinueGame()
        {
            State = SaveSystem.Load();
            if (State == null) { StartNewGame(); return; }
            BeginSession();
            EnterZone(State.prologueDone ? ZoneDb.HubId : ZoneDb.PrologueId);
        }

        void BeginSession()
        {
            InGame = true;
            SetMenuPause(false);
            if (Hero != null) Destroy(Hero.gameObject);
            var go = new GameObject("Hero");
            go.transform.SetParent(transform, false);
            Hero = go.AddComponent<Hero>();
            Hero.Init();
            lantern = new GameObject("Lantern").AddComponent<Light>();
            lantern.transform.SetParent(go.transform, false);
            lantern.transform.localPosition = new Vector3(0f, 3f, 0f);
            lantern.type = LightType.Point;
            lantern.color = Mats.Hex(0xFFB070);
            lantern.range = 13f;
            lantern.intensity = 2.2f;
            lantern.shadows = LightShadows.None;
            CameraRig.I.Target = Hero.transform;
        }

        public void QuitToTitle()
        {
            if (InGame) SaveSystem.Save(State);
            ClearZone();
            if (Hero != null) Destroy(Hero.gameObject);
            Hero = null;
            InGame = false;
            SetMenuPause(false);
        }

        public void SetMenuPause(bool on)
        {
            MenuPaused = on;
            Time.timeScale = on ? 0f : 1f;
        }

        // ---------- Зоны ----------

        public void EnterZone(string zoneId)
        {
            ClearZone();
            Zone = ZoneDb.Get(zoneId);
            State.currentZone = zoneId;
            int visits = State.VisitsOf(zoneId);
            ZoneSeed = GameOps.ZoneSeed(State, zoneId);
            ZoneLevel = Zone.Kind == ZoneKind.Wild ? ZoneDb.LevelForVisit(Zone, visits) : Zone.Level;
            Map = DungeonGen.Generate(Zone, ZoneSeed);
            if (Zone.Kind == ZoneKind.Wild) State.AddVisit(zoneId);
            CombatRng = new Rng(Hash.Combine(ZoneSeed, 11, 0));
            LootRng = new Rng(Hash.Combine(ZoneSeed, 12, 0));
            Flow = new FlowField(Map);
            flowCell = new Cell(-1, -1);

            ZoneRoot = new GameObject("Zone " + zoneId).transform;
            ZoneView.Build(Map, Zone, ZoneRoot, ZoneSeed);
            lantern.enabled = Zone.Palette.HeroLantern;

            DungeonLayout.CellToWorld(Map.Start, out float sx, out float sz);
            Hero.PlaceAt(new Vector3(sx, 0f, sz));
            Hero.RefreshStats(Zone.Kind == ZoneKind.Hub);
            CameraRig.I.Snap();

            if (Zone.Kind == ZoneKind.Hub) SetupHub();
            else SpawnMonsters();
            if (Zone.Kind == ZoneKind.Wild)
                SpawnInteractable("portal", "Портал в лагерь", CellOffset(Map.Start, 2, 0), () => EnterZone(ZoneDb.HubId));

            QuestOps.Refresh(State);
            UI.OnZoneEntered(Zone, ZoneLevel);
        }

        void ClearZone()
        {
            StopAllCoroutines();
            if (ZoneRoot != null) Destroy(ZoneRoot.gameObject);
            ZoneRoot = null;
            Enemies.Clear();
            Drops.Clear();
            Interactables.Clear();
            Boss = null;
            FxSystem.I?.Clear();
            UI?.CloseAll();
        }

        void SetupHub()
        {
            var rng = new Rng(Hash.Combine((ulong)State.worldSeed, 40, State.VisitsOf("hub_return")));
            State.AddVisit("hub_return");
            StockBrushes = Vendor.Stock(rng, State.hero.level, VendorKind.Brushes);
            StockOutfit = Vendor.Stock(rng, State.hero.level, VendorKind.Outfit);
            if (State.hero.potions < 3) State.hero.potions = 3;

            foreach (var n in Map.Npcs)
            {
                var info = NpcInfo.Get(n.Id);
                string id = n.Id;
                System.Action use = id == HubLayout.Waypoint ? (System.Action)(() => UI.OpenWaypoint()) : () => UI.OpenNpc(id);
                var it = SpawnInteractable(n.Id, info.Name, CellCenter(n.At), use);
                it.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            SaveSystem.Save(State);
        }

        void SpawnMonsters()
        {
            var rng = new Rng(Hash.Combine(ZoneSeed, 21, 0));
            foreach (var pack in Map.Packs)
            {
                var def = EnemyDb.Get(pack.EnemyId);
                RareMod mod = null;
                if (pack.RareModId != null)
                    foreach (var m in EnemyDb.RareMods) if (m.Id == pack.RareModId) mod = m;
                for (int i = 0; i < pack.Count; i++)
                {
                    var cell = pack.At;
                    for (int t = 0; t < 8; t++)
                    {
                        var c = new Cell(pack.At.X + rng.Range(-2, 2), pack.At.Y + rng.Range(-2, 2));
                        if (Map.IsFloor(c.X, c.Y)) { cell = c; break; }
                    }
                    var rarity = pack.Rarity == MonsterRarity.Rare ? (i == 0 ? MonsterRarity.Rare : MonsterRarity.Normal) : pack.Rarity;
                    var pos = CellCenter(cell) + new Vector3(rng.Range(-0.5f, 0.5f), 0f, rng.Range(-0.5f, 0.5f));
                    SpawnEnemy(def, ZoneLevel, rarity, rarity == MonsterRarity.Rare ? mod : null, pos);
                }
            }
            if (!string.IsNullOrEmpty(Map.BossId))
            {
                Boss = SpawnEnemy(EnemyDb.Get(Map.BossId), ZoneLevel + 1, MonsterRarity.Boss, null, CellCenter(Map.Boss));
                Boss.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }

        public Enemy SpawnEnemy(EnemyDef def, int level, MonsterRarity rarity, RareMod mod, Vector3 pos)
        {
            var go = new GameObject(def.Name);
            go.transform.SetParent(ZoneRoot, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var e = go.AddComponent<Enemy>();
            e.Init(def, level, rarity, mod);
            Enemies.Add(e);
            return e;
        }

        Interactable SpawnInteractable(string assetId, string label, Vector3 pos, System.Action use)
        {
            var go = new GameObject(label);
            go.transform.SetParent(ZoneRoot, false);
            go.transform.position = pos;
            AssetProvider.Spawn(assetId, go.transform).name = "Model";
            var it = go.AddComponent<Interactable>();
            it.Label = label;
            it.OnUse = use;
            Interactables.Add(it);
            return it;
        }

        static Vector3 CellCenter(Cell c)
        {
            DungeonLayout.CellToWorld(c, out float x, out float z);
            return new Vector3(x, 0f, z);
        }

        Vector3 CellOffset(Cell c, int dx, int dy)
        {
            var o = new Cell(c.X + dx, c.Y + dy);
            return CellCenter(Map.IsFloor(o.X, o.Y) ? o : c);
        }

        // ---------- Бой ----------

        public void AlertAround(Vector3 p, float r)
        {
            foreach (var e in Enemies)
                if (!e.Aggro && (e.Pos - p).sqrMagnitude < r * r) e.Aggro = true;
        }

        public void HitEnemy(Enemy e, SkillDef s, int rank, float mult)
        {
            if (e == null || e.Dead) return;
            var hit = DamageCalc.RollSkill(s, rank, Hero.Stats, CombatRng);
            float dmg = DamageCalc.MitigateFor(hit.Amount * mult, hit.Element, e.Defence);
            var c = hit.Crit ? Mats.Hex(0xFFE35A) : Color.white;
            FxSystem.Text(e.Pos + Vector3.up * e.Radius, Mathf.Max(1, Mathf.RoundToInt(dmg)).ToString() + (hit.Crit ? "!" : ""), c, hit.Crit);
            if (e.ApplyDamage(dmg)) KillEnemy(e);
        }

        public void KillEnemy(Enemy e)
        {
            if (!Enemies.Remove(e)) return;
            e.Dead = true;
            var h = State.hero;

            var unlocked = new List<string>();
            int xp = EnemyDb.XpAt(e.Def, e.Level, e.Rarity, h.level);
            int levels = HeroOps.GainXp(h, xp, unlocked);
            if (levels > 0) OnLevelUp(unlocked);
            Hero.Life = Mathf.Min(Hero.Stats[Stat.MaxLife], Hero.Life + Hero.Stats[Stat.LifeOnKill]);

            foreach (var d in Loot.Roll(LootRng, e.Level, e.Rarity, Hero.Stats[Stat.ItemFindPct]))
            {
                var off = Random.insideUnitCircle * 1.6f;
                var land = e.Pos + new Vector3(off.x, 0f, off.y);
                if (!Map.IsWalkableWorld(land.x, land.z)) land = e.Pos;
                land.y = 0f;
                Drops.Add(LootDrop.Spawn(d, e.Pos, land, ZoneRoot));
            }

            foreach (var r in QuestOps.OnKill(State, Zone.Id, e.IsBoss, LootRng)) ShowReward(r);

            // Враг лопается краской: мир получает цвет обратно.
            var c = Mats.RandomPaint();
            FxSystem.Explosion(e.Pos, c, Mathf.Max(1.4f, e.Radius * 3f), e.IsBoss ? 0.9f : 0.08f);

            if (e.IsBoss)
            {
                Boss = null;
                GameOps.OnZoneBossKilled(State, Zone);
                SpawnInteractable("portal", "Портал в лагерь", CellCenter(Map.Exit), () => EnterZone(ZoneDb.HubId));
                UI.Message("Победа: " + e.Def.Name, Mats.Hex(0xFFC928), true);
                if (!string.IsNullOrEmpty(Zone.Unlocks)) UI.Message("Открыт путь: " + ZoneDb.Get(Zone.Unlocks).Name, Mats.Hex(0x9BFF6A));
                for (int i = 0; i < 6; i++) FxSystem.Explosion(e.Pos + Random.insideUnitSphere * 3f, Mats.Paint[i % Mats.Paint.Length], 2f, 0f);
                SaveSystem.Save(State);
            }
            Destroy(e.gameObject);
        }

        void OnLevelUp(List<string> unlocked)
        {
            Hero.RefreshStats(true);
            UI.Message("Новый уровень: " + State.hero.level, Mats.Hex(0xFFC928), true);
            foreach (var id in unlocked) UI.Message("Новый навык: " + SkillDb.Get(id).Name + " (K)", Mats.Hex(0x2FB8FF));
            for (int i = 0; i < Mats.Paint.Length; i++)
                FxSystem.Splash(Hero.Pos, Mats.Paint[i], 6, 6f);
            FxSystem.Ring(Hero.Pos, Mats.Hex(0xFFC928), 3f, 0.5f);
        }

        void ShowReward(QuestReward r)
        {
            if (r == null) return;
            UI.Message("Задание выполнено: " + r.Quest.Title, Mats.Hex(0xFFC928), true);
            string tail = "+" + r.Xp + " опыта, +" + r.Gold + " золота";
            if (r.Item != null) tail += ", " + r.Item.name;
            UI.Message(tail, Color.white);
            if (r.LevelsGained > 0) OnLevelUp(new List<string>());
            Hero.RefreshStats(false);
        }

        public void TurnInQuest(string id)
        {
            ShowReward(QuestOps.TurnIn(State, id, LootRng));
            SaveSystem.Save(State);
        }

        public void HeroTakeDamage(float raw, Element el, Vector3 from)
        {
            if (Hero == null || Hero.Dead) return;
            float dmg = DamageCalc.MitigateFor(raw, el, Hero.Stats);
            FxSystem.Text(Hero.Pos, "-" + Mathf.Max(1, Mathf.RoundToInt(dmg)), Mats.Hex(0xFF5A5A));
            FxSystem.Splash(Hero.Pos, Mats.Hex(Names.ColorOf(el == Element.Raw ? Element.Crimson : el)), 6, 3f, 0.25f);
            Hero.ApplyDamage(dmg);
        }

        public void OnHeroDied() => UI.ShowDeath();

        public void Respawn()
        {
            Hero.Revive();
            EnterZone(State.prologueDone ? ZoneDb.HubId : ZoneDb.PrologueId);
        }

        // ---------- Предметы ----------

        public void PickUp(LootDrop d)
        {
            if (d == null) return;
            var h = State.hero;
            switch (d.Drop.Kind)
            {
                case DropKind.Gold:
                    h.gold += d.Drop.Amount;
                    break;
                case DropKind.Potion:
                    if (h.potions >= GameState.MaxPotions) return;
                    h.potions++;
                    break;
                default:
                    if (!HeroOps.AddToInventory(h, d.Drop.Item)) { UI.Message("Сумка полна", Mats.Hex(0xFF7A7A)); return; }
                    FxSystem.Text(d.transform.position, d.Drop.Item.name, d.LabelColor);
                    break;
            }
            Drops.Remove(d);
            Destroy(d.gameObject);
        }

        public bool TryPickUpByLabel(LootDrop d)
        {
            if ((d.transform.position - Hero.Pos).sqrMagnitude > LabelPickupRadius * LabelPickupRadius)
            {
                UI.Message("Слишком далеко", Mats.Hex(0xBBBBBB));
                return false;
            }
            PickUp(d);
            return true;
        }

        public void OnEquipmentChanged() => Hero.RefreshStats(false);

        public Interactable NearestInteractable()
        {
            Interactable best = null;
            float bd = float.MaxValue;
            foreach (var it in Interactables)
            {
                if (it == null) continue;
                float d = (it.transform.position - Hero.Pos).sqrMagnitude;
                if (d < it.Radius * it.Radius && d < bd) { bd = d; best = it; }
            }
            return best;
        }

        // ---------- Кадр ----------

        void Update()
        {
            if (!InGame || Hero == null) return;

            if (GameInput.Down(Act.Menu)) UI.HandleEscape();
            if (GameInput.Down(Act.Inventory)) UI.Toggle(GameUI.Panel.Inventory);
            if (GameInput.Down(Act.Character)) UI.Toggle(GameUI.Panel.Character);
            if (GameInput.Down(Act.Skills)) UI.Toggle(GameUI.Panel.Skills);
            if (Paused) return;

            var cell = DungeonLayout.WorldToCell(Hero.Pos.x, Hero.Pos.z);
            flowTimer -= Time.deltaTime;
            if ((cell.X != flowCell.X || cell.Y != flowCell.Y) && flowTimer <= 0f)
            {
                flowCell = cell;
                flowTimer = 0.2f;
                Flow.Compute(cell, 48);
            }

            for (int i = Drops.Count - 1; i >= 0; i--)
            {
                var d = Drops[i];
                if (d == null || d.Drop.Kind == DropKind.Item) continue;
                if ((d.transform.position - Hero.Pos).sqrMagnitude < AutoPickupRadius * AutoPickupRadius) PickUp(d);
            }

            if (GameInput.Down(Act.Interact) && !UI.BlocksGameplay)
            {
                var it = NearestInteractable();
                if (it != null) it.OnUse?.Invoke();
                else
                {
                    LootDrop best = null;
                    float bd = ItemPickupRadius * ItemPickupRadius;
                    foreach (var d in Drops)
                    {
                        float dd = (d.transform.position - Hero.Pos).sqrMagnitude;
                        if (dd < bd) { bd = dd; best = d; }
                    }
                    if (best != null) PickUp(best);
                }
            }
        }

        void OnApplicationQuit()
        {
            if (InGame && State != null) SaveSystem.Save(State);
        }
    }

    /// <summary>Имена и реплики жителей лагеря.</summary>
    public sealed class NpcInfo
    {
        public string Name, Greeting;
        public VendorKind? Vendor;

        public static NpcInfo Get(string id)
        {
            switch (id)
            {
                case HubLayout.Gouache:
                    return new NpcInfo { Name = "Тётушка Гуашь", Greeting = "Ох, живая краска! Давно я такой не видела. Садись, грейся — а потом за работу." };
                case HubLayout.Sanguine:
                    return new NpcInfo { Name = "Мастер Сангина", Greeting = "Кисти и палитры. Ломаются реже, чем художники.", Vendor = VendorKind.Brushes };
                case HubLayout.Indigo:
                    return new NpcInfo { Name = "Лис Индиго", Greeting = "Шляпы, кольца, зелья. Почти ничего не краденое.", Vendor = VendorKind.Outfit };
                default:
                    return new NpcInfo { Name = "Мольберт путей", Greeting = "Куда рисуем дорогу?" };
            }
        }
    }
}
