using System.Collections.Generic;
using AshenCanvas.Game.Art;
using AshenCanvas.Game.Boot;
using AshenCanvas.Game.Fx;
using AshenCanvas.Sim.Enemies;
using AshenCanvas.Sim.Stats;
using AshenCanvas.Sim.World;
using UnityEngine;

namespace AshenCanvas.Game.Enemies
{
    /// <summary>
    /// Враг: преследование по карте расстояний, предупреждение перед каждым ударом,
    /// поведение по архетипу. Боссы чередуют приёмы и зовут подмогу на 70% и 35% здоровья.
    /// </summary>
    public sealed class Enemy : MonoBehaviour
    {
        const float AggroRange = 14f;

        public EnemyDef Def;
        public MonsterRarity Rarity;
        public RareMod Mod;
        public int Level;
        public float Life, MaxLife, Damage, Speed, Radius;
        public bool Dead, Aggro;
        public string DisplayName;
        public readonly StatBlock Defence = new StatBlock();

        public bool IsBoss => Rarity == MonsterRarity.Boss;
        public Vector3 Pos => transform.position;

        enum St { Idle, Chase, Windup, Charging, Recover }
        enum Move { Melee, Slam, Volley, Charge, Shoot, Explode }

        St state;
        Move move;
        float stateT, windupLen, cd, slowT, slowPct, punch, regenPct;
        Telegraph tele;
        Vector3 lockedPoint, chargeDir;
        float chargeLeft;
        bool chargeHit, summoned70, summoned35;
        int bossRotation;
        Transform body;
        Color elementColor;

        public void Init(EnemyDef d, int level, MonsterRarity r, RareMod mod)
        {
            Def = d; Level = level; Rarity = r; Mod = mod;
            MaxLife = EnemyDb.LifeAt(d, level, r) * (mod != null ? mod.LifeMult : 1f);
            Life = MaxLife;
            Damage = EnemyDb.DamageAt(d, level, r) * (mod != null ? mod.DamageMult : 1f);
            Speed = d.Speed * (mod != null ? mod.SpeedMult : 1f) * (r == MonsterRarity.Magic ? 1.1f : 1f);
            regenPct = mod != null ? mod.RegenPctPerSec : 0f;
            float scale = d.Size * (r == MonsterRarity.Rare ? 1.2f : r == MonsterRarity.Magic ? 1.08f : 1f);
            Radius = 0.45f * scale;
            Defence[Stat.Armor] = d.Armor * (1f + 0.1f * (level - 1));
            // Враг сопротивляется своей стихии.
            if (d.Element != Element.Raw) Defence[StatBlock.ResFor(d.Element)] = 30f;
            DisplayName = mod != null ? mod.Name + " " + d.Name.ToLowerInvariant() : d.Name;
            elementColor = Mats.Hex(Names.ColorOf(d.Element == Element.Raw ? Element.Crimson : d.Element));
            cd = Random.Range(0.3f, 1.2f);

            var model = AssetProvider.Spawn(d.AssetId, transform);
            model.name = "Model";
            model.transform.localScale = Vector3.one * scale;
            body = AssetProvider.FindDeep(model.transform, "Body");
            if (body == null) body = model.transform;

            if (r == MonsterRarity.Magic || r == MonsterRarity.Rare)
            {
                var ring = PlaceholderFactory.P(transform, PrimitiveType.Cylinder, new Vector3(0, 0.03f, 0), new Vector3(Radius * 3.2f, 0.01f, Radius * 3.2f), Color.white);
                ring.GetComponent<Renderer>().sharedMaterial = Mats.Unlit(r == MonsterRarity.Rare ? Mats.Hex(0xFFE35A, 0.55f) : Mats.Hex(0x7A9CFF, 0.5f));
            }
        }

        public void Slow(float pct, float duration)
        {
            slowPct = Mathf.Max(slowPct, pct);
            slowT = Mathf.Max(slowT, duration);
        }

        /// <summary>Урон уже после защиты. Возвращает true, если враг погиб.</summary>
        public bool ApplyDamage(float dmg)
        {
            if (Dead) return false;
            Life -= dmg;
            punch = 1f;
            Aggro = true;
            if (Life <= 0f)
            {
                Dead = true;
                CancelTelegraph();
                return true;
            }
            return false;
        }

        void OnDestroy() => CancelTelegraph();

        void CancelTelegraph()
        {
            tele?.Destroy();
            tele = null;
        }

        void Update()
        {
            var root = GameRoot.I;
            if (Dead || root == null || root.Hero == null || root.Paused) return;
            float dt = Time.deltaTime;
            if (regenPct > 0f) Life = Mathf.Min(MaxLife, Life + MaxLife * regenPct / 100f * dt);
            if (slowT > 0f) { slowT -= dt; if (slowT <= 0f) slowPct = 0f; }
            float speed = Speed * (1f - slowPct / 100f);

            var hero = root.Hero;
            Vector3 toHero = hero.Pos - Pos; toHero.y = 0f;
            float dist = toHero.magnitude;
            bool heroAlive = !hero.Dead;

            if (!Aggro)
            {
                if (heroAlive && dist < AggroRange && root.Map.LineClear(Pos.x, Pos.z, hero.Pos.x, hero.Pos.z))
                {
                    Aggro = true;
                    root.AlertAround(Pos, 8f);
                }
                Animate(dt, false);
                return;
            }

            cd -= dt;
            stateT += dt;
            bool moving = false;

            switch (state)
            {
                case St.Idle:
                case St.Chase:
                case St.Recover:
                    if (state == St.Recover && stateT < 0.25f) break;
                    if (!heroAlive) break;
                    if (IsBoss) BossThink(root, dist);
                    else Think(root, dist);
                    if (state == St.Chase || state == St.Idle || state == St.Recover)
                        moving = Chase(root, toHero, dist, speed, dt);
                    break;

                case St.Windup:
                    tele?.SetProgress(stateT / windupLen);
                    Face(move == Move.Slam ? lockedPoint - Pos : toHero, dt * 3f);
                    if (stateT >= windupLen) Release(root, hero.Pos);
                    break;

                case St.Charging:
                    float step = 15f * dt;
                    var p = Pos;
                    float bx = p.x, bz = p.z;
                    root.Map.MoveCircle(ref p.x, ref p.z, chargeDir.x * step, chargeDir.z * step, Radius);
                    transform.position = p;
                    chargeLeft -= step;
                    if (!chargeHit && (hero.Pos - Pos).sqrMagnitude < (Radius + 0.8f) * (Radius + 0.8f))
                    {
                        chargeHit = true;
                        root.HeroTakeDamage(Damage * 1.5f, Def.Element, Pos);
                    }
                    bool blocked = Mathf.Abs(p.x - bx) + Mathf.Abs(p.z - bz) < step * 0.3f;
                    if (chargeLeft <= 0f || blocked)
                    {
                        if (blocked) { FxSystem.Splash(Pos, Mats.Hex(0x8E8A96), 12); Cam.CameraRig.Shake(0.15f); }
                        Enter(St.Recover);
                        cd = Def.AttackCooldown;
                    }
                    break;
            }
            Separate(root);
            Animate(dt, moving);
        }

        void Think(GameRoot root, float dist)
        {
            if (cd > 0f) return;
            bool los = root.Map.LineClear(Pos.x, Pos.z, root.Hero.Pos.x, root.Hero.Pos.z);
            switch (Def.Archetype)
            {
                case Archetype.Melee:
                    if (dist <= Def.AttackRange + root.Hero.Radius) StartWindup(Move.Melee, Def.Windup);
                    break;
                case Archetype.Ranged:
                    if (dist <= Def.AttackRange && los) StartWindup(Move.Shoot, Def.Windup);
                    break;
                case Archetype.Charger:
                    if (dist <= Def.AttackRange && los) StartWindup(Move.Charge, Def.Windup);
                    break;
                case Archetype.Exploder:
                    if (dist <= Def.AttackRange) StartWindup(Move.Explode, Def.Windup);
                    break;
            }
        }

        void BossThink(GameRoot root, float dist)
        {
            if ((Def.Moves & BossMove.Summon) != 0)
            {
                if (!summoned70 && Life < MaxLife * 0.7f) { summoned70 = true; Summon(root); }
                if (!summoned35 && Life < MaxLife * 0.35f) { summoned35 = true; Summon(root); }
            }
            if (cd > 0f) return;

            var options = new List<Move>();
            if ((Def.Moves & BossMove.Slam) != 0) options.Add(Move.Slam);
            if ((Def.Moves & BossMove.Volley) != 0) options.Add(Move.Volley);
            if ((Def.Moves & BossMove.Charge) != 0) options.Add(Move.Charge);

            if (dist <= Def.AttackRange + root.Hero.Radius && bossRotation % 3 != 2)
            {
                bossRotation++;
                StartWindup(Move.Melee, Def.Windup * 0.8f);
                return;
            }
            if (options.Count == 0 || dist > 16f) return;
            var m = options[bossRotation % options.Count];
            bossRotation++;
            if (m == Move.Charge && !root.Map.LineClear(Pos.x, Pos.z, root.Hero.Pos.x, root.Hero.Pos.z)) m = options[0];
            StartWindup(m, m == Move.Volley ? 0.7f : Def.Windup * 1.2f);
        }

        void StartWindup(Move m, float len)
        {
            var hero = GameRoot.I.Hero;
            move = m;
            windupLen = len;
            Enter(St.Windup);
            var dir = hero.Pos - Pos; dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
            dir.Normalize();
            var warn = Color.Lerp(elementColor, new Color(1f, 0.35f, 0.2f), 0.5f);
            switch (m)
            {
                case Move.Melee:
                    lockedPoint = Pos + dir * (Def.AttackRange * 0.55f);
                    tele = Telegraph.Circle(lockedPoint, Def.AttackRange * 0.65f + Radius * 0.3f, warn);
                    break;
                case Move.Slam:
                    lockedPoint = hero.Pos;
                    tele = Telegraph.Circle(lockedPoint, 4f, warn);
                    break;
                case Move.Charge:
                    chargeDir = dir;
                    tele = Telegraph.Line(Pos, dir, Def.Archetype == Archetype.Boss ? 13f : Def.AttackRange + 2f, Radius * 2.4f, warn);
                    break;
                case Move.Explode:
                    lockedPoint = Pos;
                    tele = Telegraph.Circle(Pos, Def.BlastRadius, warn);
                    break;
                case Move.Shoot:
                case Move.Volley:
                    lockedPoint = hero.Pos;
                    break;
            }
        }

        void Release(GameRoot root, Vector3 heroPos)
        {
            CancelTelegraph();
            var hero = root.Hero;
            switch (move)
            {
                case Move.Melee:
                {
                    float r = Def.AttackRange * 0.65f + Radius * 0.3f + hero.Radius;
                    if ((hero.Pos - lockedPoint).sqrMagnitude <= r * r) root.HeroTakeDamage(Damage, Def.Element, Pos);
                    FxSystem.Splash(lockedPoint, Mats.Hex(0x8E8A96), 8, 4f);
                    break;
                }
                case Move.Slam:
                    if ((hero.Pos - lockedPoint).sqrMagnitude <= (4f + hero.Radius) * (4f + hero.Radius))
                        root.HeroTakeDamage(Damage * 1.8f, Def.Element, lockedPoint);
                    FxSystem.Explosion(lockedPoint, elementColor, 4f, 0.35f);
                    break;
                case Move.Charge:
                    chargeLeft = Def.Archetype == Archetype.Boss ? 13f : Def.AttackRange + 2f;
                    chargeHit = false;
                    Enter(St.Charging);
                    return;
                case Move.Explode:
                    if ((hero.Pos - Pos).sqrMagnitude <= (Def.BlastRadius + hero.Radius) * (Def.BlastRadius + hero.Radius))
                        root.HeroTakeDamage(Damage, Def.Element, Pos);
                    FxSystem.Explosion(Pos, elementColor, Def.BlastRadius, 0.3f);
                    root.KillEnemy(this);
                    return;
                case Move.Shoot:
                {
                    var dir = heroPos - Pos; dir.y = 0f;
                    EnemyBolt.Fire(root, Pos + Vector3.up * 0.8f, dir.normalized, Def.ProjectileSpeed, Damage, Def.Element, elementColor);
                    break;
                }
                case Move.Volley:
                {
                    int n = 14;
                    for (int i = 0; i < n; i++)
                    {
                        var dir = Quaternion.Euler(0, i * 360f / n, 0) * Vector3.forward;
                        EnemyBolt.Fire(root, Pos + Vector3.up * 1f, dir, 8f, Damage * 0.8f, Def.Element, elementColor);
                    }
                    FxSystem.Ring(Pos, elementColor, 3f);
                    break;
                }
            }
            Enter(St.Recover);
            cd = Def.AttackCooldown * (IsBoss ? 1f : Random.Range(0.85f, 1.15f));
        }

        void Summon(GameRoot root)
        {
            int count = 3 + (Level >= 8 ? 1 : 0);
            var def = EnemyDb.Get(Def.SummonId);
            for (int i = 0; i < count; i++)
            {
                var off = Quaternion.Euler(0, i * 360f / count, 0) * Vector3.forward * 3f;
                var p = Pos + off;
                if (!root.Map.CircleFree(p.x, p.z, 0.5f)) p = Pos;
                var e = root.SpawnEnemy(def, Level, MonsterRarity.Normal, null, p);
                e.Aggro = true;
            }
            FxSystem.Ring(Pos, elementColor, 6f, 0.6f);
            FxSystem.Text(Pos, "На помощь!", elementColor, true);
        }

        bool Chase(GameRoot root, Vector3 toHero, float dist, float speed, float dt)
        {
            Vector3 dir;
            float keep = Def.Archetype == Archetype.Ranged ? Def.AttackRange * 0.8f : Def.AttackRange * 0.8f;
            bool los = root.Map.LineClear(Pos.x, Pos.z, root.Hero.Pos.x, root.Hero.Pos.z);
            if (Def.Archetype == Archetype.Ranged && los && dist < 4.5f) dir = -toHero.normalized;     // отходит
            else if (los && dist <= keep) { Face(toHero, dt * 6f); return false; }
            else if (los) dir = toHero.normalized;
            else
            {
                var cell = DungeonLayout.WorldToCell(Pos.x, Pos.z);
                if (!root.Flow.NextStep(cell, out var next)) return false;
                DungeonLayout.CellToWorld(next, out float nx, out float nz);
                dir = new Vector3(nx - Pos.x, 0f, nz - Pos.z).normalized;
            }
            var p = Pos;
            root.Map.MoveCircle(ref p.x, ref p.z, dir.x * speed * dt, dir.z * speed * dt, Radius);
            transform.position = p;
            Face(dir.sqrMagnitude > 0.01f ? dir : toHero, dt * 8f);
            if (state != St.Chase) Enter(St.Chase);
            return true;
        }

        void Separate(GameRoot root)
        {
            var p = Pos;
            foreach (var o in root.Enemies)
            {
                if (o == this || o.Dead) continue;
                var d = p - o.Pos; d.y = 0f;
                float min = Radius + o.Radius;
                float sq = d.sqrMagnitude;
                if (sq < min * min && sq > 0.0001f)
                {
                    var push = d.normalized * (min - Mathf.Sqrt(sq)) * 0.5f;
                    root.Map.MoveCircle(ref p.x, ref p.z, push.x, push.z, Radius);
                }
            }
            transform.position = p;
        }

        void Face(Vector3 dir, float k)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Mathf.Clamp01(k));
        }

        void Enter(St s) { state = s; stateT = 0f; }

        void Animate(float dt, bool moving)
        {
            if (body == null) return;
            punch = Mathf.Max(0f, punch - dt * 6f);
            float bob = moving ? Mathf.Abs(Mathf.Sin(Time.time * 10f + transform.position.x)) * 0.12f : 0f;
            float wind = state == St.Windup ? Mathf.Clamp01(stateT / windupLen) : 0f;
            float sx = 1f + punch * 0.25f + wind * 0.15f;
            float sy = 1f - punch * 0.2f + wind * 0.1f;
            body.localScale = new Vector3(sx, sy, sx);
            body.localPosition = new Vector3(0f, bob, 0f);
            body.localRotation = Quaternion.Euler(-wind * 15f, 0f, 0f);
        }
    }

    /// <summary>Сгусток краски врага. Летит по прямой, бьёт героиню, лопается о стену.</summary>
    public sealed class EnemyBolt : MonoBehaviour
    {
        Vector3 dir;
        float speed, damage, life = 4f;
        Element element;
        Color color;

        public static void Fire(GameRoot root, Vector3 pos, Vector3 dir, float speed, float damage, Element e, Color c)
        {
            var go = PlaceholderFactory.P(root.ZoneRoot, PrimitiveType.Sphere, pos, Vector3.one * 0.45f, c).gameObject;
            go.GetComponent<Renderer>().sharedMaterial = Mats.Unlit(c);
            var b = go.AddComponent<EnemyBolt>();
            b.dir = dir; b.speed = speed; b.damage = damage; b.element = e; b.color = c;
        }

        void Update()
        {
            var root = GameRoot.I;
            if (root == null || root.Paused) return;
            float dt = Time.deltaTime;
            transform.position += dir * speed * dt;
            life -= dt;
            var p = transform.position;
            var hero = root.Hero;
            if (hero != null && !hero.Dead)
            {
                var d = hero.Pos - p; d.y = 0f;
                if (d.sqrMagnitude < (hero.Radius + 0.3f) * (hero.Radius + 0.3f))
                {
                    root.HeroTakeDamage(damage, element, p);
                    Pop(p);
                    return;
                }
            }
            if (life <= 0f || !root.Map.IsWalkableWorld(p.x, p.z)) Pop(p);
        }

        void Pop(Vector3 p)
        {
            FxSystem.Splash(p, color, 8, 3f, 0.25f);
            FxSystem.Decal(p, color, 0.7f);
            Destroy(gameObject);
        }
    }
}
