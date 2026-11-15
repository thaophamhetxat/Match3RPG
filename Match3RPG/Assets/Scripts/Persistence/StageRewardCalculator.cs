using UnityEngine;

namespace Match3RPG.Persistence
{
    /// <summary>
    /// Single source of truth for the stage clear reward formula. Used both by the
    /// stage challenge popup preview and by the real victory reward grant.
    /// </summary>
    public static class StageRewardCalculator
    {
        public const string SpiritStoneItemId = "linh_thach_ha_pham";
        public const int FirstClearSpiritStoneQuantity = 200;
        public const int RepeatSpiritStoneQuantity = 100;
        public const int FirstClearExperience = 100;
        public const int RepeatExperience = 50;
        public const float StageScalePerStage = 0.1f;

        public static void GetStageRewards(int stageNumber, bool isFirstClear,
            out int spiritStoneQuantity, out int experience)
        {
            int number = stageNumber < 1 ? 1 : stageNumber;
            float multiplier = 1f + StageScalePerStage * (number - 1);
            spiritStoneQuantity = Mathf.RoundToInt((isFirstClear
                ? FirstClearSpiritStoneQuantity : RepeatSpiritStoneQuantity) * multiplier);
            experience = Mathf.RoundToInt((isFirstClear
                ? FirstClearExperience : RepeatExperience) * multiplier);
        }
    }
}
