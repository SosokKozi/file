using System.Collections;
using System.Collections.Generic;
using AshenCanvas.Game.Art;
using AshenCanvas.Game.Boot;
using AshenCanvas.Game.Enemies;
using AshenCanvas.Game.Fx;
using AshenCanvas.Game.Player;
using AshenCanvas.Sim.Progression;
using AshenCanvas.Sim.Skills;
using AshenCanvas.Sim.Stats;
using UnityEngine;

namespace AshenCanvas.Game.Combat
{
    /// <summary>Исполнение навыков героини: урон берётся из Sim (DamageCalc), здесь — только геометрия и эффекты.</summary>
    public static class SkillCaster
    {
        static float lastNoMana;

        public static Color ColorOf(SkillDef s)
            => s.Element == Element.Raw ? Mats.RandomPaint() : Mats.Hex(Names.ColorOf(s.Element));

        public static bool TryCast(Hero hero, string id)
        {
            var root = GameRoot.I;
            var def = SkillDb.Get(id);
            if (def == null) return false;
            int rank = HeroOps.RankOf(root.State.hero, id);
            if (rank <= 0 || hero.CooldownLeft(id) > 0f || hero.Dashing) return false;
            if (hero.Mana < def.ManaCost)
            {
                if (Time.time - lastNoMana > 1f) { lastNoMana = Time.time; FxSystem.Text(hero.Pos, "Нет маны", Mats.Hex(0x7A9CFF)); }
                return false;
            }

            var st = hero.Stats;
            hero.Mana -= def.ManaCost;
            float cd = SkillDb.CooldownFor(def, st);
            if (def.Kind == SkillKind.MeleeArc || def.Kind == SkillKind.Projectile) cd *= SkillDb.CastTimeScale(st);
            hero.Cooldowns[id] = Time.time + cd;
            hero.FaceAim();
            hero.Swing(def.Element, 1f);

            float area = Mathf.Sqrt(1f + st[Stat.AreaPct] / 100f);
            var dir = hero.Aim - hero.Pos; dir.y = 0f;
            float aimDist = dir.magnitude;
            dir = aimDist > 0.01f ? dir / aimDist : hero.transform.forward;

            switch (def.Kind)
            {
                case SkillKind.MeleeArc: Melee(root, hero, def, rank, dir, area); break;
                case SkillKind.Projectile:
                    for (int i = 0; i < def.Count; i++)
                    {
                        float a = (i - (def.Count - 1) * 0.5f) * 14f;
                        PaintBolt.Fire(root, hero.Pos + Vector3.up * 1f + dir * 0.6f, Quaternion.Euler(0, a, 0) * dir, def, rank, area);
                    }
                    break;
                case SkillKind.Nova: Nova(root, hero, def, rank, area); break;
                case SkillKind.Dash: Dash(root, hero, def, rank, dir, aimDist); break;
                case SkillKind.GroundBurst:
                {
                    var at = hero.Pos + dir * Mathf.Min(aimDist, def.Range);
                    root.StartCoroutine(BurstLater(root, at, def, rank, def.Radius * area));
                    break;
                }
                case SkillKind.Pool:
                {
                    var at = hero.Pos + dir * Mathf.Min(aimDist, def.Range);
                    InkPool.Create(root, at, def, rank, def.Radius * area);
                    break;
                }
                case SkillKind.Masterpiece: root.StartCoroutine(Masterpiece(root, hero, def, rank, def.Radius * area)); break;
            }
            return true;
        }

        static void Melee(GameRoot root, Hero hero, SkillDef def, int rank, Vector3 dir, float area)
        {
            float range = def.Range * area;
            float halfArc = def.Radius * 0.5f;
            var c = ColorOf(def);
            foreach (var e in Snapshot(root))
            {
                var d = e.Pos - hero.Pos; d.y = 0f;
                if (d.magnitude > range + e.Radius) continue;
                if (d.sqrMagnitude > 0.01f && Vector3.Angle(dir, d) > halfArc) continue;
                root.HitEnemy(e, def, rank, 1f);
                FxSystem.Splash(e.Pos, c, 10, 5f);
            }
            for (int i = -2; i <= 2; i++)
            {
                var p = hero.Pos + Quaternion.Euler(0, i * halfArc * 0.4f, 0) * dir * range * 0.7f;
                FxSystem.Splash(p, Color.Lerp(c, Mats.RandomPaint(), 0.3f), 4, 3f, 0.3f);
            }
            FxSystem.Decal(hero.Pos + dir * range * 0.6f, c, 0.9f);
        }

        static void Nova(GameRoot root, Hero hero, SkillDef def, int rank, float area)
        {
            float r = def.Radius * area;
            var c = ColorOf(def);
            foreach (var e in Snapshot(root))
            {
                if ((e.Pos - hero.Pos).sqrMagnitude > (r + e.Radius) * (r + e.Radius)) continue;
                e.Slow(def.SlowPct, def.Duration);
                root.HitEnemy(e, def, rank, 1f);
            }
            FxSystem.Ring(hero.Pos, c, r, 0.4f);
            FxSystem.Ring(hero.Pos, Color.white, r * 0.7f, 0.3f);
            for (int i = 0; i < 10; i++)
            {
                var p = hero.Pos + Quaternion.Euler(0, i * 36f, 0) * Vector3.forward * r * 0.8f;
                FxSystem.Splash(p, c, 5, 3f, 0.35f);
                FxSystem.Decal(p, c, 1f);
            }
        }

        static void Dash(GameRoot root, Hero hero, SkillDef def, int rank, Vector3 dir, float aimDist)
        {
            var hit = new HashSet<Enemy>();
            var to = hero.Pos + dir * Mathf.Clamp(aimDist, 2f, def.Range);
            var c = ColorOf(def);
            float lastDecal = 0f;
            hero.StartDash(to, 0.18f, p =>
            {
                foreach (var e in Snapshot(root))
                {
                    if (hit.Contains(e)) continue;
                    if ((e.Pos - p).sqrMagnitude > (def.Radius + e.Radius) * (def.Radius + e.Radius)) continue;
                    hit.Add(e);
                    root.HitEnemy(e, def, rank, 1f);
                    FxSystem.Splash(e.Pos, Mats.RandomPaint(), 10, 5f);
                }
                if (Time.time - lastDecal > 0.03f)
                {
                    lastDecal = Time.time;
                    FxSystem.Decal(p, Color.Lerp(c, Mats.RandomPaint(), 0.5f), 0.8f);
                }
            });
        }

        static IEnumerator BurstLater(GameRoot root, Vector3 at, SkillDef def, int rank, float radius)
        {
            var c = ColorOf(def);
            var tele = Telegraph.Circle(at, radius, c);
            float t = 0f;
            while (t < def.Delay)
            {
                if (!root.Paused) t += Time.deltaTime;
                tele.SetProgress(t / def.Delay);
                yield return null;
            }
            tele.Destroy();
            HitArea(root, at, radius, def, rank, 1f);
            FxSystem.Explosion(at, c, radius, 0.3f);
        }

        static IEnumerator Masterpiece(GameRoot root, Hero hero, SkillDef def, int rank, float radius)
        {
            var center = hero.Pos;
            for (int i = 0; i < Mats.Paint.Length; i++)
            {
                var p = center + Quaternion.Euler(0, i * 360f / Mats.Paint.Length, 0) * Vector3.forward * radius * 0.55f;
                FxSystem.Explosion(p, Mats.Paint[i], radius * 0.35f, 0.15f);
                yield return new WaitForSeconds(0.05f);
            }
            HitArea(root, center, radius, def, rank, 1f);
            FxSystem.Explosion(center, Color.white, radius * 0.6f, 0.8f);
            FxSystem.Ring(center, Mats.Hex(0xFFC928), radius, 0.5f);
        }

        public static void HitArea(GameRoot root, Vector3 at, float radius, SkillDef def, int rank, float mult)
        {
            foreach (var e in Snapshot(root))
            {
                var d = e.Pos - at; d.y = 0f;
                if (d.sqrMagnitude <= (radius + e.Radius) * (radius + e.Radius)) root.HitEnemy(e, def, rank, mult);
            }
        }

        /// <summary>Копия списка живых врагов: удар может убить врага и изменить исходный список.</summary>
        static List<Enemy> Snapshot(GameRoot root)
        {
            var list = new List<Enemy>(root.Enemies.Count);
            foreach (var e in root.Enemies) if (!e.Dead) list.Add(e);
            return list;
        }
    }

    /// <summary>Капля «Брызг»: летит к цели, лопается взрывом.</summary>
    public sealed class PaintBolt : MonoBehaviour
    {
        SkillDef def;
        int rank;
        float area, travelled;
        Vector3 dir;
        Color color;
        const float Speed = 20f;

        public static void Fire(GameRoot root, Vector3 pos, Vector3 dir, SkillDef def, int rank, float area)
        {
            var c = SkillCaster.ColorOf(def);
            var go = PlaceholderFactory.P(root.ZoneRoot, PrimitiveType.Sphere, pos, Vector3.one * 0.5f, c).gameObject;
            go.GetComponent<Renderer>().sharedMaterial = Mats.Unlit(c);
            var b = go.AddComponent<PaintBolt>();
            b.def = def; b.rank = rank; b.area = area; b.dir = dir; b.color = c;
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.startWidth = 0.4f;
            trail.endWidth = 0f;
            trail.sharedMaterial = Mats.Unlit(c);
        }

        void Update()
        {
            var root = GameRoot.I;
            if (root == null || root.Paused) return;
            float step = Speed * Time.deltaTime;
            transform.position += dir * step;
            travelled += step;
            var p = transform.position;
            foreach (var e in root.Enemies)
            {
                if (e.Dead) continue;
                var d = e.Pos - p; d.y = 0f;
                if (d.sqrMagnitude < (e.Radius + 0.35f) * (e.Radius + 0.35f)) { Burst(root, p); return; }
            }
            if (travelled >= def.Range || !root.Map.IsWalkableWorld(p.x, p.z)) Burst(root, p);
        }

        void Burst(GameRoot root, Vector3 p)
        {
            p.y = 0f;
            float r = def.Radius * area;
            SkillCaster.HitArea(root, p, r, def, rank, 1f);
            FxSystem.Explosion(p, color, r, 0.06f);
            Destroy(gameObject);
        }
    }

    /// <summary>Чернильная лужа: жжёт врагов 4 раза в секунду.</summary>
    public sealed class InkPool : MonoBehaviour
    {
        SkillDef def;
        int rank;
        float radius, age, tick;
        Color color;

        public static void Create(GameRoot root, Vector3 at, SkillDef def, int rank, float radius)
        {
            var go = new GameObject("InkPool");
            go.transform.SetParent(root.ZoneRoot, false);
            go.transform.position = new Vector3(at.x, 0f, at.z);
            var pool = go.AddComponent<InkPool>();
            pool.def = def; pool.rank = rank; pool.radius = radius;
            pool.color = SkillCaster.ColorOf(def);
            FxSystem.Decal(at, pool.color, radius * 2f);
            FxSystem.Splash(at, pool.color, 24, 5f);
        }

        void Update()
        {
            var root = GameRoot.I;
            if (root == null || root.Paused) return;
            age += Time.deltaTime;
            tick -= Time.deltaTime;
            if (tick <= 0f)
            {
                tick = 0.25f;
                SkillCaster.HitArea(root, transform.position, radius, def, rank, 1f);
                var off = Random.insideUnitCircle * radius * 0.8f;
                FxSystem.Splash(transform.position + new Vector3(off.x, 0f, off.y), color, 3, 2.5f, 0.3f);
            }
            if (age >= def.Duration) Destroy(gameObject);
        }
    }
}
