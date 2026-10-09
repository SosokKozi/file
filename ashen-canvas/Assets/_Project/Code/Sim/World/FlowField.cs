using System.Collections.Generic;

namespace AshenCanvas.Sim.World
{
    /// <summary>Карта расстояний до цели по полу (8 направлений, без срезания углов).</summary>
    public sealed class FlowField
    {
        static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        static readonly int[] DY = { 0, 0, 1, -1, 1, -1, 1, -1 };

        readonly DungeonLayout map;
        public readonly int[] Dist;
        readonly Queue<int> queue = new Queue<int>();
        public Cell Target { get; private set; }

        public FlowField(DungeonLayout map)
        {
            this.map = map;
            Dist = new int[map.W * map.H];
        }

        public void Compute(Cell target, int maxDist = int.MaxValue)
        {
            Target = target;
            for (int i = 0; i < Dist.Length; i++) Dist[i] = -1;
            if (!map.IsFloor(target.X, target.Y)) return;
            queue.Clear();
            Dist[target.Y * map.W + target.X] = 0;
            queue.Enqueue(target.Y * map.W + target.X);
            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                int x = idx % map.W, y = idx / map.W, d = Dist[idx];
                if (d >= maxDist) continue;
                for (int k = 0; k < 8; k++)
                {
                    int nx = x + DX[k], ny = y + DY[k];
                    if (!map.IsFloor(nx, ny)) continue;
                    if (k >= 4 && (!map.IsFloor(x + DX[k], y) || !map.IsFloor(x, y + DY[k]))) continue;
                    int ni = ny * map.W + nx;
                    if (Dist[ni] >= 0) continue;
                    Dist[ni] = d + 1;
                    queue.Enqueue(ni);
                }
            }
        }

        public int DistAt(Cell c) => map.InBounds(c.X, c.Y) ? Dist[c.Y * map.W + c.X] : -1;

        /// <summary>Соседняя клетка ближе к цели; false — пути нет или уже на месте.</summary>
        public bool NextStep(Cell from, out Cell next)
        {
            next = from;
            int best = DistAt(from);
            if (best <= 0) return false;
            bool found = false;
            for (int k = 0; k < 8; k++)
            {
                int nx = from.X + DX[k], ny = from.Y + DY[k];
                if (!map.IsFloor(nx, ny)) continue;
                if (k >= 4 && (!map.IsFloor(from.X + DX[k], from.Y) || !map.IsFloor(from.X, from.Y + DY[k]))) continue;
                int d = Dist[ny * map.W + nx];
                if (d >= 0 && d < best) { best = d; next = new Cell(nx, ny); found = true; }
            }
            return found;
        }
    }
}
