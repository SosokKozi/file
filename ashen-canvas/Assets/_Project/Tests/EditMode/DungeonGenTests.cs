using NUnit.Framework;
using AshenCanvas.Sim.Enemies;
using AshenCanvas.Sim.World;

namespace AshenCanvas.Tests
{
    public class DungeonGenTests
    {
        static System.Collections.Generic.IEnumerable<string> GeneratedZones()
        {
            foreach (var z in ZoneDb.All)
                if (z.Kind != ZoneKind.Hub) yield return z.Id;
        }

        [TestCaseSource(nameof(GeneratedZones))]
        public void ManySeeds_AllValid(string zoneId)
        {
            var z = ZoneDb.Get(zoneId);
            for (ulong seed = 1; seed <= 300; seed++)
            {
                var map = DungeonGen.Generate(z, seed);
                Assert.IsNull(DungeonGen.Validate(z, map), zoneId + " seed " + seed);
                Assert.AreEqual(z.BossId, map.BossId);
            }
        }

        [TestCaseSource(nameof(GeneratedZones))]
        public void SameSeed_SameLayout(string zoneId)
        {
            var z = ZoneDb.Get(zoneId);
            var a = DungeonGen.Generate(z, 777);
            var b = DungeonGen.Generate(z, 777);
            CollectionAssert.AreEqual(a.Tiles, b.Tiles);
            Assert.AreEqual(a.Packs.Count, b.Packs.Count);
            Assert.AreEqual(a.Props.Count, b.Props.Count);
        }

        [Test]
        public void DifferentSeeds_DifferentLayouts()
        {
            var z = ZoneDb.Get("rot_marsh");
            CollectionAssert.AreNotEqual(DungeonGen.Generate(z, 1).Tiles, DungeonGen.Generate(z, 2).Tiles);
        }

        [Test]
        public void Prologue_HasFewEnemies_AndNoElites()
        {
            var z = ZoneDb.Get(ZoneDb.PrologueId);
            for (ulong seed = 1; seed <= 100; seed++)
            {
                var map = DungeonGen.Generate(z, seed);
                int enemies = 0;
                foreach (var p in map.Packs)
                {
                    enemies += p.Count;
                    Assert.AreEqual(MonsterRarity.Normal, p.Rarity);
                }
                Assert.That(enemies, Is.InRange(3, 15), "seed " + seed);
            }
        }

        [Test]
        public void NoPackNearStart()
        {
            var z = ZoneDb.Get("ink_catacombs");
            var map = DungeonGen.Generate(z, 9);
            var ff = new FlowField(map);
            ff.Compute(map.Start);
            foreach (var p in map.Packs) Assert.GreaterOrEqual(ff.DistAt(p.At), 7);
        }

        [Test]
        public void Hub_HasAllNpcs_OnFloor_AndReachable()
        {
            var map = DungeonGen.Generate(ZoneDb.Get(ZoneDb.HubId), 0);
            var ff = new FlowField(map);
            ff.Compute(map.Start);
            Assert.AreEqual(4, map.Npcs.Count);
            foreach (var n in map.Npcs) Assert.Greater(ff.DistAt(n.At), 0, n.Id);
        }

        [Test]
        public void FlowField_LeadsToTarget()
        {
            var z = ZoneDb.Get("faded_forest");
            var map = DungeonGen.Generate(z, 5);
            var ff = new FlowField(map);
            ff.Compute(map.Boss);
            var c = map.Start;
            int steps = 0;
            while (ff.NextStep(c, out var next) && steps < 10000) { c = next; steps++; }
            Assert.AreEqual(map.Boss.X, c.X);
            Assert.AreEqual(map.Boss.Y, c.Y);
        }

        [Test]
        public void MoveCircle_DoesNotEnterWalls()
        {
            var map = DungeonGen.Generate(ZoneDb.Get(ZoneDb.HubId), 0);
            DungeonLayout.CellToWorld(map.Start, out float x, out float z);
            for (int i = 0; i < 500; i++) map.MoveCircle(ref x, ref z, 0f, -0.3f, 0.4f);
            Assert.IsTrue(map.CircleFree(x, z, 0.4f));
            Assert.Greater(z, 2 * DungeonLayout.TileSize);
        }
    }
}
