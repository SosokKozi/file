using System.Collections.Generic;
using AshenCanvas.Game.Art;
using AshenCanvas.Game.Boot;
using AshenCanvas.Game.Cam;
using AshenCanvas.Game.Combat;
using AshenCanvas.Game.Fx;
using AshenCanvas.Game.Input;
using AshenCanvas.Sim.Progression;
using AshenCanvas.Sim.Skills;
using AshenCanvas.Sim.Stats;
using UnityEngine;

namespace AshenCanvas.Game.Player
{
    /// <summary>Мирра: бег на WASD, прицел мышью, навыки на ЛКМ/ПКМ/Q/E/R/F, зелье на 1.</summary>
    public sealed class Hero : MonoBehaviour
    {
        public const float BaseSpeed = 5.5f;

        public StatBlock Stats = new StatBlock();
        public float Life, Mana;
        public float Radius = 0.45f;
        public bool Dead;
        public Vector3 Aim;
        public readonly Dictionary<string, float> Cooldowns = new Dictionary<string, float>();
        public Vector3 Pos => transform.position;

        Transform body, brushPivot;
        Material brushTipMat;
        float swingT = 1f, swingDir = 1f, hurtT;
        Vector3 dashFrom, dashTo;
        float dashT = -1f, dashLen;
        System.Action<Vector3> dashTick;

        public bool Dashing => dashT >= 0f;

        public void Init()
        {
            var model = AssetProvider.Spawn("hero", transform);
            model.name = "Model";
            body = AssetProvider.FindDeep(model.transform, "Body");
            brushPivot = AssetProvider.FindDeep(model.transform, "BrushPivot");
            var tip = AssetProvider.FindDeep(model.transform, "BrushTip");
            if (tip != null && tip.GetComponent<Renderer>() != null) brushTipMat = tip.GetComponent<Renderer>().material;
            RefreshStats(true);
        }

        public void RefreshStats(bool fill)
        {
            Stats = HeroOps.ComputeStats(GameRoot.I.State.hero);
            if (fill) { Life = Stats[Stat.MaxLife]; Mana = Stats[Stat.MaxMana]; }
            Life = Mathf.Min(Life, Stats[Stat.MaxLife]);
            Mana = Mathf.Min(Mana, Stats[Stat.MaxMana]);
        }

        public void PlaceAt(Vector3 p)
        {
            transform.position = p;
            dashT = -1f;
        }

        public float CooldownLeft(string id) => Cooldowns.TryGetValue(id, out float t) ? Mathf.Max(0f, t - Time.time) : 0f;

        void Update()
        {
            var root = GameRoot.I;
            if (root == null || root.Paused || Dead) return;
            float dt = Time.deltaTime;

            Life = Mathf.Min(Stats[Stat.MaxLife], Life + Stats[Stat.LifeRegen] * dt);
            Mana = Mathf.Min(Stats[Stat.MaxMana], Mana + Stats[Stat.ManaRegen] * dt);

            var mouse = GameInput.MousePosition;
            if (CameraRig.I != null && CameraRig.I.GroundPoint(mouse, out var g)) Aim = g;

            if (Dashing) { UpdateDash(dt); Animate(dt, true); return; }

            // Движение относительно камеры (она смотрит строго вдоль +Z).
            var mv = GameInput.Move;
            var dir = new Vector3(mv.x, 0f, mv.y);
            bool moving = dir.sqrMagnitude > 0.01f;
            if (moving)
            {
                float speed = BaseSpeed * (1f + Stats[Stat.MoveSpeedPct] / 100f);
                var p = Pos;
                root.Map.MoveCircle(ref p.x, ref p.z, dir.x * speed * dt, dir.z * speed * dt, Radius);
                transform.position = p;
            }

            bool overUi = root.UI != null && root.UI.PointerOverUi(mouse);
            bool uiBusy = root.UI != null && root.UI.BlocksGameplay;
            if (!uiBusy)
            {
                for (int slot = 0; slot < HeroOps.HotbarSize; slot++)
                {
                    var act = GameInput.SkillAct(slot);
                    bool mouseSlot = slot < 2;
                    if (mouseSlot && overUi) continue;
                    if (!GameInput.Held(act)) continue;
                    var hotbar = root.State.hero.hotbar;
                    if (slot < hotbar.Count && !string.IsNullOrEmpty(hotbar[slot]))
                        SkillCaster.TryCast(this, hotbar[slot]);
                }
                if (GameInput.Down(Act.Potion)) DrinkPotion();
            }

            var face = Aim - Pos; face.y = 0f;
            if (swingT < 1f && face.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(face);
            else if (moving) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 14f);
            Animate(dt, moving);
        }

        public void FaceAim()
        {
            var face = Aim - Pos; face.y = 0f;
            if (face.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(face);
        }

        public void Swing(Element e, float speedScale)
        {
            swingT = 0f;
            swingDir = -swingDir;
            if (brushTipMat != null) brushTipMat.color = Mats.Hex(Names.ColorOf(e == Element.Raw ? Element.Crimson : e));
        }

        public void StartDash(Vector3 to, float duration, System.Action<Vector3> onTick)
        {
            dashFrom = Pos;
            dashTo = to;
            dashLen = duration;
            dashT = 0f;
            dashTick = onTick;
        }

        void UpdateDash(float dt)
        {
            dashT += dt;
            float k = Mathf.Clamp01(dashT / dashLen);
            var want = Vector3.Lerp(dashFrom, dashTo, k);
            var p = Pos;
            GameRoot.I.Map.MoveCircle(ref p.x, ref p.z, want.x - p.x, want.z - p.z, Radius);
            transform.position = p;
            dashTick?.Invoke(p);
            if (k >= 1f) { dashT = -1f; dashTick = null; }
        }

        public void DrinkPotion()
        {
            var h = GameRoot.I.State.hero;
            if (h.potions <= 0 || Life >= Stats[Stat.MaxLife]) return;
            h.potions--;
            Life = Mathf.Min(Stats[Stat.MaxLife], Life + Stats[Stat.MaxLife] * 0.6f);
            FxSystem.Splash(Pos, Mats.Hex(0xFF5F7A), 20, 4f);
            FxSystem.Text(Pos, "+здоровье", Mats.Hex(0xFF7A8A));
        }

        /// <summary>Урон уже после защиты.</summary>
        public void ApplyDamage(float dmg)
        {
            if (Dead) return;
            Life -= dmg;
            hurtT = 1f;
            CameraRig.Shake(Mathf.Clamp(dmg / Mathf.Max(1f, Stats[Stat.MaxLife]) * 2f, 0.08f, 0.5f));
            if (Life <= 0f)
            {
                Life = 0f;
                Dead = true;
                FxSystem.Explosion(Pos, Mats.Hex(0x4B2E83), 2.5f, 0.6f);
                GameRoot.I.OnHeroDied();
            }
        }

        public void Revive()
        {
            Dead = false;
            RefreshStats(true);
            if (body != null) body.gameObject.SetActive(true);
        }

        void Animate(float dt, bool moving)
        {
            if (body == null) return;
            swingT = Mathf.Min(1f, swingT + dt * 5f);
            hurtT = Mathf.Max(0f, hurtT - dt * 4f);
            float bob = moving ? Mathf.Abs(Mathf.Sin(Time.time * 11f)) * 0.08f : Mathf.Sin(Time.time * 2f) * 0.015f;
            body.localPosition = new Vector3(0f, bob, 0f);
            body.localRotation = Quaternion.Euler(moving ? 8f : 0f, 0f, hurtT * 10f);
            if (brushPivot != null)
            {
                // Взмах: кисть описывает дугу вокруг героини и возвращается.
                float s = swingT < 1f ? Mathf.Sin(swingT * Mathf.PI) : 0f;
                brushPivot.localRotation = Quaternion.Euler(-s * 50f, swingDir * (s * 140f - 20f * s), 0f);
            }
            body.gameObject.SetActive(!Dead);
        }
    }
}
