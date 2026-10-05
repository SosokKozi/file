using System;

namespace StormPath.Sim.Gen
{
    public enum NodeType : byte { Start, Tutorial, Sanctuary, Level, Elite, Shop, Riddle, Camp, Final }

    /// <summary>Wastes — Пустоши (обучение, привал), остальные — биомы акта.</summary>
    public enum BiomeId : byte { None, Wastes, Inferno, Chronowaste, StormPeaks }

    [Serializable]
    public struct MapNode
    {
        public int row, col;
        public NodeType type;
        public BiomeId biome;
        public float k;
        public ulong levelSeed;
        /// <summary>Индексы точек следующего ряда в <see cref="RouteMap.nodes"/>, по возрастанию.</summary>
        public int[] next;
    }

    /// <summary>Точки лежат по возрастанию (ряд, колонка); первая — старт, последняя — финал.</summary>
    [Serializable]
    public sealed class RouteMap
    {
        public ulong seed;
        public int act;
        public MapNode[] nodes;
        public int current;
    }
}
