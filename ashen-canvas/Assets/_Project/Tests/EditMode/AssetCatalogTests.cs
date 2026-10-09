using System.IO;
using NUnit.Framework;
using AshenCanvas.Sim.Enemies;
using AshenCanvas.Sim.Items;

namespace AshenCanvas.Tests
{
    /// <summary>Каждая модель, которую просит игра, описана в каталоге (оттуда берутся промпты для Tripo).</summary>
    public class AssetCatalogTests
    {
        static string CatalogText()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                var path = Path.Combine(dir.FullName, "Assets", "_Project", "Art", "asset_catalog.json");
                if (File.Exists(path)) return File.ReadAllText(path);
                dir = dir.Parent;
            }
            Assert.Fail("Не найден asset_catalog.json");
            return null;
        }

        [Test]
        public void EveryEnemyAndItemModel_IsInCatalog()
        {
            var text = CatalogText();
            foreach (var e in EnemyDb.All) StringAssert.Contains("\"" + e.AssetId + "\"", text, e.Id);
            foreach (var b in ItemDb.Bases) StringAssert.Contains("\"" + b.AssetId + "\"", text, b.Id);
            foreach (var id in new[] { "hero", "npc_gouache", "npc_sanguine", "npc_indigo", "npc_waypoint", "portal", "prop_fountain" })
                StringAssert.Contains("\"" + id + "\"", text, id);
        }
    }
}
