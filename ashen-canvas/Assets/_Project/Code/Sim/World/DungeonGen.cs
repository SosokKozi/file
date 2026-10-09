using System;
using System.Collections.Generic;
using AshenCanvas.Sim.Core;
using AshenCanvas.Sim.Enemies;

namespace AshenCanvas.Sim.World
{
    /// <summary>
    /// Генератор зон. Комнаты + коридоры, старт и босс в самых дальних точках,
    /// стаи врагов, декор. Зона, не прошедшая <see cref="Validate"/>, не выпускается.
    /// </summary>
    public static class DungeonGen
    {
        public const int MaxAttempts = 12;
        const int CorridorWidth = 2;
        const int SafeStartRadius = 7;

        public static DungeonLayout Generate(ZoneDef z, ulong seed)
        {
            if (z.Style == LayoutStyle.Hub) return HubLayout.Build(z);
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var rng = new Rng(Hash.Combine(seed, attempt, 0x5EED));
                var map = z.Style == LayoutStyle.Chain ? BuildChain(z, rng) : BuildBranching(z, rng);
                if (map == null) continue;
                FinishLayout(z, map, rng);
                if (Validate(z, map) == null) return map;
            }
            throw new InvalidOperationException("Не удалось собрать зону " + z.Id + " для seed " + seed);
        }

        // --- Цепочка комнат слева направо (пролог) ---
        static DungeonLayout BuildChain(ZoneDef z, Rng rng)
        {
            const int h = 30;
            var rects = new List<RoomRect>();
            int cursor = 3;
            for (int i = 0; i < z.Rooms; i++)
            {
                bool boss = i == z.Rooms - 1;
                int w = boss ? z.BossRoomSize : rng.Range(z.RoomMin, z.RoomMax);
                int rh = boss ? z.BossRoomSize : rng.Range(z.RoomMin, z.RoomMax);
                int y = h / 2 - rh / 2 + rng.Range(-5, 5);
                y = Math.Max(2, Math.Min(h - rh - 2, y));
                rects.Add(new RoomRect(cursor, y, w, rh));
                cursor += w + rng.Range(3, 6);
            }
            var map = new DungeonLayout(cursor + 2, h);
            foreach (var r in rects) { map.Rooms.Add(r); map.CarveRect(r.X, r.Y, r.W, r.H); }
            for (int i = 0; i + 1 < rects.Count; i++) CarveCorridor(map, rects[i].Center, rects[i + 1].Center, rng);
            map.StartRoom = 0;
            map.BossRoom = rects.Count - 1;
            return map;
        }

        // --- Разветвлённая зона: комнаты, остовное дерево и несколько петель ---
        static DungeonLayout BuildBranching(ZoneDef z, Rng rng)
        {
            int size = z.Size;
            var map = new DungeonLayout(size, size);
            for (int tries = 0; tries < 800 && map.Rooms.Count < z.Rooms; tries++)
            {
                int w = rng.Range(z.RoomMin, z.RoomMax), h = rng.Range(z.RoomMin, z.RoomMax);
                var r = new RoomRect(rng.Range(2, size - w - 2), rng.Range(2, size - h - 2), w, h);
                bool ok = true;
                foreach (var o in map.Rooms) if (r.Overlaps(o, 3)) { ok = false; break; }
                if (ok) map.Rooms.Add(r);
            }
            if (map.Rooms.Count < Math.Max(4, z.Rooms * 2 / 3)) return null;

            foreach (var r in map.Rooms) map.CarveRect(r.X, r.Y, r.W, r.H);

            // Прим: соединяем ближайшие комнаты, пока все не связаны.
            int n = map.Rooms.Count;
            var inTree = new bool[n];
            inTree[0] = true;
            for (int added = 1; added < n; added++)
            {
                int bi = -1, bj = -1, bd = int.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (!inTree[i]) continue;
                    for (int j = 0; j < n; j++)
                    {
                        if (inTree[j]) continue;
                        int d = Manhattan(map.Rooms[i].Center, map.Rooms[j].Center);
                        if (d < bd) { bd = d; bi = i; bj = j; }
                    }
                }
                inTree[bj] = true;
                CarveCorridor(map, map.Rooms[bi].Center, map.Rooms[bj].Center, rng);
            }
            for (int k = 0; k < z.Loops; k++)
            {
                int a = rng.NextInt(n), b = rng.NextInt(n);
                if (a != b && Manhattan(map.Rooms[a].Center, map.Rooms[b].Center) < size / 2)
                    CarveCorridor(map, map.Rooms[a].Center, map.Rooms[b].Center, rng);
            }

            // Старт — самая левая нижняя комната, босс — самая далёкая по полу.
            int start = 0;
            for (int i = 1; i < n; i++)
            {
                var c = map.Rooms[i].Center; var s = map.Rooms[start].Center;
                if (c.X + c.Y < s.X + s.Y) start = i;
            }
            map.StartRoom = start;
            var ff = new FlowField(map);
            ff.Compute(map.Rooms[start].Center);
            int boss = start, far = -1;
            for (int i = 0; i < n; i++)
            {
                int d = ff.DistAt(map.Rooms[i].Center);
                if (i != start && d > far) { far = d; boss = i; }
            }
            map.BossRoom = boss;

            // Арена босса не меньше BossRoomSize.
            var br = map.Rooms[boss];
            if (br.W < z.BossRoomSize || br.H < z.BossRoomSize)
            {
                var c = br.Center;
                int half = z.BossRoomSize / 2;
                int x = Math.Max(2, Math.Min(size - z.BossRoomSize - 2, c.X - half));
                int y = Math.Max(2, Math.Min(size - z.BossRoomSize - 2, c.Y - half));
                var big = new RoomRect(x, y, z.BossRoomSize, z.BossRoomSize);
                map.CarveRect(big.X, big.Y, big.W, big.H);
                map.Rooms[boss] = big;
                // Арена могла сдвинуться: протягиваем коридор от старой середины к новой.
                CarveCorridor(map, c, big.Center, rng);
            }
            return map;
        }

        static void FinishLayout(ZoneDef z, DungeonLayout map, Rng rng)
        {
            map.Start = map.Rooms[map.StartRoom].Center;
            map.Boss = map.Rooms[map.BossRoom].Center;
            map.Exit = new Cell(map.Boss.X, map.Boss.Y + 3);
            if (!map.IsFloor(map.Exit.X, map.Exit.Y)) map.Exit = map.Boss;
            map.BossId = z.BossId;

            var ff = new FlowField(map);
            ff.Compute(map.Start);

            for (int i = 0; i < map.Rooms.Count; i++)
            {
                if (i == map.StartRoom || i == map.BossRoom) continue;
                var r = map.Rooms[i];
                int packs = rng.Range(z.PacksMin, z.PacksMax);
                for (int p = 0; p < packs; p++)
                {
                    var at = new Cell(rng.Range(r.X + 1, r.X + r.W - 2), rng.Range(r.Y + 1, r.Y + r.H - 2));
                    int d = ff.DistAt(at);
                    if (d < SafeStartRadius) continue;
                    var pack = new PackSpawn
                    {
                        At = at,
                        EnemyId = rng.Pick(z.Roster),
                        Count = rng.Range(z.PackSizeMin, z.PackSizeMax),
                        Rarity = MonsterRarity.Normal,
                    };
                    int roll = rng.NextInt(100);
                    if (roll < z.RarePct)
                    {
                        pack.Rarity = MonsterRarity.Rare;
                        pack.RareModId = rng.Pick(EnemyDb.RareMods).Id;
                    }
                    else if (roll < z.RarePct + z.MagicPct) pack.Rarity = MonsterRarity.Magic;
                    map.Packs.Add(pack);
                }
            }

            if (z.PropKinds.Length > 0)
            {
                for (int i = 0; i < map.Rooms.Count; i++)
                {
                    var r = map.Rooms[i];
                    int count = rng.Range(2, 5);
                    for (int k = 0; k < count; k++)
                    {
                        var at = new Cell(rng.Range(r.X, r.X + r.W - 1), rng.Range(r.Y, r.Y + r.H - 1));
                        if (!map.IsFloor(at.X, at.Y) || !NextToWall(map, at)) continue;
                        if (Manhattan(at, map.Start) < 2 || Manhattan(at, map.Boss) < 4 || Manhattan(at, map.Exit) < 2) continue;
                        map.Props.Add(new PropSpawn { At = at, Kind = rng.Pick(z.PropKinds) });
                    }
                }
            }
        }

        /// <summary>Причина, по которой зона не годится, или null.</summary>
        public static string Validate(ZoneDef z, DungeonLayout map)
        {
            if (!map.IsFloor(map.Start.X, map.Start.Y)) return "старт не на полу";
            if (!map.IsFloor(map.Boss.X, map.Boss.Y)) return "босс не на полу";
            var ff = new FlowField(map);
            ff.Compute(map.Start);
            int bossDist = ff.DistAt(map.Boss);
            if (bossDist < 0) return "босс недостижим";
            if (bossDist < z.MinBossDistance) return "босс слишком близко: " + bossDist;
            if (ff.DistAt(map.Exit) < 0) return "выход недостижим";
            for (int i = 0; i < map.Tiles.Length; i++)
                if (map.Tiles[i] != 0 && ff.Dist[i] < 0) return "есть отрезанный пол";
            foreach (var p in map.Packs)
                if (ff.DistAt(p.At) < 0) return "стая в стене";
            var br = map.Rooms[map.BossRoom];
            if (br.W < z.BossRoomSize || br.H < z.BossRoomSize) return "арена босса мала";
            return null;
        }

        static void CarveCorridor(DungeonLayout map, Cell a, Cell b, Rng rng)
        {
            bool horizontalFirst = rng.Chance(50);
            var corner = horizontalFirst ? new Cell(b.X, a.Y) : new Cell(a.X, b.Y);
            CarveLine(map, a, corner);
            CarveLine(map, corner, b);
        }

        static void CarveLine(DungeonLayout map, Cell a, Cell b)
        {
            int x0 = Math.Min(a.X, b.X), x1 = Math.Max(a.X, b.X);
            int y0 = Math.Min(a.Y, b.Y), y1 = Math.Max(a.Y, b.Y);
            map.CarveRect(x0, y0, x1 - x0 + CorridorWidth, y1 - y0 + CorridorWidth);
        }

        static bool NextToWall(DungeonLayout map, Cell c)
            => !map.IsFloor(c.X + 1, c.Y) || !map.IsFloor(c.X - 1, c.Y) || !map.IsFloor(c.X, c.Y + 1) || !map.IsFloor(c.X, c.Y - 1);

        static int Manhattan(Cell a, Cell b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
}
