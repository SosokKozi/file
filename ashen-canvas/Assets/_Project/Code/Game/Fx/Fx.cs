using System.Collections.Generic;
using AshenCanvas.Game.Art;
using AshenCanvas.Game.Cam;
using UnityEngine;

namespace AshenCanvas.Game.Fx
{
    public sealed class FloatText
    {
        public Vector3 World;
        public string Text;
        public Color Color;
        public float Age, Life = 0.9f;
        public bool Big;
    }

    /// <summary>
    /// Все эффекты: капли краски (одна система частиц на всю игру), кляксы на полу,
    /// вспышки, кольца, круги предупреждений и всплывающие числа.
    /// Случайность здесь — только косметика, поэтому можно UnityEngine.Random.
    /// </summary>
    public sealed class FxSystem : MonoBehaviour
    {
        public static FxSystem I { get; private set; }
        public static readonly List<FloatText> Texts = new List<FloatText>();

        const int DecalCount = 160;
        const float DecalLife = 12f;

        ParticleSystem drops;
        Transform floorPlane;
        readonly Transform[] decals = new Transform[DecalCount];
        readonly Material[] decalMats = new Material[DecalCount];
        readonly float[] decalBorn = new float[DecalCount];
        readonly Color[] decalColor = new Color[DecalCount];
        int nextDecal;

        sealed class Pulse
        {
            public Transform T;
            public Material M;
            public float Age, Life, From, To, Alpha;
            public bool Flat;
        }
        readonly List<Pulse> pulses = new List<Pulse>();

        void Awake()
        {
            I = this;
            floorPlane = new GameObject("FloorPlane").transform;
            floorPlane.SetParent(transform, false);

            var go = new GameObject("PaintDrops");
            go.transform.SetParent(transform, false);
            drops = go.AddComponent<ParticleSystem>();
            drops.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = drops.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 2.2f;
            main.maxParticles = 3000;
            main.startLifetime = 1f;
            var emission = drops.emission;
            emission.enabled = false;
            var shape = drops.shape;
            shape.enabled = false;
            var size = drops.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            var col = drops.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.Planes;
            col.SetPlane(0, floorPlane);
            col.bounce = 0.15f;
            col.dampen = 0.7f;
            col.lifetimeLoss = 0.2f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.NewUnlit(Color.white, true);
            drops.Play();

            for (int i = 0; i < DecalCount; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(q.GetComponent<Collider>());
                q.name = "Decal";
                q.transform.SetParent(transform, false);
                q.transform.localEulerAngles = new Vector3(90, 0, 0);
                decalMats[i] = Mats.NewUnlit(Color.clear, true);
                var rend = q.GetComponent<Renderer>();
                rend.sharedMaterial = decalMats[i];
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                q.SetActive(false);
                decals[i] = q.transform;
            }
        }

        /// <summary>Убирает кляксы и частицы при смене зоны.</summary>
        public void Clear()
        {
            drops.Clear();
            foreach (var d in decals) d.gameObject.SetActive(false);
            foreach (var p in pulses) if (p.T != null) Destroy(p.T.gameObject);
            pulses.Clear();
            Texts.Clear();
        }

        // ---------- Публичные эффекты ----------

        /// <summary>Брызги краски во все стороны и вверх.</summary>
        public static void Splash(Vector3 pos, Color c, int count = 18, float speed = 6f, float size = 0.35f)
        {
            if (I == null) return;
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                var dir = Random.insideUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 1.4f + 0.3f;
                ep.position = pos + Vector3.up * 0.6f;
                ep.velocity = dir.normalized * speed * Random.Range(0.4f, 1.1f);
                ep.startColor = Random.value < 0.75f ? c : Mats.RandomPaint();
                ep.startSize = size * Random.Range(0.6f, 1.4f);
                ep.startLifetime = Random.Range(0.6f, 1.1f);
                I.drops.Emit(ep, 1);
            }
        }

        /// <summary>Яркий взрыв: вспышка, кольцо, брызги и кляксы.</summary>
        public static void Explosion(Vector3 pos, Color c, float radius, float shake = 0.25f)
        {
            if (I == null) return;
            I.AddPulse(PrimitiveType.Sphere, pos + Vector3.up * 0.5f, c, radius * 0.3f, radius * 1.4f, 0.25f, 0.85f, false);
            I.AddPulse(PrimitiveType.Cylinder, pos + Vector3.up * 0.05f, Color.Lerp(c, Color.white, 0.4f), radius * 0.4f, radius * 2.1f, 0.35f, 0.7f, true);
            Splash(pos, c, Mathf.RoundToInt(16 + radius * 8), 5f + radius * 1.5f, 0.4f);
            int blots = Mathf.Clamp(Mathf.RoundToInt(radius * 1.5f), 2, 8);
            for (int i = 0; i < blots; i++)
            {
                var off = Random.insideUnitCircle * radius * 0.8f;
                Decal(pos + new Vector3(off.x, 0, off.y), i == 0 ? c : Color.Lerp(c, Mats.RandomPaint(), 0.35f), Random.Range(0.6f, 1.4f) * Mathf.Max(1f, radius * 0.5f));
            }
            if (shake > 0f) CameraRig.Shake(shake);
        }

        /// <summary>Кольцо волны на полу.</summary>
        public static void Ring(Vector3 pos, Color c, float radius, float life = 0.35f)
        {
            if (I == null) return;
            I.AddPulse(PrimitiveType.Cylinder, pos + Vector3.up * 0.06f, c, 0.5f, radius * 2f, life, 0.8f, true);
        }

        /// <summary>Клякса на полу. Самые старые исчезают первыми.</summary>
        public static void Decal(Vector3 pos, Color c, float size)
        {
            if (I == null) return;
            int i = I.nextDecal;
            I.nextDecal = (I.nextDecal + 1) % DecalCount;
            var t = I.decals[i];
            t.gameObject.SetActive(true);
            t.position = new Vector3(pos.x, 0.02f + i * 0.0004f, pos.z);
            t.localEulerAngles = new Vector3(90, Random.Range(0f, 360f), 0);
            t.localScale = new Vector3(size * Random.Range(0.8f, 1.3f), size, 1f);
            c.a = 0.9f;
            I.decalColor[i] = c;
            I.decalMats[i].color = c;
            I.decalBorn[i] = Time.time;
        }

        public static void Text(Vector3 world, string text, Color c, bool big = false)
        {
            Texts.Add(new FloatText { World = world + Vector3.up * 2f, Text = text, Color = c, Big = big, Life = big ? 1.3f : 0.9f });
            if (Texts.Count > 80) Texts.RemoveAt(0);
        }

        // ---------- Внутреннее ----------

        void AddPulse(PrimitiveType type, Vector3 pos, Color c, float from, float to, float life, float alpha, bool flat)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var m = Mats.NewUnlit(c, flat);
            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = m;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pulses.Add(new Pulse { T = go.transform, M = m, Life = life, From = from, To = to, Alpha = alpha, Flat = flat });
            Apply(pulses[pulses.Count - 1], 0f);
        }

        static void Apply(Pulse p, float k)
        {
            float s = Mathf.Lerp(p.From, p.To, 1f - (1f - k) * (1f - k));
            p.T.localScale = p.Flat ? new Vector3(s, 0.01f, s) : new Vector3(s, s, s);
            var c = p.M.color;
            c.a = p.Alpha * (1f - k);
            p.M.color = c;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = pulses.Count - 1; i >= 0; i--)
            {
                var p = pulses[i];
                p.Age += dt;
                if (p.Age >= p.Life || p.T == null)
                {
                    if (p.T != null) Destroy(p.T.gameObject);
                    Destroy(p.M);
                    pulses.RemoveAt(i);
                    continue;
                }
                Apply(p, p.Age / p.Life);
            }

            float now = Time.time;
            for (int i = 0; i < DecalCount; i++)
            {
                if (!decals[i].gameObject.activeSelf) continue;
                float age = now - decalBorn[i];
                if (age > DecalLife) { decals[i].gameObject.SetActive(false); continue; }
                if (age > DecalLife - 2f)
                {
                    var c = decalColor[i];
                    c.a = 0.9f * (DecalLife - age) / 2f;
                    decalMats[i].color = c;
                }
            }

            for (int i = Texts.Count - 1; i >= 0; i--)
            {
                Texts[i].Age += dt;
                Texts[i].World += Vector3.up * dt * 1.6f;
                if (Texts[i].Age > Texts[i].Life) Texts.RemoveAt(i);
            }
        }
    }

    /// <summary>Круг или полоса предупреждения: заполняется к моменту удара.</summary>
    public sealed class Telegraph
    {
        readonly GameObject outer, inner;
        readonly Material outerMat, innerMat;
        readonly float size;
        readonly bool line;

        Telegraph(GameObject outer, GameObject inner, Material om, Material im, float size, bool line)
        {
            this.outer = outer; this.inner = inner; outerMat = om; innerMat = im; this.size = size; this.line = line;
        }

        public static Telegraph Circle(Vector3 pos, float radius, Color c)
        {
            var o = MakeDisc(pos, radius * 2f, new Color(c.r, c.g, c.b, 0.25f), out var om);
            var i = MakeDisc(pos + Vector3.up * 0.005f, 0.01f, new Color(c.r, c.g, c.b, 0.5f), out var im);
            return new Telegraph(o, i, om, im, radius * 2f, false);
        }

        /// <summary>Полоса от <paramref name="from"/> в сторону <paramref name="dir"/> длиной length.</summary>
        public static Telegraph Line(Vector3 from, Vector3 dir, float length, float width, Color c)
        {
            var rot = Quaternion.LookRotation(dir);
            var center = from + dir * length * 0.5f;
            var o = MakeQuad(center, rot, width, length, new Color(c.r, c.g, c.b, 0.25f), out var om);
            var i = MakeQuad(center, rot, width, 0.01f, new Color(c.r, c.g, c.b, 0.5f), out var im);
            i.transform.position = from + Vector3.up * 0.005f;
            return new Telegraph(o, i, om, im, length, true) { lineFrom = from, lineDir = dir, lineWidth = width };
        }

        Vector3 lineFrom, lineDir;
        float lineWidth;

        public void SetProgress(float k)
        {
            if (inner == null) return;
            k = Mathf.Clamp01(k);
            if (line)
            {
                float len = Mathf.Max(0.01f, size * k);
                inner.transform.position = lineFrom + lineDir * len * 0.5f + Vector3.up * 0.005f;
                inner.transform.localScale = new Vector3(lineWidth, len, 1f);
            }
            else
            {
                float s = Mathf.Max(0.01f, size * k);
                inner.transform.localScale = new Vector3(s, s, 1f);
            }
        }

        public void Destroy()
        {
            if (outer != null) Object.Destroy(outer);
            if (inner != null) Object.Destroy(inner);
            if (outerMat != null) Object.Destroy(outerMat);
            if (innerMat != null) Object.Destroy(innerMat);
        }

        /// <summary>Кладём в корень зоны: при смене зоны исчезнет вместе с ней, даже если ударивший уже пропал.</summary>
        static GameObject NewQuad()
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var root = Boot.GameRoot.I;
            if (root != null && root.ZoneRoot != null) q.transform.SetParent(root.ZoneRoot, false);
            return q;
        }

        static GameObject MakeDisc(Vector3 pos, float size, Color c, out Material m)
        {
            var q = NewQuad();
            q.transform.position = new Vector3(pos.x, 0.04f, pos.z);
            q.transform.eulerAngles = new Vector3(90, 0, 0);
            q.transform.localScale = new Vector3(size, size, 1f);
            m = Mats.NewUnlit(c, true);
            q.GetComponent<Renderer>().sharedMaterial = m;
            return q;
        }

        static GameObject MakeQuad(Vector3 center, Quaternion rot, float width, float length, Color c, out Material m)
        {
            var q = NewQuad();
            q.transform.position = new Vector3(center.x, 0.04f, center.z);
            q.transform.rotation = rot * Quaternion.Euler(90, 0, 0);
            q.transform.localScale = new Vector3(width, length, 1f);
            m = Mats.NewUnlit(c);
            q.GetComponent<Renderer>().sharedMaterial = m;
            return q;
        }
    }
}
