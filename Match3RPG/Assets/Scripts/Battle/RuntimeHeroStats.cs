using System;
using Match3RPG.Campaign;
using Match3RPG.Campaign.Data;
using Match3RPG.Persistence;

namespace Match3RPG.Battle
{
    public static class HeroLevelProgression
    {
        public const int MaximumLevel = 139;

        public static int GetExperienceToNextLevel(int level)
        {
            int currentLevel = Math.Max(1, Math.Min(MaximumLevel, level));
            long experience = 200;
            for (int index = 1; index < currentLevel; index++)
                experience = Math.Min(int.MaxValue, (experience * 5L + 3L) / 4L);
            return (int)experience;
        }

        public static void AddExperience(HeroProgressSaveData progress, int amount)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));
            if (amount <= 0)
                return;

            long experience = Math.Max(0, progress.experience) + (long)amount;
            int level = Math.Max(1, Math.Min(MaximumLevel, progress.level));
            while (level < MaximumLevel)
            {
                if (level == 9 || (level >= 19 && level % 10 == 9))
                    break;
                int threshold = GetExperienceToNextLevel(level);
                if (experience < threshold)
                    break;
                experience -= threshold;
                level++;
            }

            progress.level = level;
            progress.experience = level >= MaximumLevel
                ? 0
                : (int)Math.Min(int.MaxValue, experience);
        }
    }

    public readonly struct RuntimeHeroStats
    {
        public string HeroId { get; }
        public int Level { get; }
        public int Experience { get; }
        public int MaxHP { get; }
        public int MaxSoulHP { get; }
        public int Attack { get; }
        public int SoulAttack { get; }
        public int Defense { get; }
        public float CriticalChancePercent { get; }
        public float CriticalDamageMultiplier { get; }
        public float DodgePercent { get; }
        public float AccuracyPercent { get; }
        public int Speed { get; }
        public int ExperienceStars { get; }
        public int HealthStars { get; }
        public int ShieldStars { get; }
        public int MaxMana { get; }
        public int MaxRage { get; }

        public RuntimeHeroStats(HeroData config, HeroProgressSaveData progress,
            int equipmentAttackBonus = 0, int equipmentHealthBonus = 0,
            StarChartStatBonuses starBonuses = default,
            float equipmentAttackPercentBonus = 0f,
            float equipmentMaxHealthPercentBonus = 0f,
            float equipmentDefensePercentBonus = 0f,
            float equipmentCritRatePercentBonus = 0f,
            int equipmentSpeedBonus = 0,
            float equipmentCritDamagePercentBonus = 0f)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            HeroId = config.StableId;
            Level = progress != null
                ? Math.Max(1, Math.Min(HeroLevelProgression.MaximumLevel, progress.level))
                : 1;
            Experience = progress != null ? Math.Max(0, progress.experience) : 0;
            int levelsGained = Level - 1;
            long baseMaxHp = 500L + levelsGained * 100L
                + starBonuses.HealthStars * 50L + equipmentHealthBonus;
            MaxHP = ClampToPositiveInt((long)Math.Round(
                baseMaxHp * (1.0 + equipmentMaxHealthPercentBonus / 100.0),
                MidpointRounding.AwayFromZero));
            MaxSoulHP = SoulHealthCalculator.CalculateMaxHealth(MaxHP, Level);
            long baseAttack = 50L + levelsGained * 5L
                + starBonuses.AttackStars + equipmentAttackBonus;
            Attack = ClampToPositiveInt((long)Math.Round(
                baseAttack * (1.0 + equipmentAttackPercentBonus / 100.0),
                MidpointRounding.AwayFromZero));
            SoulAttack = ClampToPositiveInt(50L + levelsGained * 5L
                + starBonuses.SoulStars + equipmentAttackBonus);
            double baseDefense = 20.0 + levelsGained;
            Defense = ClampToPositiveInt((long)Math.Ceiling(
                baseDefense * (1.0 + 0.01 * starBonuses.RageStars)
                * (1.0 + equipmentDefensePercentBonus / 100.0) - 1e-9));
            CriticalChancePercent = 10f + levelsGained * .5f + equipmentCritRatePercentBonus;
            CriticalDamageMultiplier = 2f + starBonuses.RageStars * .01f
                + equipmentCritDamagePercentBonus / 100f;
            DodgePercent = levelsGained * .5f + starBonuses.ManaStars;
            AccuracyPercent = 100f + levelsGained * .5f + starBonuses.ManaStars * .5f;
            Speed = ClampToPositiveInt(10L + levelsGained + starBonuses.ManaStars
                + equipmentSpeedBonus);
            ExperienceStars = starBonuses.ExperienceStars;
            HealthStars = starBonuses.HealthStars;
            ShieldStars = starBonuses.ShieldStars;
            MaxMana = Math.Max(1, config.BaseMana);
            MaxRage = Math.Max(1, config.BaseRage);
        }

        static int ClampToPositiveInt(long value)
        {
            return value <= 1 ? 1 : value >= int.MaxValue ? int.MaxValue : (int)value;
        }
    }

    public static class SoulHealthCalculator
    {
        public static int CalculateMaxHealth(int maxHealth, int level)
        {
            long soulHealth = (long)Math.Max(0, maxHealth) / 5
                + 200L + (long)Math.Max(1, level) * 100;
            return soulHealth >= int.MaxValue ? int.MaxValue : (int)soulHealth;
        }

        public static int CalculateBossMaxHealth(int maxHealth, int level)
        {
            long soulHealth = (long)Math.Max(0, maxHealth) / 5
                + 1000L + (long)Math.Max(1, level) * 500;
            return soulHealth >= int.MaxValue ? int.MaxValue : (int)soulHealth;
        }
    }
}
