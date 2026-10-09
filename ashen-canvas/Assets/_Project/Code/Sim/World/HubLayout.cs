namespace AshenCanvas.Sim.World
{
    /// <summary>Лагерь собран вручную: площадь, фонтан в центре, торговцы по краям.</summary>
    public static class HubLayout
    {
        public const string Gouache = "npc_gouache";   // задания
        public const string Sanguine = "npc_sanguine"; // кисти и палитры
        public const string Indigo = "npc_indigo";     // одежда, украшения, зелья
        public const string Waypoint = "npc_waypoint"; // мольберт путей

        public static DungeonLayout Build(ZoneDef z)
        {
            var map = new DungeonLayout(34, 28);
            map.CarveRect(2, 2, 30, 24);
            // Фонтан — сплошной блок в центре; его обходят.
            for (int y = 12; y < 16; y++)
                for (int x = 15; x < 19; x++) map.Tiles[y * map.W + x] = 0;
            // Палатки по углам.
            for (int y = 20; y < 25; y++)
                for (int x = 3; x < 7; x++) map.Tiles[y * map.W + x] = 0;
            for (int y = 20; y < 25; y++)
                for (int x = 27; x < 31; x++) map.Tiles[y * map.W + x] = 0;

            var all = new RoomRect(2, 2, 30, 24);
            map.Rooms.Add(all);
            map.StartRoom = 0;
            map.BossRoom = 0;
            map.Start = new Cell(17, 5);
            map.Boss = map.Start;
            map.Exit = map.Start;

            map.Npcs.Add(new NpcSpot { Id = Gouache, At = new Cell(10, 14) });
            map.Npcs.Add(new NpcSpot { Id = Sanguine, At = new Cell(8, 19) });
            map.Npcs.Add(new NpcSpot { Id = Indigo, At = new Cell(26, 19) });
            map.Npcs.Add(new NpcSpot { Id = Waypoint, At = new Cell(17, 21) });

            int[,] props =
            {
                { 13, 11, (int)PropKind.Lamp }, { 21, 11, (int)PropKind.Lamp }, { 13, 17, (int)PropKind.Lamp }, { 21, 17, (int)PropKind.Lamp },
                { 7, 18, (int)PropKind.Easel }, { 9, 18, (int)PropKind.Crate }, { 25, 18, (int)PropKind.Bucket }, { 28, 18, (int)PropKind.Crate },
                { 3, 3, (int)PropKind.Bucket }, { 30, 3, (int)PropKind.Easel }, { 15, 23, (int)PropKind.Easel }, { 19, 23, (int)PropKind.Easel },
            };
            for (int i = 0; i < props.GetLength(0); i++)
                map.Props.Add(new PropSpawn { At = new Cell(props[i, 0], props[i, 1]), Kind = (PropKind)props[i, 2] });
            return map;
        }
    }
}
