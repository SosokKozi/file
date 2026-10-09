using UnityEngine;

namespace AshenCanvas.Game.Art
{
    /// <summary>
    /// Болванки из примитивов: узнаваемый силуэт и цвет, пока нет настоящих моделей.
    /// Имена дочерних узлов (Body, BrushPivot, BrushTip) — часть договорённости с кодом анимации.
    /// </summary>
    public static class PlaceholderFactory
    {
        public static GameObject Build(string id)
        {
            var root = new GameObject(id);
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);

            switch (id)
            {
                case "hero": Hero(body); break;
                case "npc_gouache": Npc(body, Mats.Hex(0xE06A9A), Mats.Hex(0xFFE08A), true); break;
                case "npc_sanguine": Npc(body, Mats.Hex(0xA8432F), Mats.Hex(0x5A3A2A), false); break;
                case "npc_indigo": Fox(body); break;
                case "npc_waypoint": WaypointEasel(body); break;

                case "enemy_husk": Husk(body, 1f); break;
                case "enemy_spitter": Spitter(body); break;
                case "enemy_charger": Charger(body); break;
                case "enemy_inkbomb": InkBomb(body); break;
                case "enemy_wolf": Wolf(body); break;
                case "enemy_knight": Knight(body); break;
                case "boss_herald": Husk(body, 1f); Crown(body, 2.0f, Mats.Hex(0xBFB8C8)); break;
                case "boss_matron": Spitter(body); Crown(body, 1.3f, Mats.Hex(0x9BFF6A)); break;
                case "boss_sepia": Wolf(body); Crown(body, 1.4f, Mats.Hex(0xFF9A3A)); break;
                case "boss_eraser": Knight(body); Crown(body, 2.3f, Mats.Hex(0xFFC928)); break;

                case "prop_pillar": P(body, PrimitiveType.Cylinder, new Vector3(0, 1.6f, 0), new Vector3(0.9f, 1.6f, 0.9f), Mats.Hex(0x5A5466)); break;
                case "prop_candle": Candle(body); break;
                case "prop_easel": Easel(body, Mats.RandomPaint()); break;
                case "prop_bucket": Bucket(body); break;
                case "prop_rubble": Rubble(body); break;
                case "prop_tree": Tree(body); break;
                case "prop_crate": P(body, PrimitiveType.Cube, new Vector3(0, 0.45f, 0), new Vector3(0.9f, 0.9f, 0.9f), Mats.Hex(0x9A6A3A), new Vector3(0, 20, 0)); break;
                case "prop_lamp": Lamp(body); break;
                case "prop_fountain": Fountain(body); break;
                case "portal": Portal(body); break;

                case "loot_gold": P(body, PrimitiveType.Cylinder, new Vector3(0, 0.1f, 0), new Vector3(0.45f, 0.06f, 0.45f), Mats.Hex(0xFFC928)); break;
                case "loot_potion": Potion(body); break;
                default: LootItem(body, id); break;
            }
            return root.gameObject;
        }

        public static Transform P(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Color c, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(t);
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col);
            }
            go.GetComponent<Renderer>().sharedMaterial = Mats.Lit(c);
            var tr = go.transform;
            tr.SetParent(parent, false);
            tr.localPosition = pos;
            tr.localScale = scale;
            tr.localEulerAngles = euler;
            return tr;
        }

        static Transform Node(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        // --- Героиня: художница в берете, с огромной кистью ---
        static void Hero(Transform b)
        {
            var coat = Mats.Hex(0x4B2E83);
            P(b, PrimitiveType.Capsule, new Vector3(0, 0.85f, 0), new Vector3(0.62f, 0.62f, 0.5f), coat);
            P(b, PrimitiveType.Cylinder, new Vector3(0, 0.45f, 0), new Vector3(0.8f, 0.3f, 0.7f), Mats.Hex(0x3A2266)); // юбка
            P(b, PrimitiveType.Cube, new Vector3(0, 0.95f, 0.22f), new Vector3(0.42f, 0.5f, 0.06f), Mats.Hex(0xF2E6D0)); // фартук
            P(b, PrimitiveType.Sphere, new Vector3(0.06f, 0.82f, 0.26f), new Vector3(0.1f, 0.1f, 0.03f), Mats.Hex(0xFF3B4A));
            P(b, PrimitiveType.Sphere, new Vector3(-0.08f, 1.0f, 0.26f), new Vector3(0.08f, 0.08f, 0.03f), Mats.Hex(0x2FB8FF));
            P(b, PrimitiveType.Sphere, new Vector3(0, 1.52f, 0), new Vector3(0.46f, 0.46f, 0.46f), Mats.Hex(0xF6C9A8)); // голова
            P(b, PrimitiveType.Sphere, new Vector3(0, 1.58f, -0.1f), new Vector3(0.52f, 0.5f, 0.5f), Mats.Hex(0xFF7A3A)); // волосы
            P(b, PrimitiveType.Sphere, new Vector3(0.16f, 1.38f, -0.26f), new Vector3(0.22f, 0.32f, 0.2f), Mats.Hex(0xFF7A3A)); // хвостик
            P(b, PrimitiveType.Cylinder, new Vector3(0.05f, 1.78f, 0), new Vector3(0.56f, 0.05f, 0.56f), Mats.Hex(0xE8283A), new Vector3(0, 0, 12)); // берет
            P(b, PrimitiveType.Sphere, new Vector3(0.05f, 1.84f, 0), new Vector3(0.08f, 0.08f, 0.08f), Mats.Hex(0xE8283A));
            P(b, PrimitiveType.Sphere, new Vector3(0.09f, 1.54f, 0.21f), new Vector3(0.06f, 0.08f, 0.04f), Mats.Hex(0x1A1420)); // глаза
            P(b, PrimitiveType.Sphere, new Vector3(-0.09f, 1.54f, 0.21f), new Vector3(0.06f, 0.08f, 0.04f), Mats.Hex(0x1A1420));

            var pivot = Node(b, "BrushPivot", new Vector3(0.38f, 1.0f, 0.1f));
            P(pivot, PrimitiveType.Cylinder, new Vector3(0, 0.15f, 0.45f), new Vector3(0.08f, 0.6f, 0.08f), Mats.Hex(0x8A5A2A), new Vector3(70, 0, 0));
            P(pivot, PrimitiveType.Cylinder, new Vector3(0, 0.35f, 1.0f), new Vector3(0.11f, 0.06f, 0.11f), Mats.Hex(0xC0C0C8), new Vector3(70, 0, 0));
            var tip = P(pivot, PrimitiveType.Sphere, new Vector3(0, 0.42f, 1.22f), new Vector3(0.2f, 0.2f, 0.36f), Color.white, new Vector3(70, 0, 0));
            tip.name = "BrushTip";
            tip.GetComponent<Renderer>().sharedMaterial = Mats.NewLit(Mats.Hex(0xFF3B4A));
        }

        static void Npc(Transform b, Color robe, Color hat, bool bun)
        {
            P(b, PrimitiveType.Capsule, new Vector3(0, 0.9f, 0), new Vector3(0.75f, 0.9f, 0.6f), robe);
            P(b, PrimitiveType.Sphere, new Vector3(0, 1.85f, 0), new Vector3(0.5f, 0.5f, 0.5f), Mats.Hex(0xF0C0A0));
            if (bun) P(b, PrimitiveType.Sphere, new Vector3(0, 2.15f, -0.05f), new Vector3(0.32f, 0.32f, 0.32f), hat);
            else P(b, PrimitiveType.Cube, new Vector3(0, 0.95f, 0.32f), new Vector3(0.55f, 0.75f, 0.06f), hat); // кожаный фартук
            P(b, PrimitiveType.Sphere, new Vector3(0.1f, 1.88f, 0.22f), new Vector3(0.06f, 0.08f, 0.04f), Mats.Hex(0x1A1420));
            P(b, PrimitiveType.Sphere, new Vector3(-0.1f, 1.88f, 0.22f), new Vector3(0.06f, 0.08f, 0.04f), Mats.Hex(0x1A1420));
        }

        static void Fox(Transform b)
        {
            var fur = Mats.Hex(0x3A4FD8);
            P(b, PrimitiveType.Capsule, new Vector3(0, 0.85f, 0), new Vector3(0.65f, 0.85f, 0.55f), fur);
            P(b, PrimitiveType.Sphere, new Vector3(0, 1.75f, 0.05f), new Vector3(0.55f, 0.48f, 0.6f), fur);
            P(b, PrimitiveType.Cube, new Vector3(0.17f, 2.05f, 0), new Vector3(0.12f, 0.28f, 0.08f), fur, new Vector3(0, 0, -15));
            P(b, PrimitiveType.Cube, new Vector3(-0.17f, 2.05f, 0), new Vector3(0.12f, 0.28f, 0.08f), fur, new Vector3(0, 0, 15));
            P(b, PrimitiveType.Sphere, new Vector3(0, 1.68f, 0.34f), new Vector3(0.18f, 0.14f, 0.24f), Mats.Hex(0xF2E6D0));
            P(b, PrimitiveType.Capsule, new Vector3(0, 0.55f, -0.5f), new Vector3(0.3f, 0.5f, 0.3f), fur, new Vector3(-60, 0, 0));
            P(b, PrimitiveType.Cylinder, new Vector3(0, 2.2f, 0), new Vector3(0.5f, 0.12f, 0.5f), Mats.Hex(0xFFC928)); // шляпа
        }

        static void WaypointEasel(Transform b)
        {
            Easel(b, Mats.Hex(0x6BE36B));
            P(b, PrimitiveType.Cube, new Vector3(0, 1.7f, 0.12f), new Vector3(0.7f, 0.08f, 0.02f), Mats.Hex(0xFF5FA2));
            P(b, PrimitiveType.Cube, new Vector3(0.1f, 1.4f, 0.12f), new Vector3(0.08f, 0.5f, 0.02f), Mats.Hex(0x2FB8FF));
        }

        // --- Враги: выцветшие, серые, с яркими глазами ---
        static void Eyes(Transform b, float y, float z, float spread, Color c)
        {
            var m = Mats.Unlit(c);
            var l = P(b, PrimitiveType.Sphere, new Vector3(spread, y, z), Vector3.one * 0.12f, c);
            var r = P(b, PrimitiveType.Sphere, new Vector3(-spread, y, z), Vector3.one * 0.12f, c);
            l.GetComponent<Renderer>().sharedMaterial = m;
            r.GetComponent<Renderer>().sharedMaterial = m;
        }

        static void Husk(Transform b, float s)
        {
            var grey = Mats.Hex(0x8E8A96);
            P(b, PrimitiveType.Capsule, new Vector3(0, 0.85f, 0.05f) * s, new Vector3(0.6f, 0.75f, 0.5f) * s, grey, new Vector3(12, 0, 0));
            P(b, PrimitiveType.Sphere, new Vector3(0, 1.55f, 0.2f) * s, new Vector3(0.45f, 0.42f, 0.45f) * s, Mats.Hex(0xA6A2AE));
            P(b, PrimitiveType.Capsule, new Vector3(0.4f, 0.8f, 0.25f) * s, new Vector3(0.18f, 0.5f, 0.18f) * s, grey, new Vector3(30, 0, 10));
            P(b, PrimitiveType.Capsule, new Vector3(-0.4f, 0.8f, 0.25f) * s, new Vector3(0.18f, 0.5f, 0.18f) * s, grey, new Vector3(30, 0, -10));
            Eyes(b, 1.58f * s, 0.4f * s, 0.1f * s, Mats.Hex(0xFF7A3A));
        }

        static void Spitter(Transform b)
        {
            P(b, PrimitiveType.Sphere, new Vector3(0, 0.6f, 0), new Vector3(1.1f, 0.9f, 1.1f), Mats.Hex(0x7A7684));
            P(b, PrimitiveType.Cylinder, new Vector3(0, 0.75f, 0.5f), new Vector3(0.35f, 0.15f, 0.35f), Mats.Hex(0x3A2A4E), new Vector3(90, 0, 0));
            Eyes(b, 1.0f, 0.38f, 0.2f, Mats.Hex(0x8A3CFF));
        }

        static void Charger(Transform b)
        {
            var mud = Mats.Hex(0x6E6A5E);
            P(b, PrimitiveType.Cube, new Vector3(0, 0.75f, 0), new Vector3(1.1f, 0.9f, 1.5f), mud);
            P(b, PrimitiveType.Cube, new Vector3(0, 0.85f, 0.85f), new Vector3(0.8f, 0.6f, 0.4f), Mats.Hex(0x5A564C));
            P(b, PrimitiveType.Cylinder, new Vector3(0.35f, 1.1f, 1.0f), new Vector3(0.12f, 0.3f, 0.12f), Mats.Hex(0xE8E2D6), new Vector3(60, 0, 0));
            P(b, PrimitiveType.Cylinder, new Vector3(-0.35f, 1.1f, 1.0f), new Vector3(0.12f, 0.3f, 0.12f), Mats.Hex(0xE8E2D6), new Vector3(60, 0, 0));
            Eyes(b, 1.0f, 1.06f, 0.22f, Mats.Hex(0x9BFF6A));
        }

        static void InkBomb(Transform b)
        {
            var ink = Mats.Hex(0x2A2240);
            P(b, PrimitiveType.Sphere, new Vector3(0, 0.55f, 0), Vector3.one * 0.9f, ink);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                P(b, PrimitiveType.Sphere, new Vector3(0, 0.55f, 0) + dir * 0.45f, Vector3.one * 0.3f, ink);
            }
            Eyes(b, 0.75f, 0.4f, 0.15f, Mats.Hex(0xFFC928));
        }

        static void Wolf(Transform b)
        {
            var sketch = Mats.Hex(0xB8B0A0);
            P(b, PrimitiveType.Capsule, new Vector3(0, 0.7f, 0), new Vector3(0.5f, 0.7f, 0.5f), sketch, new Vector3(90, 0, 0));
            P(b, PrimitiveType.Sphere, new Vector3(0, 0.95f, 0.75f), new Vector3(0.45f, 0.42f, 0.55f), sketch);
            P(b, PrimitiveType.Cube, new Vector3(0, 0.85f, 1.05f), new Vector3(0.2f, 0.18f, 0.3f), Mats.Hex(0x4A4440));
            P(b, PrimitiveType.Cube, new Vector3(0.14f, 1.22f, 0.7f), new Vector3(0.1f, 0.22f, 0.06f), sketch);
            P(b, PrimitiveType.Cube, new Vector3(-0.14f, 1.22f, 0.7f), new Vector3(0.1f, 0.22f, 0.06f), sketch);
            for (int i = 0; i < 4; i++)
                P(b, PrimitiveType.Cylinder, new Vector3(i < 2 ? 0.18f : -0.18f, 0.25f, i % 2 == 0 ? 0.4f : -0.4f), new Vector3(0.12f, 0.25f, 0.12f), Mats.Hex(0x4A4440));
            P(b, PrimitiveType.Capsule, new Vector3(0, 0.85f, -0.8f), new Vector3(0.15f, 0.35f, 0.15f), sketch, new Vector3(-50, 0, 0));
            Eyes(b, 1.02f, 1.02f, 0.13f, Mats.Hex(0xFF3B4A));
        }

        static void Knight(Transform b)
        {
            var pink = Mats.Hex(0xC8A0A8);
            P(b, PrimitiveType.Cube, new Vector3(0, 0.95f, 0), new Vector3(0.9f, 1.2f, 0.6f), pink);
            P(b, PrimitiveType.Cube, new Vector3(0, 1.8f, 0), new Vector3(0.6f, 0.55f, 0.55f), Mats.Hex(0x8A8A96));
            P(b, PrimitiveType.Cube, new Vector3(0, 1.8f, 0.28f), new Vector3(0.45f, 0.08f, 0.02f), Mats.Hex(0xFFC928));
            P(b, PrimitiveType.Cube, new Vector3(0.65f, 1.0f, 0.3f), new Vector3(0.15f, 1.6f, 0.15f), Mats.Hex(0x6A6A76));
            P(b, PrimitiveType.Cube, new Vector3(-0.65f, 1.0f, 0.2f), new Vector3(0.12f, 1.1f, 0.9f), Mats.Hex(0x6A6A76));
        }

        static void Crown(Transform b, float y, Color c)
        {
            for (int i = 0; i < 5; i++)
            {
                var dir = Quaternion.Euler(0, i * 72f, 0) * Vector3.forward;
                P(b, PrimitiveType.Cube, new Vector3(0, y, 0) + dir * 0.22f, new Vector3(0.1f, 0.3f, 0.1f), c, new Vector3(0, i * 72f, 0));
            }
        }

        // --- Окружение ---
        static void Candle(Transform b)
        {
            P(b, PrimitiveType.Cylinder, new Vector3(0, 0.3f, 0), new Vector3(0.18f, 0.3f, 0.18f), Mats.Hex(0xE8E2D6));
            var flame = P(b, PrimitiveType.Sphere, new Vector3(0, 0.68f, 0), new Vector3(0.12f, 0.2f, 0.12f), Color.white);
            flame.GetComponent<Renderer>().sharedMaterial = Mats.Unlit(Mats.Hex(0xFFB040));
            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(b, false);
            light.transform.localPosition = new Vector3(0, 0.9f, 0);
            light.type = LightType.Point;
            light.color = Mats.Hex(0xFF9A4A);
            light.range = 6f;
            light.intensity = 1.6f;
        }

        static void Easel(Transform b, Color canvas)
        {
            var wood = Mats.Hex(0x9A6A3A);
            P(b, PrimitiveType.Cube, new Vector3(0.35f, 0.9f, 0), new Vector3(0.08f, 1.8f, 0.08f), wood, new Vector3(-8, 0, -8));
            P(b, PrimitiveType.Cube, new Vector3(-0.35f, 0.9f, 0), new Vector3(0.08f, 1.8f, 0.08f), wood, new Vector3(-8, 0, 8));
            P(b, PrimitiveType.Cube, new Vector3(0, 0.85f, -0.3f), new Vector3(0.08f, 1.7f, 0.08f), wood, new Vector3(18, 0, 0));
            P(b, PrimitiveType.Cube, new Vector3(0, 1.3f, 0.08f), new Vector3(0.95f, 0.75f, 0.04f), Mats.Hex(0xF4EBDC), new Vector3(-8, 0, 0));
            P(b, PrimitiveType.Sphere, new Vector3(0.1f, 1.35f, 0.12f), new Vector3(0.4f, 0.3f, 0.02f), canvas, new Vector3(-8, 0, 0));
        }

        static void Bucket(Transform b)
        {
            P(b, PrimitiveType.Cylinder, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.3f, 0.6f), Mats.Hex(0x8A8A96));
            P(b, PrimitiveType.Cylinder, new Vector3(0, 0.61f, 0), new Vector3(0.52f, 0.01f, 0.52f), Mats.RandomPaint());
        }

        static void Rubble(Transform b)
        {
            for (int i = 0; i < 4; i++)
                P(b, PrimitiveType.Cube, new Vector3(Random.Range(-0.5f, 0.5f), 0.2f, Random.Range(-0.5f, 0.5f)),
                  Vector3.one * Random.Range(0.3f, 0.6f), Mats.Hex(0x5A5466), new Vector3(Random.Range(0, 40), Random.Range(0, 90), 0));
        }

        static void Tree(Transform b)
        {
            P(b, PrimitiveType.Cylinder, new Vector3(0, 1.0f, 0), new Vector3(0.35f, 1.0f, 0.35f), Mats.Hex(0x5A4636));
            float h = Random.Range(2.2f, 2.8f);
            P(b, PrimitiveType.Sphere, new Vector3(0, h, 0), Vector3.one * Random.Range(1.6f, 2.2f), Mats.Hex(0x6E8A5E));
            P(b, PrimitiveType.Sphere, new Vector3(0.5f, h + 0.6f, 0.2f), Vector3.one * 1.2f, Mats.Hex(0x7E9A6A));
        }

        static void Lamp(Transform b)
        {
            P(b, PrimitiveType.Cylinder, new Vector3(0, 1.3f, 0), new Vector3(0.12f, 1.3f, 0.12f), Mats.Hex(0x3A3440));
            var bulb = P(b, PrimitiveType.Sphere, new Vector3(0, 2.75f, 0), Vector3.one * 0.45f, Color.white);
            bulb.GetComponent<Renderer>().sharedMaterial = Mats.Unlit(Mats.Hex(0xFFE08A));
            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(b, false);
            light.transform.localPosition = new Vector3(0, 2.6f, 0);
            light.type = LightType.Point;
            light.color = Mats.Hex(0xFFD08A);
            light.range = 8f;
            light.intensity = 1.2f;
        }

        static void Fountain(Transform b)
        {
            P(b, PrimitiveType.Cylinder, new Vector3(0, 0.4f, 0), new Vector3(7f, 0.4f, 7f), Mats.Hex(0xB8A88A));
            P(b, PrimitiveType.Cylinder, new Vector3(0, 0.82f, 0), new Vector3(6.2f, 0.02f, 6.2f), Mats.Hex(0x2FB8FF));
            P(b, PrimitiveType.Cylinder, new Vector3(0, 1.4f, 0), new Vector3(0.8f, 1.2f, 0.8f), Mats.Hex(0xB8A88A));
            P(b, PrimitiveType.Sphere, new Vector3(0, 2.8f, 0), Vector3.one * 1.2f, Mats.Hex(0xFF5FA2));
        }

        static void Portal(Transform b)
        {
            var ring = P(b, PrimitiveType.Cylinder, new Vector3(0, 1.4f, 0), new Vector3(2.2f, 0.05f, 2.2f), Color.white, new Vector3(90, 0, 0));
            ring.GetComponent<Renderer>().sharedMaterial = Mats.Unlit(Mats.Hex(0x2FB8FF, 0.85f));
            var inner = P(b, PrimitiveType.Cylinder, new Vector3(0, 1.4f, -0.02f), new Vector3(1.7f, 0.06f, 1.7f), Color.white, new Vector3(90, 0, 0));
            inner.GetComponent<Renderer>().sharedMaterial = Mats.Unlit(Mats.Hex(0xFF5FA2, 0.8f));
            inner.name = "Swirl";
        }

        static void Potion(Transform b)
        {
            var bottle = P(b, PrimitiveType.Sphere, new Vector3(0, 0.25f, 0), Vector3.one * 0.4f, Color.white);
            bottle.GetComponent<Renderer>().sharedMaterial = Mats.Unlit(Mats.Hex(0xFF3B4A));
            P(b, PrimitiveType.Cylinder, new Vector3(0, 0.5f, 0), new Vector3(0.12f, 0.1f, 0.12f), Mats.Hex(0x9A6A3A));
        }

        static void LootItem(Transform b, string id)
        {
            // item_brush, item_hat ... — пока общий свёрток-силуэт по слоту.
            Color c = Mats.Hex(0xE8E2D6);
            switch (id)
            {
                case "item_brush":
                    P(b, PrimitiveType.Cylinder, new Vector3(0, 0.12f, 0), new Vector3(0.08f, 0.45f, 0.08f), Mats.Hex(0x8A5A2A), new Vector3(90, 30, 0));
                    P(b, PrimitiveType.Sphere, new Vector3(0.22f, 0.12f, 0.38f), new Vector3(0.16f, 0.16f, 0.26f), Mats.RandomPaint(), new Vector3(0, 30, 0));
                    return;
                case "item_palette":
                    P(b, PrimitiveType.Cylinder, new Vector3(0, 0.06f, 0), new Vector3(0.7f, 0.03f, 0.5f), Mats.Hex(0xC08A5A));
                    for (int i = 0; i < 4; i++) P(b, PrimitiveType.Sphere, new Vector3(-0.2f + i * 0.13f, 0.1f, 0.1f), Vector3.one * 0.1f, Mats.Paint[i]);
                    return;
                case "item_ring":
                case "item_amulet":
                    P(b, PrimitiveType.Cylinder, new Vector3(0, 0.05f, 0), new Vector3(0.3f, 0.03f, 0.3f), Mats.Hex(0xFFC928));
                    P(b, PrimitiveType.Sphere, new Vector3(0, 0.12f, 0.12f), Vector3.one * 0.12f, Mats.RandomPaint());
                    return;
                default:
                    P(b, PrimitiveType.Cube, new Vector3(0, 0.15f, 0), new Vector3(0.55f, 0.25f, 0.4f), c, new Vector3(0, 25, 0));
                    return;
            }
        }
    }
}
