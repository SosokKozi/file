using System.Collections.Generic;
using AshenCanvas.Game.Art;
using AshenCanvas.Sim.Core;
using AshenCanvas.Sim.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace AshenCanvas.Game.World
{
    /// <summary>Строит меши пола и стен из сетки зоны, свет, туман и декор.</summary>
    public static class ZoneView
    {
        public static void Build(DungeonLayout map, ZoneDef z, Transform root, ulong seed)
        {
            var pal = z.Palette;
            var floorA = Mats.Hex(pal.Floor);
            var floorB = Color.Lerp(floorA, Color.black, 0.12f);
            var wall = Mats.Hex(pal.Wall);
            var wallTop = Color.Lerp(wall, Color.white, 0.18f);
            bool outdoor = z.Kind == ZoneKind.Wild && z.Id != "ink_catacombs";
            var rng = new Rng(Hash.Combine(seed, 3, 3));

            var fa = new MeshData(); var fb = new MeshData();
            var walls = new MeshData(); var tops = new MeshData();
            float T = DungeonLayout.TileSize;
            for (int y = 0; y < map.H; y++)
                for (int x = 0; x < map.W; x++)
                {
                    if (map.IsFloor(x, y))
                    {
                        var target = ((Hash.Combine(seed, x, y) & 3) == 0) ? fb : fa;
                        target.Quad(new Vector3(x * T, 0, y * T), T);
                    }
                    else if (NearFloor(map, x, y))
                    {
                        float h = outdoor ? 1.4f + rng.NextFloat() * 1.6f : 2.2f;
                        var min = new Vector3(x * T, 0, y * T);
                        walls.Box(min, new Vector3(T, h, T));
                        tops.Quad(new Vector3(x * T, h + 0.01f, y * T), T);
                    }
                }

            MakeMesh("Floor A", fa, floorA, root, false);
            MakeMesh("Floor B", fb, floorB, root, false);
            MakeMesh("Walls", walls, wall, root, true);
            MakeMesh("Wall Tops", tops, wallTop, root, false);

            foreach (var p in map.Props)
            {
                var go = AssetProvider.Spawn("prop_" + p.Kind.ToString().ToLowerInvariant(), root);
                DungeonLayout.CellToWorld(p.At, out float px, out float pz);
                go.transform.position = new Vector3(px, 0, pz);
                go.transform.rotation = Quaternion.Euler(0, rng.NextInt(360), 0);
            }

            if (z.Kind == ZoneKind.Hub)
            {
                var f = AssetProvider.Spawn("prop_fountain", root);
                f.transform.position = new Vector3(17 * T, 0, 14 * T);
            }

            SetupLighting(pal, root);
        }

        static void SetupLighting(ZonePalette pal, Transform root)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Mats.Hex(pal.Ambient);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Mats.Hex(pal.Fog);
            RenderSettings.fogStartDistance = 16f;
            RenderSettings.fogEndDistance = 16f + 0.9f / Mathf.Max(0.001f, pal.FogDensity);
            if (Camera.main != null) Camera.main.backgroundColor = Mats.Hex(pal.Fog);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.color = Mats.Hex(pal.Light);
            sun.intensity = pal.LightIntensity;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        }

        static bool NearFloor(DungeonLayout map, int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (map.IsFloor(x + dx, y + dy)) return true;
            return false;
        }

        static void MakeMesh(string name, MeshData d, Color c, Transform root, bool shadows)
        {
            if (d.V.Count == 0) return;
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(d.V);
            mesh.SetNormals(d.N);
            mesh.SetTriangles(d.Tri, 0);
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mats.Lit(c);
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        sealed class MeshData
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<int> Tri = new List<int>();

            public void Quad(Vector3 min, float size)
            {
                Face(min, min + new Vector3(0, 0, size), min + new Vector3(size, 0, size), min + new Vector3(size, 0, 0), Vector3.up);
            }

            public void Box(Vector3 min, Vector3 size)
            {
                var max = min + size;
                // Четыре боковые грани; верх рисуется отдельным мешем другого цвета, низ не нужен.
                Face(new Vector3(min.x, min.y, min.z), new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(max.x, min.y, min.z), Vector3.back);
                Face(new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z), new Vector3(min.x, min.y, max.z), Vector3.forward);
                Face(new Vector3(min.x, min.y, max.z), new Vector3(min.x, max.y, max.z), new Vector3(min.x, max.y, min.z), new Vector3(min.x, min.y, min.z), Vector3.left);
                Face(new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z), new Vector3(max.x, min.y, max.z), Vector3.right);
            }

            /// <summary>Четырёхугольник a-b-c-d по часовой стрелке, если смотреть со стороны нормали.</summary>
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
            {
                int i = V.Count;
                V.Add(a); V.Add(b); V.Add(c); V.Add(d);
                N.Add(n); N.Add(n); N.Add(n); N.Add(n);
                Tri.Add(i); Tri.Add(i + 1); Tri.Add(i + 2);
                Tri.Add(i); Tri.Add(i + 2); Tri.Add(i + 3);
            }
        }
    }
}
