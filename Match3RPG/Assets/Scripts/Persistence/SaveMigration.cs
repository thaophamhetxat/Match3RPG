using System;
using System.Collections.Generic;
using Match3RPG.Campaign.Data;
using UnityEngine;

namespace Match3RPG.Persistence
{
    public static class SaveMigration
    {
        public const int CurrentSaveVersion = 4;
        public const string StartingHeroId = "hero_02";

        const string LegacyCampaignPrefix = "match3rpg.campaign.v1.";
        const string LegacyInitializedKey = LegacyCampaignPrefix + "initialized";
        const string LegacyHighestRegionKey = LegacyCampaignPrefix + "highest_region";
        const string LegacyLastRegionKey = LegacyCampaignPrefix + "last_region";

        public static GameSaveData CreateNewGame(CampaignCatalog catalog = null)
        {
            if (catalog == null)
                catalog = Resources.Load<CampaignCatalog>("Campaign/CampaignCatalog");

            GameSaveData data = new GameSaveData
            {
                saveVersion = CurrentSaveVersion,
                selectedHeroId = StartingHeroId,
                loadout = new PlayerLoadoutSaveData { heroId = StartingHeroId },
                campaign = CreateDefaultCampaign(catalog)
            };
            data.heroes.Add(new HeroProgressSaveData { heroId = StartingHeroId, level = 1 });
            CaptureLegacyCampaignProgress(data.campaign, catalog);
            Normalize(data);
            return data;
        }

        public static GameSaveData Migrate(GameSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.saveVersion < 0)
                throw new InvalidOperationException($"Save version cannot be negative: {data.saveVersion}.");
            if (data.saveVersion > CurrentSaveVersion)
                throw new NotSupportedException(
                    $"Save version {data.saveVersion} is newer than supported version {CurrentSaveVersion}.");

            while (data.saveVersion < CurrentSaveVersion)
            {
                switch (data.saveVersion)
                {
                    case 0:
                        MigrateVersionZero(data);
                        break;
                    case 1:
                        MigrateVersionOne(data);
                        break;
                    case 2:
                        MigrateVersionTwo(data);
                        break;
                    case 3:
                        MigrateVersionThree(data);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"No migration is defined for save version {data.saveVersion}.");
                }
            }

            Normalize(data);
            return data;
        }

        static void MigrateVersionZero(GameSaveData data)
        {
            Normalize(data);
            data.saveVersion = 1;
        }

        static void MigrateVersionOne(GameSaveData data)
        {
            Normalize(data);
            data.saveVersion = 2;
        }

        static void MigrateVersionTwo(GameSaveData data)
        {
            Normalize(data);
            data.saveVersion = 3;
        }

        static void MigrateVersionThree(GameSaveData data)
        {
            Normalize(data);
            data.saveVersion = 4;
        }

        static CampaignSaveData CreateDefaultCampaign(CampaignCatalog catalog)
        {
            CampaignSaveData campaign = new CampaignSaveData();
            if (catalog == null)
                return campaign;

            bool hasFirstRegion = false;
            foreach (RegionData region in catalog.Regions)
            {
                if (region == null || string.IsNullOrEmpty(region.RegionId))
                    continue;

                campaign.regions.Add(new CampaignRegionProgressSaveData
                {
                    regionId = region.RegionId,
                    highestUnlockedStage = 1
                });
                if (!hasFirstRegion)
                {
                    campaign.lastSelectedRegionId = region.RegionId;
                    hasFirstRegion = true;
                }
            }
            return campaign;
        }

        static void CaptureLegacyCampaignProgress(CampaignSaveData campaign, CampaignCatalog catalog)
        {
            bool hasLegacyProgress = PlayerPrefs.HasKey(LegacyInitializedKey)
                || PlayerPrefs.HasKey(LegacyHighestRegionKey)
                || PlayerPrefs.HasKey(LegacyLastRegionKey);
            if (catalog != null)
            {
                foreach (RegionData region in catalog.Regions)
                {
                    if (region == null || string.IsNullOrEmpty(region.RegionId))
                        continue;

                    hasLegacyProgress |= PlayerPrefs.HasKey(LegacyHighestStageKey(region.RegionId));
                    if (region.Stages != null)
                        foreach (StageData stage in region.Stages)
                            if (stage != null && !string.IsNullOrEmpty(stage.StageId))
                                hasLegacyProgress |= PlayerPrefs.HasKey(
                                    LegacyCompletedStageKey(stage.StageId));
                }
            }

            if (!hasLegacyProgress)
                return;

            campaign.highestUnlockedRegion = Mathf.Max(1,
                PlayerPrefs.GetInt(LegacyHighestRegionKey, campaign.highestUnlockedRegion));
            string lastRegionId = PlayerPrefs.GetString(
                LegacyLastRegionKey, campaign.lastSelectedRegionId);
            if (!string.IsNullOrEmpty(lastRegionId))
                campaign.lastSelectedRegionId = lastRegionId;

            if (catalog == null)
                return;

            foreach (RegionData region in catalog.Regions)
            {
                if (region == null || string.IsNullOrEmpty(region.RegionId))
                    continue;

                CampaignRegionProgressSaveData regionProgress = FindRegionProgress(campaign, region.RegionId);
                if (regionProgress == null)
                {
                    regionProgress = new CampaignRegionProgressSaveData { regionId = region.RegionId };
                    campaign.regions.Add(regionProgress);
                }

                regionProgress.highestUnlockedStage = Mathf.Max(1,
                    PlayerPrefs.GetInt(LegacyHighestStageKey(region.RegionId), 1));
                if (region.Stages != null)
                    foreach (StageData stage in region.Stages)
                    {
                        if (stage != null
                            && !string.IsNullOrEmpty(stage.StageId)
                            && PlayerPrefs.GetInt(LegacyCompletedStageKey(stage.StageId), 0) != 0
                            && !regionProgress.completedStageIds.Contains(stage.StageId))
                            regionProgress.completedStageIds.Add(stage.StageId);
                    }
            }
        }

        static CampaignRegionProgressSaveData FindRegionProgress(
            CampaignSaveData campaign, string regionId)
        {
            foreach (CampaignRegionProgressSaveData region in campaign.regions)
                if (region != null && string.Equals(region.regionId, regionId, StringComparison.Ordinal))
                    return region;
            return null;
        }

        static void Normalize(GameSaveData data)
        {
            data.heroes = data.heroes ?? new List<HeroProgressSaveData>();
            data.pets = data.pets ?? new List<PetProgressSaveData>();
            data.companions = data.companions ?? new List<CompanionProgressSaveData>();
            data.skills = data.skills ?? new List<SkillProgressSaveData>();
            data.inventory = data.inventory ?? new InventorySaveData();
            data.starChartGemInventory = data.starChartGemInventory
                ?? new StarChartGemInventorySaveData();
            data.rewardedStageIds = data.rewardedStageIds ?? new List<string>();
            data.inventory.stacks = data.inventory.stacks ?? new List<InventoryStackSaveData>();
            data.inventory.itemInstances = data.inventory.itemInstances ?? new List<ItemInstanceSaveData>();
            data.loadout = data.loadout ?? new PlayerLoadoutSaveData();
            data.loadout.equipment = data.loadout.equipment ?? new List<EquipmentLoadoutSaveData>();
            data.loadout.petIds = data.loadout.petIds ?? new List<string>();
            data.loadout.skillIds = data.loadout.skillIds ?? new List<string>();
            data.campaign = data.campaign ?? new CampaignSaveData();
            data.campaign.regions = data.campaign.regions ?? new List<CampaignRegionProgressSaveData>();
            data.settings = data.settings ?? new SettingsSaveData();
            int[] savedBaseGemCounts = data.starChartGemInventory.baseGemCounts;
            int[] normalizedBaseGemCounts = new int[7];
            if (savedBaseGemCounts != null)
                for (int index = 0; index < normalizedBaseGemCounts.Length
                    && index < savedBaseGemCounts.Length; index++)
                    normalizedBaseGemCounts[index] = Math.Max(0, savedBaseGemCounts[index]);
            data.starChartGemInventory.baseGemCounts = normalizedBaseGemCounts;
            data.starChartGemInventory.standardDiamonds =
                Math.Max(0, data.starChartGemInventory.standardDiamonds);
            data.starChartGemInventory.specialDiamonds =
                Math.Max(0, data.starChartGemInventory.specialDiamonds);
            data.starChartGemInventory.realmProgress =
                data.starChartGemInventory.realmProgress
                ?? new List<StarChartRealmProgressSaveData>();
            List<StarChartRealmProgressSaveData> normalizedRealmProgress =
                new List<StarChartRealmProgressSaveData>(14);
            bool[] seenRealms = new bool[14];
            foreach (StarChartRealmProgressSaveData realmProgress
                in data.starChartGemInventory.realmProgress)
            {
                if (realmProgress == null || realmProgress.realmIndex < 0
                    || realmProgress.realmIndex >= seenRealms.Length
                    || seenRealms[realmProgress.realmIndex])
                    continue;

                seenRealms[realmProgress.realmIndex] = true;
                int[] savedStars = realmProgress.litStarsPerBranch;
                int[] normalizedStars = new int[7];
                if (savedStars != null)
                    for (int index = 0; index < normalizedStars.Length
                        && index < savedStars.Length; index++)
                        normalizedStars[index] = Math.Max(0,
                            Math.Min(10, savedStars[index]));
                normalizedRealmProgress.Add(new StarChartRealmProgressSaveData
                {
                    realmIndex = realmProgress.realmIndex,
                    litStarsPerBranch = normalizedStars
                });
            }
            data.starChartGemInventory.realmProgress = normalizedRealmProgress;

            if (string.IsNullOrEmpty(data.selectedHeroId))
                data.selectedHeroId = StartingHeroId;
            if (string.IsNullOrEmpty(data.loadout.heroId))
                data.loadout.heroId = data.selectedHeroId;
            if (data.heroes.Count == 0)
                data.heroes.Add(new HeroProgressSaveData { heroId = data.selectedHeroId, level = 1 });
            if (string.IsNullOrEmpty(data.campaign.lastSelectedRegionId))
                data.campaign.lastSelectedRegionId = "region_01";

            foreach (CampaignRegionProgressSaveData region in data.campaign.regions)
                if (region != null)
                    region.completedStageIds = region.completedStageIds ?? new List<string>();
        }

        static string LegacyHighestStageKey(string regionId) =>
            LegacyCampaignPrefix + "highest_stage." + regionId;

        static string LegacyCompletedStageKey(string stageId) =>
            LegacyCampaignPrefix + "completed_stage." + stageId;
    }
}
