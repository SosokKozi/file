using System.Collections.Generic;
using AshenCanvas.Sim.Enemies;

namespace AshenCanvas.Sim.World
{
    public struct Cell
    {
        public int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
        public override string ToString() => "(" + X + "," + Y + ")";
    }

    public struct RoomRect
    {
        public int X, Y, W, H;
        public RoomRect(int x, int y, int w, int h) { X = x; Y = y; W = w; H = h; }
        public Cell Center => new Cell(X + W / 2, Y + H / 2);
        public bool Contains(int x, int y) => x >= X && y >= Y && x < X + W && y < Y + H;
        public bool Overlaps(RoomRect o, int gap)
            => X - gap < o.X + o.W && o.X - gap < X + W && Y - gap < o.Y + o.H && o.Y - gap < Y + H;
    }

    public sealed class PackSpawn
    {
        public Cell At;
        public string EnemyId;
        public int Count;
        public MonsterRarity Rarity;
        public string RareModId;  // только для редкого вожака
    }

    public enum PropKind { Pillar, Candle, Easel, Bucket, Rubble, Tree, Crate, Lamp }

    public struct PropSpawn
    {
        public Cell At;
        public PropKind Kind;
    }

    public sealed class NpcSpot
    {
        public string Id;
        public Cell At;
    }

    /// <summary>Сетка зоны: 0 — стена, 1 — пол. Клетка = <see cref="TileSize"/> метров.</summary>
    public sealed class DungeonLayout
    {
        public const float TileSize = 2f;

        public int W, H;
        public byte[] Tiles;
        public List<RoomRect> Rooms = new List<RoomRect>();
        public int StartRoom = -1, BossRoom = -1;
        public Cell Start, Boss, Exit;
        public List<PackSpawn> Packs = new List<PackSpawn>();
        public List<PropSpawn> Props = new List<PropSpawn>();
        public List<NpcSpot> Npcs = new List<NpcSpot>();
        public string BossId;

        public DungeonLayout(int w, int h)
        {
            W = w; H = h;
            Tiles = new byte[w * h];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
        public bool IsFloor(int x, int y) => InBounds(x, y) && Tiles[y * W + x] != 0;
        public void SetFloor(int x, int y) { if (x > 0 && y > 0 && x < W - 1 && y < H - 1) Tiles[y * W + x] = 1; }

        public void CarveRect(int x, int y, int w, int h)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++) SetFloor(i, j);
        }

        public int FloorCount()
        {
            int n = 0;
            for (int i = 0; i < Tiles.Length; i++) if (Tiles[i] != 0) n++;
            return n;
        }

        /// <summary>Центр клетки в метрах (x, z).</summary>
        public static void CellToWorld(Cell c, out float x, out float z)
        {
            x = (c.X + 0.5f) * TileSize;
            z = (c.Y + 0.5f) * TileSize;
        }

        public static Cell WorldToCell(float x, float z)
            => new Cell((int)System.Math.Floor(x / TileSize), (int)System.Math.Floor(z / TileSize));

        public bool IsWalkableWorld(float x, float z)
        {
            var c = WorldToCell(x, z);
            return IsFloor(c.X, c.Y);
        }

        /// <summary>Движение круга по сетке с прилипанием к стенам (сначала X, потом Z).</summary>
        public void MoveCircle(ref float x, ref float z, float dx, float dz, float radius)
        {
            if (dx != 0f && CircleFree(x + dx, z, radius)) x += dx;
            if (dz != 0f && CircleFree(x, z + dz, radius)) z += dz;
        }

        public bool CircleFree(float x, float z, float r)
        {
            return IsWalkableWorld(x - r, z - r) && IsWalkableWorld(x + r, z - r)
                && IsWalkableWorld(x - r, z + r) && IsWalkableWorld(x + r, z + r);
        }

        /// <summary>Есть ли прямая видимость между точками (шаг по полклетки).</summary>
        public bool LineClear(float x0, float z0, float x1, float z1)
        {
            float dx = x1 - x0, dz = z1 - z0;
            float len = (float)System.Math.Sqrt(dx * dx + dz * dz);
            int steps = (int)(len / (TileSize * 0.5f)) + 1;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                if (!IsWalkableWorld(x0 + dx * t, z0 + dz * t)) return false;
            }
            return true;
        }
    }
}
