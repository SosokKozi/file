using AshenCanvas.Sim.Core;
using AshenCanvas.Sim.Items;
using AshenCanvas.Sim.Progression;
using AshenCanvas.Sim.Quests;
using AshenCanvas.Sim.World;

namespace AshenCanvas.Sim.Run
{
    public static class GameOps
    {
        public static GameState NewGame(long seed)
        {
            var gs = new GameState { worldSeed = seed, currentZone = ZoneDb.PrologueId };
            var rng = new Rng(Hash.Combine((ulong)seed, 0, 0x57A27));
            var brush = ItemGen.Generate(rng, 1, Rarity.Normal, b => b.Id == "bristle_brush");
            var apron = ItemGen.Generate(rng, 1, Rarity.Normal, b => b.Id == "apron");
            gs.hero.equipment.Add(new EquippedItem { slot = (int)EquipSlot.Brush, item = brush });
            gs.hero.equipment.Add(new EquippedItem { slot = (int)EquipSlot.Coat, item = apron });
            HeroOps.UnlockSkills(gs.hero);
            gs.Unlock(ZoneDb.PrologueId);
            QuestOps.Refresh(gs);
            return gs;
        }

        /// <summary>Seed очередного захода в зону: каждый заход — новая карта, но повторяемая.</summary>
        public static ulong ZoneSeed(GameState gs, string zoneId)
        {
            if (zoneId == ZoneDb.PrologueId) return Hash.Combine((ulong)gs.worldSeed, 1, 0);
            return Hash.Combine((ulong)gs.worldSeed, zoneId.GetHashCodeStable(), gs.VisitsOf(zoneId));
        }

        /// <summary>Победа над боссом зоны: открывает следующую зону и лагерь.</summary>
        public static void OnZoneBossKilled(GameState gs, ZoneDef z)
        {
            if (z.Kind == ZoneKind.Prologue)
            {
                gs.prologueDone = true;
                gs.Unlock(ZoneDb.HubId);
            }
            gs.Unlock(z.Unlocks);
            QuestOps.Refresh(gs);
        }
    }

    static class StringHash
    {
        /// <summary>string.GetHashCode в .NET случаен между запусками — свой стабильный FNV-1a.</summary>
        public static int GetHashCodeStable(this string s)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in s) { h ^= c; h *= 16777619; }
                return (int)h;
            }
        }
    }
}
