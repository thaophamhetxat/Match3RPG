using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3RPG.Campaign.Data
{
    [CreateAssetMenu(menuName = "Match3 RPG/Campaign/Campaign Catalog")]
    public sealed class CampaignCatalog : ScriptableObject
    {
        [SerializeField] Sprite worldMapSprite;
        [SerializeField] Sprite worldMapHeaderSprite;
        [SerializeField] Sprite worldMapFooterSprite;
        [SerializeField] GameObject worldMapMapLayoutPrefab;
        [SerializeField] RegionData[] regions = Array.Empty<RegionData>();
        [SerializeField] Sprite lockedStageMarker;
        [SerializeField] Sprite availableStageMarker;
        [SerializeField] Sprite completedStageMarker;
        [SerializeField] Sprite bossStageMarker;
        [SerializeField] Sprite campaignLockedStageMarker;
        [SerializeField] Sprite campaignAvailableStageMarker;
        [SerializeField] Sprite campaignCompletedStageMarker;
        [SerializeField] Sprite campaignBossStageMarker;
        [SerializeField] Sprite campaignStageGlow;
        [SerializeField] Sprite campaignStagePlatform;
        [SerializeField] Sprite campaignBackButton;
        [SerializeField] Sprite campaignRegionHeader;
        [SerializeField] Sprite campaignTooltipFrame;
        public IReadOnlyList<RegionData> Regions => regions ?? Array.Empty<RegionData>();
        public Sprite WorldMapSprite => worldMapSprite;
        public Sprite WorldMapHeaderSprite => worldMapHeaderSprite;
        public Sprite WorldMapFooterSprite => worldMapFooterSprite;
        public GameObject WorldMapMapLayoutPrefab => worldMapMapLayoutPrefab;
        public Sprite LockedStageMarker => lockedStageMarker;
        public Sprite AvailableStageMarker => availableStageMarker;
        public Sprite CompletedStageMarker => completedStageMarker;
        public Sprite BossStageMarker => bossStageMarker;
        public Sprite CampaignLockedStageMarker => campaignLockedStageMarker;
        public Sprite CampaignAvailableStageMarker => campaignAvailableStageMarker;
        public Sprite CampaignCompletedStageMarker => campaignCompletedStageMarker;
        public Sprite CampaignBossStageMarker => campaignBossStageMarker;
        public Sprite CampaignStageGlow => campaignStageGlow;
        public Sprite CampaignStagePlatform => campaignStagePlatform;
        public Sprite CampaignBackButton => campaignBackButton;
        public Sprite CampaignRegionHeader => campaignRegionHeader;
        public Sprite CampaignTooltipFrame => campaignTooltipFrame;

        public RegionData GetRegion(string regionId)
        {
            if (string.IsNullOrEmpty(regionId) || regions == null)
                return null;
            foreach (RegionData region in regions)
                if (region != null && string.Equals(region.RegionId, regionId, StringComparison.Ordinal))
                    return region;
            return null;
        }

        public StageData GetStage(string stageId, out RegionData region)
        {
            region = null;
            if (string.IsNullOrEmpty(stageId) || regions == null)
                return null;

            foreach (RegionData candidate in regions)
            {
                if (candidate == null || candidate.Stages == null)
                    continue;
                foreach (StageData stage in candidate.Stages)
                {
                    if (stage == null || !string.Equals(stage.StageId, stageId, StringComparison.Ordinal))
                        continue;
                    region = candidate;
                    return stage;
                }
            }
            return null;
        }

        public int GetRegionIndex(string regionId)
        {
            if (regions == null)
                return -1;
            for (int index = 0; index < regions.Length; index++)
                if (regions[index] != null && regions[index].RegionId == regionId)
                    return index;
            return -1;
        }

        public IEnumerable<StageData> EnumerateStages()
        {
            if (regions == null)
                yield break;
            foreach (RegionData region in regions)
            {
                if (region == null || region.Stages == null)
                    continue;
                foreach (StageData stage in region.Stages)
                    if (stage != null)
                        yield return stage;
            }
        }

        public void Configure(Sprite worldMap, RegionData[] orderedRegions, Sprite lockedMarker,
            Sprite availableMarker, Sprite completedMarker, Sprite bossMarker,
            Sprite campaignLockedMarker, Sprite campaignAvailableMarker,
            Sprite campaignCompletedMarker, Sprite campaignBossMarker, Sprite campaignGlow,
            Sprite campaignPlatform, Sprite campaignBack, Sprite campaignHeader,
            Sprite campaignTooltip)
        {
            worldMapSprite = worldMap;
            regions = orderedRegions ?? Array.Empty<RegionData>();
            lockedStageMarker = lockedMarker;
            availableStageMarker = availableMarker;
            completedStageMarker = completedMarker;
            bossStageMarker = bossMarker;
            campaignLockedStageMarker = campaignLockedMarker;
            campaignAvailableStageMarker = campaignAvailableMarker;
            campaignCompletedStageMarker = campaignCompletedMarker;
            campaignBossStageMarker = campaignBossMarker;
            campaignStageGlow = campaignGlow;
            campaignStagePlatform = campaignPlatform;
            campaignBackButton = campaignBack;
            campaignRegionHeader = campaignHeader;
            campaignTooltipFrame = campaignTooltip;
        }
    }
}
