using System;
using System.Collections.Generic;
using StormPath.Sim.Core;
using StormPath.Sim.Validate;

namespace StormPath.Sim.Gen
{
    /// <summary>Генератор карты маршрута: seed → карта (см. навык route-map).</summary>
    public static class RouteMapGen
    {
        public const int Width = 4;
        public const int Center = 1;
        public const int SanctuaryRow = 4;
        public const int ActFirstRow = 5;
        public const int LastRow = 16;
        public const int ActLastRow = LastRow - 2;
        public const int CampRow = LastRow - 1;
        public const int MaxAttempts = 50;
        public const int MaxSameBiomeRun = 3;

        static readonly ulong[] ActSalt = { 0xA17C0001D3B2F001UL, 0xA17C0002E5C4A102UL, 0xA17C0003F7D6B203UL };
        static readonly float[] ActBase = { 0.15f, 0.45f, 0.70f };
        static readonly BiomeId[] ActBiomes = { BiomeId.Inferno, BiomeId.Chronowaste, BiomeId.StormPeaks };

        // Запасной шаблон (подобран перебором под проверки): колонки каждой тропы по рядам 5..14.
        static readonly int[][] TemplateTrails =
        {
            new[] { 3, 3, 2, 2, 3, 2, 3, 2, 2, 3 },
            new[] { 0, 0, 1, 0, 0, 0, 1, 0, 1, 0 },
            new[] { 3, 2, 1, 0, 0, 0, 1, 2, 1, 0 },
            new[] { 3, 3, 2, 2, 1, 2, 3, 3, 3, 3 },
            new[] { 2, 2, 2, 3, 3, 3, 3, 3, 2, 2 },
            new[] { 1, 0, 0, 0, 1, 0, 1, 1, 1, 2 },
        };

        public static RouteMap Generate(ulong seed, int act = 0)
        {
            CheckAct(act);
            var rng = new Rng(seed ^ ActSalt[act]);
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var map = Build(seed, act, RollTrails(rng), rng);
                if (RouteMapValidator.Validate(map).Count == 0) return map;
            }
            return GenerateFromTemplate(seed, act);
        }

        /// <summary>Запасной путь: фиксированная схема троп, типы и биомы — от seed.</summary>
        public static RouteMap GenerateFromTemplate(ulong seed, int act = 0)
        {
            CheckAct(act);
            var rng = new Rng(seed ^ ActSalt[act] ^ 0x5EEDUL);
            for (int attempt = 0; attempt < 2000; attempt++)
            {
                var map = Build(seed, act, TemplateTrails, rng);
                if (RouteMapValidator.Validate(map).Count == 0) return map;
            }
            throw new InvalidOperationException("Не удалось собрать карту по шаблону, seed=" + seed);
        }

        static void CheckAct(int act)
        {
            if (act < 0 || act >= ActSalt.Length) throw new ArgumentOutOfRangeException(nameof(act));
        }

        // Тропы: каждая идёт на следующий ряд в c-1, c или c+1; пересечения запрещены.
        static int[][] RollTrails(Rng rng)
        {
            int count = rng.Range(4, 6);
            int steps = ActLastRow - ActFirstRow;
            var trails = new int[count][];
            for (int p = 0; p < count; p++)
            {
                trails[p] = new int[steps + 1];
                trails[p][0] = rng.NextInt(Width);
            }
            var moves = new HashSet<int>();
            for (int s = 0; s < steps; s++)
            {
                moves.Clear();
                for (int p = 0; p < count; p++)
                {
                    int c = trails[p][s];
                    int to = c;
                    for (int t = 0; t < 3; t++)
                    {
                        int d = c + rng.Range(-1, 1);
                        if (d < 0 || d >= Width) continue;
                        if (d != c && moves.Contains(d * Width + c)) continue; // пересечение
                        if (moves.Contains(c * Width + d)) continue;           // расходимся: так рождаются развилки
                        to = d;
                        break;
                    }
                    moves.Add(c * Width + to);
                    trails[p][s + 1] = to;
                }
            }
            return trails;
        }

        // Точки, рёбра, типы, биомы, сложность.
        static RouteMap Build(ulong seed, int act, int[][] trails, Rng rng)
        {
            var colsByRow = new List<int>[LastRow + 1];
            for (int r = 0; r <= LastRow; r++) colsByRow[r] = new List<int>();
            for (int r = 0; r <= SanctuaryRow; r++) colsByRow[r].Add(Center);
            colsByRow[CampRow].Add(Center);
            colsByRow[LastRow].Add(Center);
            foreach (var t in trails)
                for (int i = 0; i < t.Length; i++)
                    if (!colsByRow[ActFirstRow + i].Contains(t[i])) colsByRow[ActFirstRow + i].Add(t[i]);

            var nodes = new List<MapNode>();
            var index = new int[LastRow + 1, Width];
            for (int r = 0; r <= LastRow; r++)
            {
                colsByRow[r].Sort();
                for (int c = 0; c < Width; c++) index[r, c] = -1;
                foreach (int c in colsByRow[r])
                {
                    index[r, c] = nodes.Count;
                    nodes.Add(new MapNode { row = r, col = c });
                }
            }

            int n = nodes.Count;
            var next = new List<int>[n];
            var prev = new List<int>[n];
            for (int i = 0; i < n; i++) { next[i] = new List<int>(); prev[i] = new List<int>(); }
            void Edge(int a, int b)
            {
                if (next[a].Contains(b)) return;
                next[a].Add(b);
                prev[b].Add(a);
            }

            for (int r = 0; r < SanctuaryRow; r++) Edge(index[r, Center], index[r + 1, Center]);
            for (int c = 0; c < Width; c++)
            {
                if (index[ActFirstRow, c] >= 0) Edge(index[SanctuaryRow, Center], index[ActFirstRow, c]);
                if (index[ActLastRow, c] >= 0) Edge(index[ActLastRow, c], index[CampRow, Center]);
            }
            Edge(index[CampRow, Center], index[LastRow, Center]);
            foreach (var t in trails)
                for (int i = 1; i < t.Length; i++)
                    Edge(index[ActFirstRow + i - 1, t[i - 1]], index[ActFirstRow + i, t[i]]);

            // Типы и биомы идут по рядам, так что родители всегда назначены раньше потомков.
            var run = new int[n];
            for (int i = 0; i < n; i++)
            {
                var node = nodes[i];
                if (node.row == 0) { node.type = NodeType.Start; node.biome = BiomeId.Wastes; }
                else if (node.row < SanctuaryRow) { node.type = NodeType.Tutorial; node.biome = BiomeId.Wastes; }
                else if (node.row == SanctuaryRow) { node.type = NodeType.Sanctuary; node.biome = BiomeId.Wastes; }
                else if (node.row == CampRow) { node.type = NodeType.Camp; node.biome = BiomeId.Wastes; }
                else if (node.row == LastRow) { node.type = NodeType.Final; node.biome = ActBiomes[rng.NextInt(ActBiomes.Length)]; }
                else
                {
                    for (int tries = 0; tries < 8; tries++)
                    {
                        node.type = RollType(rng, nodes, prev[i], node.row);
                        node.biome = RollBiome(rng, nodes, run, prev[i], out run[i]);
                        if (!ClashesWithSibling(nodes, next, prev[i], i, node)) break;
                    }
                }
                nodes[i] = node;
            }

            EnsureShop(rng, nodes);

            float baseK = ActBase[act];
            ulong actSeed = seed ^ ActSalt[act];
            for (int i = 0; i < n; i++)
            {
                var node = nodes[i];
                float k = 0f;
                if (node.row >= ActFirstRow)
                {
                    k = baseK + 0.04f * (node.row - ActFirstRow);
                    if (node.type == NodeType.Elite) k += 0.2f;
                    k = k < 0f ? 0f : (k > 1f ? 1f : k);
                }
                node.k = k;
                node.levelSeed = Hash.Combine(actSeed, node.row, node.col);
                next[i].Sort();
                node.next = next[i].ToArray();
                nodes[i] = node;
            }

            return new RouteMap { seed = seed, act = act, nodes = nodes.ToArray(), current = 0 };
        }

        static NodeType RollType(Rng rng, List<MapNode> nodes, List<int> parents, int row)
        {
            int roll = rng.NextInt(100);
            NodeType type;
            if (roll < 60) type = NodeType.Level;
            else if (roll < 72) type = NodeType.Elite;
            else if (roll < 82) type = NodeType.Sanctuary;
            else if (roll < 90) type = NodeType.Shop;
            else type = NodeType.Riddle;

            if (type == NodeType.Elite)
            {
                bool parentElite = false;
                foreach (int p in parents) if (nodes[p].type == NodeType.Elite) parentElite = true;
                if (row < 7 || parentElite) type = NodeType.Level;
            }
            if (type == NodeType.Shop && row > LastRow - 4) type = NodeType.Level;
            return type;
        }

        // Биом «кусками»: с вероятностью 0.6 берём биом родителя, не больше MaxSameBiomeRun подряд.
        static BiomeId RollBiome(Rng rng, List<MapNode> nodes, int[] run, List<int> parents, out int runOut)
        {
            var parentBiomes = new List<BiomeId>();
            foreach (int p in parents)
                if (nodes[p].row >= ActFirstRow) parentBiomes.Add(nodes[p].biome);

            BiomeId pick = parentBiomes.Count > 0 && rng.Chance(60)
                ? parentBiomes[rng.NextInt(parentBiomes.Count)]
                : ActBiomes[rng.NextInt(ActBiomes.Length)];

            int start = Array.IndexOf(ActBiomes, pick);
            int dir = rng.NextInt(2) == 0 ? 1 : 2;
            for (int step = 0; step < ActBiomes.Length; step++)
            {
                // Сначала выбранный биом, затем остальные в порядке, зависящем от dir.
                var b = ActBiomes[(start + step * dir) % ActBiomes.Length];
                int r = RunFor(nodes, run, parents, b);
                if (r <= MaxSameBiomeRun) { runOut = r; return b; }
            }
            runOut = RunFor(nodes, run, parents, pick);
            return pick;
        }

        static int RunFor(List<MapNode> nodes, int[] run, List<int> parents, BiomeId b)
        {
            int best = 0;
            foreach (int p in parents)
                if (nodes[p].row >= ActFirstRow && nodes[p].biome == b && run[p] > best) best = run[p];
            return best + 1;
        }

        // Соседи по развилке — выходы одной точки: у них должны различаться тип или биом.
        static bool ClashesWithSibling(List<MapNode> nodes, List<int>[] next, List<int> parents, int self, MapNode node)
        {
            foreach (int p in parents)
                foreach (int sib in next[p])
                    if (sib < self && nodes[sib].type == node.type && nodes[sib].biome == node.biome) return true;
            return false;
        }

        static void EnsureShop(Rng rng, List<MapNode> nodes)
        {
            var candidates = new List<int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].type == NodeType.Shop) return;
                if (nodes[i].type == NodeType.Level && nodes[i].row > ActFirstRow && nodes[i].row <= LastRow - 4)
                    candidates.Add(i);
            }
            if (candidates.Count == 0) return;
            int pick = candidates[rng.NextInt(candidates.Count)];
            var node = nodes[pick];
            node.type = NodeType.Shop;
            nodes[pick] = node;
        }
    }
}
