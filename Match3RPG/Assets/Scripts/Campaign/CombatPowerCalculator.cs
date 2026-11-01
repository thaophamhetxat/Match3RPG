using Match3RPG.Battle;
using UnityEngine;

namespace Match3RPG.Campaign
{
    public static class CombatPowerCalculator
    {
        public static int Calculate(RuntimeHeroStats stats, StageChallengePopupLayoutConfig config)
        {
            return Calculate(stats.MaxHP, stats.Attack, stats.Level, config);
        }

        public static int Calculate(int maxHp, int attack, int level,
            StageChallengePopupLayoutConfig config)
        {
            if (config == null)
                return 0;

            float power = maxHp * config.CombatPowerHpWeight
                + attack * config.CombatPowerAttackWeight
                + level * config.CombatPowerLevelWeight;
            return Mathf.Max(0, Mathf.RoundToInt(power));
        }
    }
}
