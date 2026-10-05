using System;
using System.Collections.Generic;
using StormPath.Sim.Gen;

namespace StormPath.Sim.Validate
{
    /// <summary>Обязательные проверки карты маршрута (навык route-map). Пустой список — карта годна.</summary>
    public static class RouteMapValidator
    {
        public const float MinForkShare = 0.4f;
        public const int MinForksPerPath = 3;

        public static List<string> Validate(RouteMap map)
        {
            var errors = new List<string>();
            if (!CheckStructure(map, errors)) return errors; // дальше индексы небезопасны

            var nodes = map.nodes;
            int n = nodes.Length;
            int last = n - 1;

            // 1. Достижимость: из старта — все точки, из каждой точки — финал.
            var fwd = new bool[n];
            fwd[0] = true;
            for (int i = 0; i < n; i++)
                if (fwd[i]) foreach (int j in nodes[i].next) fwd[j] = true;
            var back = new bool[n];
            back[last] = true;
            for (int i = n - 2; i >= 0; i--)
                foreach (int j in nodes[i].next) if (back[j]) back[i] = true;
            for (int i = 0; i < n; i++)
            {
                if (!fwd[i]) errors.Add("Точка " + i + " недостижима из старта");
                if (!back[i]) errors.Add("Тупик: из точки " + i + " нет пути к финалу");
            }

            // 2. Святилище, лечение и лавка.
            if (FinalReachableAvoiding(nodes, x => x.type == NodeType.Sanctuary && x.row <= RouteMapGen.LastRow - 3))
                errors.Add("Есть путь без святилища до ряда " + (RouteMapGen.LastRow - 3));
            if (FinalReachableAvoiding(nodes, x => x.type == NodeType.Camp || x.type == NodeType.Shop))
                errors.Add("Есть путь без привала и лавки");
            bool anyShop = false;
            for (int i = 0; i < n; i++)
            {
                var x = nodes[i];
                if (x.type == NodeType.Shop)
                {
                    anyShop = true;
                    if (x.row > RouteMapGen.LastRow - 4) errors.Add("Лавка в последних двух рядах акта: точка " + i);
                }
                if (x.type == NodeType.Elite)
                {
                    if (x.row < 7) errors.Add("Элита раньше ряда 7: точка " + i);
                    foreach (int j in x.next)
                        if (nodes[j].type == NodeType.Elite) errors.Add("Две элиты подряд: " + i + " → " + j);
                }
            }
            if (!anyShop) errors.Add("На карте нет лавки");

            // 3. Биомы: на любом пути минимум 2 биома и не больше MaxSameBiomeRun подряд.
            foreach (BiomeId b in Enum.GetValues(typeof(BiomeId)))
            {
                if (b == BiomeId.None || b == BiomeId.Wastes) continue;
                if (HasSingleBiomePath(nodes, b)) errors.Add("Есть путь через один биом: " + b);
            }
            var run = new int[n];
            var inRun = new int[n];
            for (int i = 0; i < n; i++)
            {
                if (!InAct(nodes[i])) continue;
                run[i] = inRun[i] + 1;
                if (run[i] > RouteMapGen.MaxSameBiomeRun)
                    errors.Add("Больше " + RouteMapGen.MaxSameBiomeRun + " одинаковых биомов подряд: точка " + i);
                foreach (int j in nodes[i].next)
                    if (InAct(nodes[j]) && nodes[j].biome == nodes[i].biome && run[i] > inRun[j]) inRun[j] = run[i];
            }

            // Развилки: у выходов одной точки разные тип или биом.
            for (int i = 0; i < n; i++)
            {
                var nx = nodes[i].next;
                for (int a = 0; a < nx.Length; a++)
                    for (int b = a + 1; b < nx.Length; b++)
                        if (nodes[nx[a]].type == nodes[nx[b]].type && nodes[nx[a]].biome == nodes[nx[b]].biome)
                            errors.Add("Пустой выбор: выходы точки " + i + " одинаковы (" + nx[a] + ", " + nx[b] + ")");
            }

            // Достаточно настоящих выборов.
            if (ForkShare(map) < MinForkShare) errors.Add("Мало развилок: доля < " + MinForkShare);
            if (MinForksOnPath(nodes) < MinForksPerPath) errors.Add("Есть путь меньше чем с " + MinForksPerPath + " выборами");

            // 4. Пересечения рёбер: две точки одного ряда не должны обменяться порядком.
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n && nodes[j].row == nodes[i].row; j++)
                    foreach (int a in nodes[i].next)
                        foreach (int b in nodes[j].next)
                            if ((nodes[i].col - nodes[j].col) * (nodes[a].col - nodes[b].col) < 0)
                                errors.Add("Рёбра пересекаются: " + i + "→" + a + " и " + j + "→" + b);

            return errors;
        }

        /// <summary>Доля точек акта, из которых выходит ≥ 2 ребра.</summary>
        public static float ForkShare(RouteMap map)
        {
            int total = 0, forks = 0;
            foreach (var x in map.nodes)
            {
                if (!InAct(x)) continue;
                total++;
                if (x.next != null && x.next.Length >= 2) forks++;
            }
            return total == 0 ? 0f : (float)forks / total;
        }

        static bool InAct(MapNode x) => x.row >= RouteMapGen.ActFirstRow && x.row <= RouteMapGen.ActLastRow;

        static bool CheckStructure(RouteMap map, List<string> errors)
        {
            if (map == null || map.nodes == null || map.nodes.Length < 2) { errors.Add("Пустая карта"); return false; }
            var nodes = map.nodes;
            int n = nodes.Length;
            bool ok = true;
            for (int i = 0; i < n; i++)
            {
                var x = nodes[i];
                if (i > 0 && (x.row < nodes[i - 1].row || (x.row == nodes[i - 1].row && x.col <= nodes[i - 1].col)))
                { errors.Add("Точки не по порядку: " + i); ok = false; }
                if (x.next == null) { errors.Add("Нет списка выходов: " + i); ok = false; continue; }
                foreach (int j in x.next)
                    if (j <= i || j >= n || nodes[j].row != x.row + 1)
                    { errors.Add("Плохое ребро " + i + "→" + j); ok = false; }
                NodeType expected;
                if (TryExpectedType(x.row, out expected) && x.type != expected)
                { errors.Add("Тип точки " + i + " должен быть " + expected); ok = false; }
            }
            if (nodes[0].row != 0 || nodes[n - 1].row != RouteMapGen.LastRow) { errors.Add("Нет старта или финала"); ok = false; }
            return ok;
        }

        static bool TryExpectedType(int row, out NodeType type)
        {
            type = NodeType.Level;
            if (row == 0) type = NodeType.Start;
            else if (row < RouteMapGen.SanctuaryRow) type = NodeType.Tutorial;
            else if (row == RouteMapGen.SanctuaryRow) type = NodeType.Sanctuary;
            else if (row == RouteMapGen.CampRow) type = NodeType.Camp;
            else if (row == RouteMapGen.LastRow) type = NodeType.Final;
            else return false;
            return true;
        }

        // Можно ли дойти до финала, не заходя в точки, на которые указывает blocked.
        static bool FinalReachableAvoiding(MapNode[] nodes, Func<MapNode, bool> blocked)
        {
            var reach = new bool[nodes.Length];
            reach[0] = !blocked(nodes[0]);
            for (int i = 0; i < nodes.Length; i++)
                if (reach[i])
                    foreach (int j in nodes[i].next)
                        if (!blocked(nodes[j])) reach[j] = true;
            return reach[nodes.Length - 1];
        }

        // Есть ли путь через весь акт, где каждая точка в биоме b.
        static bool HasSingleBiomePath(MapNode[] nodes, BiomeId b)
        {
            var ok = new bool[nodes.Length];
            var hasOkPred = new bool[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                var x = nodes[i];
                if (!InAct(x) || x.biome != b) continue;
                if (x.row != RouteMapGen.ActFirstRow && !hasOkPred[i]) continue;
                ok[i] = true;
                if (x.row == RouteMapGen.ActLastRow) return true;
                foreach (int j in x.next) hasOkPred[j] = true;
            }
            return false;
        }

        // Минимум развилок на пути по акту (по всем путям).
        static int MinForksOnPath(MapNode[] nodes)
        {
            var minIn = new int[nodes.Length];
            for (int i = 0; i < minIn.Length; i++) minIn[i] = int.MaxValue;
            int result = int.MaxValue;
            for (int i = 0; i < nodes.Length; i++)
            {
                var x = nodes[i];
                if (!InAct(x)) continue;
                int own = x.next.Length >= 2 ? 1 : 0;
                int total = (x.row == RouteMapGen.ActFirstRow ? 0 : minIn[i]) + own;
                if (x.row == RouteMapGen.ActLastRow) { if (total < result) result = total; continue; }
                foreach (int j in x.next) if (total < minIn[j]) minIn[j] = total;
            }
            return result;
        }
    }
}
