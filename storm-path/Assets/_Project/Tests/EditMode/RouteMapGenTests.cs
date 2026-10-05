using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using StormPath.Sim.Gen;
using StormPath.Sim.Validate;

namespace StormPath.Tests
{
    public class RouteMapGenTests
    {
        const int SeedCount = 500;

        static string Signature(RouteMap m)
        {
            var sb = new StringBuilder();
            foreach (var x in m.nodes)
                sb.Append(x.row).Append(',').Append(x.col).Append(',').Append((int)x.type).Append(',')
                  .Append((int)x.biome).Append(',').Append(x.k).Append(',').Append(x.levelSeed).Append(':')
                  .Append(string.Join("/", x.next)).Append(';');
            return sb.ToString();
        }

        static RouteMap Clone(RouteMap m)
        {
            var c = new RouteMap { seed = m.seed, act = m.act, current = m.current, nodes = (MapNode[])m.nodes.Clone() };
            for (int i = 0; i < c.nodes.Length; i++) c.nodes[i].next = (int[])c.nodes[i].next.Clone();
            return c;
        }

        static string Report(RouteMap m, List<string> errors) =>
            "seed=" + m.seed + " act=" + m.act + ": " + string.Join(" | ", errors);

        [Test]
        public void SameSeed_GivesSameMap()
        {
            for (ulong seed = 0; seed < 50; seed++)
                Assert.AreEqual(Signature(RouteMapGen.Generate(seed)), Signature(RouteMapGen.Generate(seed)));
        }

        [Test]
        public void DifferentSeeds_GiveDifferentMaps()
        {
            var signatures = new HashSet<string>();
            for (ulong seed = 0; seed < 50; seed++) signatures.Add(Signature(RouteMapGen.Generate(seed)));
            Assert.Greater(signatures.Count, 40);
        }

        [Test]
        public void ActsDifferForSameSeed()
        {
            Assert.AreNotEqual(Signature(RouteMapGen.Generate(7, 0)), Signature(RouteMapGen.Generate(7, 1)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void GeneratedMaps_PassAllChecks(int act)
        {
            for (ulong seed = 1; seed <= SeedCount; seed++)
            {
                var map = RouteMapGen.Generate(seed * 7919UL, act);
                var errors = RouteMapValidator.Validate(map);
                Assert.IsEmpty(errors, Report(map, errors));
            }
        }

        [Test]
        public void Template_PassesAllChecks()
        {
            for (ulong seed = 1; seed <= 100; seed++)
            {
                var map = RouteMapGen.GenerateFromTemplate(seed);
                var errors = RouteMapValidator.Validate(map);
                Assert.IsEmpty(errors, Report(map, errors));
            }
        }

        [Test]
        public void StartingRows_FollowTheScheme()
        {
            var m = RouteMapGen.Generate(123);
            Assert.AreEqual(NodeType.Start, m.nodes[0].type);
            for (int r = 1; r <= 3; r++) Assert.AreEqual(NodeType.Tutorial, m.nodes[r].type);
            Assert.AreEqual(NodeType.Sanctuary, m.nodes[4].type);
            Assert.AreEqual(RouteMapGen.SanctuaryRow, m.nodes[4].row);
            Assert.AreEqual(NodeType.Camp, m.nodes[m.nodes.Length - 2].type);
            Assert.AreEqual(NodeType.Final, m.nodes[m.nodes.Length - 1].type);
            Assert.AreEqual(0, m.current);
        }

        [Test]
        public void LevelSeeds_AreUniqueWithinMap()
        {
            var m = RouteMapGen.Generate(99);
            var seen = new HashSet<ulong>();
            foreach (var x in m.nodes) Assert.IsTrue(seen.Add(x.levelSeed), "повтор seed уровня в ряду " + x.row);
        }

        [Test]
        public void Difficulty_IsClamped_AndGrowsWithRow_AndEliteIsHarder()
        {
            for (ulong seed = 1; seed <= 100; seed++)
            {
                var m = RouteMapGen.Generate(seed, 2);
                foreach (var x in m.nodes) Assert.That(x.k, Is.InRange(0f, 1f));
                foreach (var x in m.nodes)
                    foreach (int j in x.next)
                    {
                        var y = m.nodes[j];
                        if (x.row >= RouteMapGen.ActFirstRow && y.type != NodeType.Elite && x.type != NodeType.Elite)
                            Assert.GreaterOrEqual(y.k, x.k - 1e-6f);
                    }
            }
            var first = RouteMapGen.Generate(5, 0);
            foreach (var x in first.nodes)
                if (x.type == NodeType.Elite)
                    Assert.AreEqual(0.15f + 0.04f * (x.row - RouteMapGen.ActFirstRow) + 0.2f, x.k, 1e-5f);
        }

        [Test]
        public void MostMaps_Reach_TargetForkShare()
        {
            double sum = 0;
            for (ulong seed = 1; seed <= 200; seed++) sum += RouteMapValidator.ForkShare(RouteMapGen.Generate(seed));
            Assert.GreaterOrEqual(sum / 200, RouteMapValidator.MinForkShare);
        }

        [Test]
        public void Validator_FindsDeadEnd()
        {
            var m = Clone(RouteMapGen.Generate(11));
            int i = FirstNode(m, 8);
            m.nodes[i].next = new int[0];
            Assert.IsNotEmpty(RouteMapValidator.Validate(m));
        }

        [Test]
        public void Validator_FindsMissingShop()
        {
            var m = Clone(RouteMapGen.Generate(12));
            for (int i = 0; i < m.nodes.Length; i++)
                if (m.nodes[i].type == NodeType.Shop) m.nodes[i].type = NodeType.Level;
            Assert.That(string.Join("|", RouteMapValidator.Validate(m)), Does.Contain("лавки"));
        }

        [Test]
        public void Validator_FindsEliteTooEarly()
        {
            var m = Clone(RouteMapGen.Generate(13));
            m.nodes[FirstNode(m, 5)].type = NodeType.Elite;
            Assert.That(string.Join("|", RouteMapValidator.Validate(m)), Does.Contain("Элита"));
        }

        [Test]
        public void Validator_FindsCrossingEdges()
        {
            var m = Clone(RouteMapGen.Generate(14));
            bool swapped = false;
            for (int i = 0; i < m.nodes.Length && !swapped; i++)
                for (int j = i + 1; j < m.nodes.Length && m.nodes[j].row == m.nodes[i].row && !swapped; j++)
                {
                    if (m.nodes[i].row < RouteMapGen.ActFirstRow || m.nodes[i].row >= RouteMapGen.ActLastRow) continue;
                    int a = m.nodes[i].next[0], b = m.nodes[j].next[0];
                    if (m.nodes[a].col >= m.nodes[b].col) continue;
                    m.nodes[i].next = new[] { b };
                    m.nodes[j].next = new[] { a };
                    swapped = true;
                }
            Assert.IsTrue(swapped, "не нашли пару точек для проверки пересечения");
            Assert.That(string.Join("|", RouteMapValidator.Validate(m)), Does.Contain("пересекаются"));
        }

        static int FirstNode(RouteMap m, int row)
        {
            for (int i = 0; i < m.nodes.Length; i++) if (m.nodes[i].row == row) return i;
            Assert.Fail("нет ряда " + row);
            return -1;
        }
    }
}
