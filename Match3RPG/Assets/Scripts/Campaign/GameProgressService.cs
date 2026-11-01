using Match3RPG.Campaign.Data;
using UnityEngine;

namespace Match3RPG.Campaign
{
    public enum CampaignNodeState
    {
        Locked,
        Available,
        Completed
    }

    public static class GameProgressService
    {
        const string KeyPrefix = "match3rpg.campaign.v1.";
        const string InitializedKey = KeyPrefix + "initialized";
        const string HighestRegionKey = KeyPrefix + "highest_region";
        const string LastRegionKey = KeyPrefix + "last_region";

        public static void EnsureInitialized(CampaignCatalog catalog)
        {
            bool changed = false;
            if (PlayerPrefs.GetInt(InitializedKey, 0) == 0)
            {
                PlayerPrefs.SetInt(HighestRegionKey, 1);
                PlayerPrefs.SetString(LastRegionKey, GetFirstRegionId(catalog));
                PlayerPrefs.SetInt(InitializedKey, 1);
                changed = true;
            }

            if (catalog != null)
            {
                foreach (RegionData region in catalog.Regions)
                {
                    if (region == null || string.IsNullOrEmpty(region.RegionId))
                        continue;
                    string highestStageKey = HighestStageKey(region.RegionId);
                    if (!PlayerPrefs.HasKey(highestStageKey))
                    {
                        PlayerPrefs.SetInt(highestStageKey, 1);
                        changed = true;
                    }
                    foreach (StageData stage in region.Stages)
                        if (stage != null && !PlayerPrefs.HasKey(CompletedStageKey(stage.StageId)))
                        {
                            PlayerPrefs.SetInt(CompletedStageKey(stage.StageId), 0);
                            changed = true;
                        }
                }
            }

            if (changed)
                PlayerPrefs.Save();
        }

        public static int HighestUnlockedRegion => Mathf.Clamp(PlayerPrefs.GetInt(HighestRegionKey, 1), 1, 5);

        public static int HighestUnlockedStage(string regionId)
        {
            if (string.IsNullOrEmpty(regionId))
                return 1;
            return Mathf.Clamp(PlayerPrefs.GetInt(HighestStageKey(regionId), 1), 1, 10);
        }

        public static bool IsStageCompleted(string stageId)
        {
            return !string.IsNullOrEmpty(stageId) && PlayerPrefs.GetInt(CompletedStageKey(stageId), 0) != 0;
        }

        public static string LastSelectedRegionId => PlayerPrefs.GetString(LastRegionKey, "region_01");

        public static CampaignNodeState GetRegionState(CampaignCatalog catalog, RegionData region)
        {
            if (catalog == null || region == null)
                return CampaignNodeState.Locked;
            int regionIndex = catalog.GetRegionIndex(region.RegionId);
            if (regionIndex < 0 || regionIndex + 1 > HighestUnlockedRegion)
                return CampaignNodeState.Locked;
            StageData finalStage = region.GetStage(10);
            return finalStage != null && IsStageCompleted(finalStage.StageId)
                ? CampaignNodeState.Completed
                : CampaignNodeState.Available;
        }

        public static CampaignNodeState GetStageState(RegionData region, StageData stage)
        {
            if (region == null || stage == null)
                return CampaignNodeState.Locked;
            if (IsStageCompleted(stage.StageId))
                return CampaignNodeState.Completed;
            if (stage.StageNumber <= HighestUnlockedStage(region.RegionId))
                return CampaignNodeState.Available;
            return CampaignNodeState.Locked;
        }

        public static bool IsRegionUnlocked(CampaignCatalog catalog, RegionData region)
        {
            return GetRegionState(catalog, region) != CampaignNodeState.Locked;
        }

        public static void RememberRegion(string regionId)
        {
            if (string.IsNullOrEmpty(regionId))
                return;
            PlayerPrefs.SetString(LastRegionKey, regionId);
            PlayerPrefs.Save();
        }

        public static void CompleteStage(CampaignCatalog catalog, RegionData region, StageData stage)
        {
            if (catalog == null || region == null || stage == null)
                return;

            string completedKey = CompletedStageKey(stage.StageId);
            PlayerPrefs.SetInt(completedKey, 1);
            int nextStage = Mathf.Clamp(stage.StageNumber + 1, 1, 10);
            int highestStage = Mathf.Max(HighestUnlockedStage(region.RegionId), nextStage);
            PlayerPrefs.SetInt(HighestStageKey(region.RegionId), highestStage);

            if (stage.StageNumber == 10)
            {
                int nextRegionIndex = catalog.GetRegionIndex(region.NextRegionId);
                if (nextRegionIndex > 0 && nextRegionIndex < catalog.Regions.Count)
                    PlayerPrefs.SetInt(HighestRegionKey,
                        Mathf.Max(HighestUnlockedRegion, nextRegionIndex + 1));
            }

            RememberRegion(region.RegionId);
            PlayerPrefs.Save();
        }

        public static void Reset(CampaignCatalog catalog)
        {
            PlayerPrefs.DeleteKey(HighestRegionKey);
            PlayerPrefs.DeleteKey(LastRegionKey);
            if (catalog != null)
            {
                foreach (RegionData region in catalog.Regions)
                {
                    if (region == null)
                        continue;
                    PlayerPrefs.DeleteKey(HighestStageKey(region.RegionId));
                    foreach (StageData stage in region.Stages)
                        if (stage != null)
                            PlayerPrefs.DeleteKey(CompletedStageKey(stage.StageId));
                }
            }
            PlayerPrefs.DeleteKey(InitializedKey);
            EnsureInitialized(catalog);
        }

        static string HighestStageKey(string regionId) => KeyPrefix + "highest_stage." + regionId;
        static string CompletedStageKey(string stageId) => KeyPrefix + "completed_stage." + stageId;

        static string GetFirstRegionId(CampaignCatalog catalog)
        {
            if (catalog != null)
                foreach (RegionData region in catalog.Regions)
                    if (region != null && !string.IsNullOrEmpty(region.RegionId))
                        return region.RegionId;
            return "region_01";
        }
    }
}
