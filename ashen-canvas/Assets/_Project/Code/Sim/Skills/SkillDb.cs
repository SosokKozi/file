using System.Collections.Generic;
using AshenCanvas.Sim.Stats;

namespace AshenCanvas.Sim.Skills
{
    public enum SkillKind
    {
        MeleeArc,    // взмах перед героиней
        Projectile,  // снаряды веером, взрываются при попадании
        Nova,        // волна вокруг героини
        Dash,        // рывок с уроном по пути
        GroundBurst, // взрыв в точке курсора после задержки
        Pool,        // лужа, наносит урон по времени
        Masterpiece  // большой многоцветный взрыв
    }

    public sealed class SkillDef
    {
        public string Id;
        public string Name;
        public string Description;
        public SkillKind Kind;
        public Element Element;
        public int UnlockLevel;
        public int MaxRank = 10;
        public float Effectiveness;   // доля урона кисти
        public float FlatDamage;
        public float ManaCost;
        public float Cooldown;        // секунды
        public float Range;           // метры
        public float Radius;          // метры
        public int Count = 1;         // снарядов
        public float Delay;           // задержка взрыва
        public float Duration;        // для луж и замедления
        public float SlowPct;         // замедление врагов
    }

    public static class SkillDb
    {
        public const float RankDamagePct = 12f;

        public static readonly IReadOnlyList<SkillDef> All = new List<SkillDef>
        {
            new SkillDef {
                Id = "brush_stroke", Name = "Мазок", Kind = SkillKind.MeleeArc, Element = Element.Raw,
                Description = "Широкий взмах кистью перед собой.",
                UnlockLevel = 1, Effectiveness = 1.0f, FlatDamage = 2f, ManaCost = 0f, Cooldown = 0.45f,
                Range = 3.2f, Radius = 100f /* угол дуги, градусы */ },
            new SkillDef {
                Id = "splatter", Name = "Брызги", Kind = SkillKind.Projectile, Element = Element.Crimson,
                Description = "Веер алых капель. Каждая лопается маленьким взрывом.",
                UnlockLevel = 2, Effectiveness = 0.7f, FlatDamage = 3f, ManaCost = 6f, Cooldown = 0.6f,
                Range = 16f, Radius = 1.6f, Count = 3 },
            new SkillDef {
                Id = "azure_wave", Name = "Лазурная волна", Kind = SkillKind.Nova, Element = Element.Azure,
                Description = "Волна акварели вокруг. Замедляет врагов.",
                UnlockLevel = 3, Effectiveness = 0.9f, FlatDamage = 4f, ManaCost = 12f, Cooldown = 3f,
                Radius = 5.5f, Duration = 2.5f, SlowPct = 45f },
            new SkillDef {
                Id = "flourish", Name = "Росчерк", Kind = SkillKind.Dash, Element = Element.Raw,
                Description = "Рывок к курсору. Бьёт всех на пути.",
                UnlockLevel = 4, Effectiveness = 1.1f, FlatDamage = 3f, ManaCost = 8f, Cooldown = 2.5f,
                Range = 8f, Radius = 1.6f },
            new SkillDef {
                Id = "golden_burst", Name = "Золотой взрыв", Kind = SkillKind.GroundBurst, Element = Element.Golden,
                Description = "Через миг в точке курсора взрывается золотая краска.",
                UnlockLevel = 6, Effectiveness = 2.2f, FlatDamage = 8f, ManaCost = 18f, Cooldown = 4f,
                Range = 14f, Radius = 3.5f, Delay = 0.45f },
            new SkillDef {
                Id = "ink_flood", Name = "Чернильный разлив", Kind = SkillKind.Pool, Element = Element.Ink,
                Description = "Лужа чернил. Жжёт всех, кто в ней стоит.",
                UnlockLevel = 8, Effectiveness = 0.35f /* за тик, 4 тика в секунду */, FlatDamage = 1f,
                ManaCost = 15f, Cooldown = 6f, Range = 14f, Radius = 3.2f, Duration = 4f },
            new SkillDef {
                Id = "masterpiece", Name = "Шедевр", Kind = SkillKind.Masterpiece, Element = Element.Golden,
                Description = "Все краски разом. Огромный взрыв вокруг героини.",
                UnlockLevel = 10, MaxRank = 5, Effectiveness = 5f, FlatDamage = 25f, ManaCost = 40f,
                Cooldown = 20f, Radius = 9f },
        };

        public static SkillDef Get(string id)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Id == id) return All[i];
            return null;
        }

        /// <summary>Перезарядка с учётом бонуса скорости перезарядки.</summary>
        public static float CooldownFor(SkillDef s, StatBlock st) => s.Cooldown / (1f + st[Stat.CooldownPct] / 100f);

        /// <summary>Длительность анимации навыка (для «Мазка» это и есть темп атак).</summary>
        public static float CastTimeScale(StatBlock st) => 1f / (1f + st[Stat.CastSpeedPct] / 100f);
    }
}
