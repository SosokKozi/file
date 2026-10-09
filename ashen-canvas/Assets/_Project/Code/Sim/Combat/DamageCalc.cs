using System;
using AshenCanvas.Sim.Core;
using AshenCanvas.Sim.Skills;
using AshenCanvas.Sim.Stats;

namespace AshenCanvas.Sim.Combat
{
    public struct Hit
    {
        public float Amount;
        public bool Crit;
        public Element Element;
    }

    public static class DamageCalc
    {
        public const float MaxResist = 75f;
        public const float MaxArmorReduction = 0.75f;

        /// <summary>Урон навыка героини до защиты цели.</summary>
        public static Hit RollSkill(SkillDef s, int rank, StatBlock st, Rng rng)
        {
            float wMin = st[Stat.WeaponMin] + st[Stat.WeaponFlat];
            float wMax = Math.Max(wMin, st[Stat.WeaponMax] + st[Stat.WeaponFlat]);
            float weapon = rng.Range(wMin, wMax);
            float baseDmg = weapon * s.Effectiveness + s.FlatDamage;
            float inc = st[Stat.DamagePct] + st[StatBlock.DamagePctFor(s.Element)] + (rank - 1) * SkillDb.RankDamagePct;
            float dmg = baseDmg * (1f + inc / 100f);
            bool crit = rng.NextFloat() * 100f < st[Stat.CritChance];
            if (crit) dmg *= st[Stat.CritMulti] / 100f;
            return new Hit { Amount = dmg, Crit = crit, Element = s.Element };
        }

        /// <summary>Сколько урона дойдёт до цели после брони или сопротивления.</summary>
        public static float Mitigate(float dmg, Element e, float armor, float resistPct)
        {
            if (dmg <= 0f) return 0f;
            if (e == Element.Raw)
            {
                float red = armor <= 0f ? 0f : Math.Min(MaxArmorReduction, armor / (armor + 5f * dmg));
                return dmg * (1f - red);
            }
            float res = Math.Max(-60f, Math.Min(MaxResist, resistPct));
            return dmg * (1f - res / 100f);
        }

        public static float MitigateFor(float dmg, Element e, StatBlock defender)
        {
            float res = e == Element.Raw ? 0f : defender[StatBlock.ResFor(e)];
            return Mitigate(dmg, e, defender[Stat.Armor], res);
        }
    }
}
