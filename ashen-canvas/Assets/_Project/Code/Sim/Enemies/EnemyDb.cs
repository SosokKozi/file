using System;
using System.Collections.Generic;
using AshenCanvas.Sim.Stats;

namespace AshenCanvas.Sim.Enemies
{
    public enum Archetype { Melee, Ranged, Charger, Exploder, Boss }

    public enum MonsterRarity { Normal, Magic, Rare, Boss }

    [Flags]
    public enum BossMove { None = 0, Slam = 1, Volley = 2, Summon = 4, Charge = 8 }

    public sealed class EnemyDef
    {
        public string Id;
        public string Name;
        public Archetype Archetype;
        public Element Element;
        public float Life;
        public float Damage;
        public float Speed;        // м/с
        public float AttackRange;  // м
        public float AttackCooldown;
        public float Windup;       // предупреждение перед ударом, с
        public float Armor;
        public float Size = 1f;
        public int Xp;
        public float ProjectileSpeed = 10f;
        public float BlastRadius = 3f;
        public BossMove Moves;
        public string SummonId;
        public string AssetId;     // модель: каталог ассетов или болванка
    }

    public sealed class RareMod
    {
        public string Id, Name;
        public float LifeMult = 1f, DamageMult = 1f, SpeedMult = 1f, RegenPctPerSec;
    }

    public static class EnemyDb
    {
        public static readonly IReadOnlyList<EnemyDef> All = new List<EnemyDef>
        {
            new EnemyDef { Id = "faded_husk", Name = "Выцветший", Archetype = Archetype.Melee, Element = Element.Raw,
                Life = 22, Damage = 5, Speed = 3.2f, AttackRange = 1.9f, AttackCooldown = 1.4f, Windup = 0.45f, Xp = 6, AssetId = "enemy_husk" },
            new EnemyDef { Id = "grey_spitter", Name = "Серый плевун", Archetype = Archetype.Ranged, Element = Element.Ink,
                Life = 14, Damage = 4, Speed = 2.6f, AttackRange = 9f, AttackCooldown = 2.2f, Windup = 0.5f, Xp = 7,
                ProjectileSpeed = 9f, Size = 0.9f, AssetId = "enemy_spitter" },
            new EnemyDef { Id = "mud_charger", Name = "Тинный таран", Archetype = Archetype.Charger, Element = Element.Raw,
                Life = 35, Damage = 9, Speed = 2.8f, AttackRange = 8f, AttackCooldown = 3.5f, Windup = 0.8f, Xp = 10,
                Armor = 10, Size = 1.25f, AssetId = "enemy_charger" },
            new EnemyDef { Id = "ink_bomb", Name = "Клякса", Archetype = Archetype.Exploder, Element = Element.Ink,
                Life = 10, Damage = 14, Speed = 4.6f, AttackRange = 2.2f, AttackCooldown = 99f, Windup = 0.6f, Xp = 5,
                BlastRadius = 3f, Size = 0.8f, AssetId = "enemy_inkbomb" },
            new EnemyDef { Id = "sketch_wolf", Name = "Набросок волка", Archetype = Archetype.Melee, Element = Element.Raw,
                Life = 18, Damage = 6, Speed = 5.5f, AttackRange = 1.9f, AttackCooldown = 1.0f, Windup = 0.3f, Xp = 7, AssetId = "enemy_wolf" },
            new EnemyDef { Id = "eraser_knight", Name = "Рыцарь-ластик", Archetype = Archetype.Melee, Element = Element.Raw,
                Life = 60, Damage = 13, Speed = 2.6f, AttackRange = 2.4f, AttackCooldown = 2.0f, Windup = 0.7f, Xp = 16,
                Armor = 25, Size = 1.35f, AssetId = "enemy_knight" },

            new EnemyDef { Id = "grey_herald", Name = "Серый Глашатай", Archetype = Archetype.Boss, Element = Element.Ink,
                Life = 260, Damage = 12, Speed = 2.8f, AttackRange = 2.8f, AttackCooldown = 1.6f, Windup = 0.9f, Xp = 80,
                Size = 2.2f, Moves = BossMove.Slam | BossMove.Volley | BossMove.Summon, SummonId = "faded_husk", AssetId = "boss_herald" },
            new EnemyDef { Id = "mire_matron", Name = "Тинная Матрона", Archetype = Archetype.Boss, Element = Element.Azure,
                Life = 600, Damage = 16, Speed = 2.6f, AttackRange = 3f, AttackCooldown = 1.5f, Windup = 0.9f, Xp = 200,
                Armor = 15, Size = 2.6f, Moves = BossMove.Slam | BossMove.Charge | BossMove.Summon, SummonId = "grey_spitter", AssetId = "boss_matron" },
            new EnemyDef { Id = "old_sepia", Name = "Старый Сепия", Archetype = Archetype.Boss, Element = Element.Crimson,
                Life = 1100, Damage = 22, Speed = 3.2f, AttackRange = 3f, AttackCooldown = 1.3f, Windup = 0.8f, Xp = 420,
                Armor = 25, Size = 2.6f, Moves = BossMove.Charge | BossMove.Volley | BossMove.Summon, SummonId = "sketch_wolf", AssetId = "boss_sepia" },
            new EnemyDef { Id = "grand_eraser", Name = "Великий Ластик", Archetype = Archetype.Boss, Element = Element.Golden,
                Life = 1800, Damage = 30, Speed = 3f, AttackRange = 3.2f, AttackCooldown = 1.2f, Windup = 0.75f, Xp = 800,
                Armor = 40, Size = 3f, Moves = BossMove.Slam | BossMove.Volley | BossMove.Charge | BossMove.Summon, SummonId = "ink_bomb", AssetId = "boss_eraser" },
        };

        public static readonly IReadOnlyList<RareMod> RareMods = new List<RareMod>
        {
            new RareMod { Id = "swift", Name = "Стремительный", SpeedMult = 1.4f },
            new RareMod { Id = "thick", Name = "Толстокожий", LifeMult = 1.8f },
            new RareMod { Id = "brutal", Name = "Свирепый", DamageMult = 1.4f },
            new RareMod { Id = "regen", Name = "Живучий", RegenPctPerSec = 2f },
        };

        public static EnemyDef Get(string id)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Id == id) return All[i];
            throw new ArgumentException("Нет врага " + id);
        }

        public static float LifeAt(EnemyDef d, int level, MonsterRarity r)
            => d.Life * (1f + 0.22f * (level - 1)) * RarityLifeMult(r);

        public static float DamageAt(EnemyDef d, int level, MonsterRarity r)
            => d.Damage * (1f + 0.12f * (level - 1)) * (r == MonsterRarity.Rare ? 1.25f : r == MonsterRarity.Magic ? 1.1f : 1f);

        public static int XpAt(EnemyDef d, int level, MonsterRarity r, int heroLevel)
        {
            float xp = d.Xp * (1f + 0.15f * (level - 1));
            if (r == MonsterRarity.Magic) xp *= 2.5f;
            else if (r == MonsterRarity.Rare) xp *= 5f;
            if (heroLevel - level > 4) xp *= 0.5f;
            return Math.Max(1, (int)Math.Round(xp));
        }

        static float RarityLifeMult(MonsterRarity r)
            => r == MonsterRarity.Magic ? 2.2f : r == MonsterRarity.Rare ? 4.5f : 1f;
    }
}
