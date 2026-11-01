using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Match3RPG.Campaign
{
    public sealed class CampaignManager : MonoBehaviour
    {
        public const string MainProgressSceneName = "MainProgress";
        public const string WorldMapSceneName = "WorldMap";
        public const string RegionMapSceneName = "RegionMap";
        public const string BattleSceneName = "Game";

        [SerializeField] CampaignCatalog catalog;
        GameConfigService gameConfigService;
        RewardPackage preparedVictoryRewards;
        string preparedRewardStageId;
        public static CampaignManager Instance { get; private set; }
        public string SelectedRegionId { get; private set; }
        public string SelectedStageId { get; private set; }
        public BossData SelectedBossData { get; private set; }
        public bool HasActiveCampaignBattle { get; private set; }
        public CampaignCatalog Catalog => catalog;
        public StageData CurrentStage => catalog != null && HasActiveCampaignBattle
            ? catalog.GetStage(SelectedStageId, out _)
            : GetDebugStage();

        public static CampaignManager GetOrCreate(CampaignCatalog campaignCatalog)
        {
            if (Instance != null)
            {
                Instance.SetCatalog(campaignCatalog);
                return Instance;
            }

            GameObject managerObject = new GameObject("CampaignManager");
            CampaignManager manager = managerObject.AddComponent<CampaignManager>();
            manager.SetCatalog(campaignCatalog);
            return manager;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameProgressService.EnsureInitialized(catalog);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetCatalog(CampaignCatalog campaignCatalog)
        {
            if (catalog == null)
                catalog = campaignCatalog;
            GameProgressService.EnsureInitialized(catalog);
        }

        StageData GetDebugStage()
        {
            RegionData debugRegion = catalog != null ? catalog.GetRegion("region_01") : null;
            return debugRegion != null ? debugRegion.GetStage(1) : null;
        }

        public bool SelectRegion(RegionData region)
        {
            if (region == null || catalog == null || !GameProgressService.IsRegionUnlocked(catalog, region))
                return false;
            SelectedRegionId = region.RegionId;
            GameProgressService.RememberRegion(region.RegionId);
            return true;
        }

        public bool BeginStage(RegionData region, StageData stage)
        {
            return BeginStage(region, stage, null);
        }

        public bool BeginStage(RegionData region, StageData stage, BossData stageBossData)
        {
            if (region == null || stage == null || stage.RegionId != region.RegionId
                || GameProgressService.GetStageState(region, stage) == CampaignNodeState.Locked
                || !SelectRegion(region))
                return false;

            SelectedBossData = ResolveBossData(stage);
            if (SelectedBossData == null)
            {
                GameConfigService config = GetGameConfigService();
                if (config == null || config.Stages == null || config.Bosses == null)
                    SelectedBossData = stageBossData;
            }
            if (SelectedBossData == null)
            {
                Debug.LogError($"Campaign stage has no resolvable BossData: {stage.StageId}", this);
                return false;
            }
            SelectedRegionId = region.RegionId;
            SelectedStageId = stage.StageId;
            HasActiveCampaignBattle = true;
            preparedVictoryRewards = null;
            preparedRewardStageId = null;
            SceneManager.LoadScene(BattleSceneName);
            return true;
        }

        public BossData ResolveBossData(StageData stage, Object context = null)
        {
            GameConfigService config = GetGameConfigService();
            if (stage != null && config != null && config.Stages != null && config.Bosses != null)
            {
                if (config.Stages.TryGetBoss(stage.StableId, config.Bosses,
                    out StageData configuredStage, out BossData configuredBoss)
                    && configuredStage == stage)
                    return configuredBoss;

                Debug.LogError($"Stage '{stage.StableId}' BossId '{stage.BossId}' could not be "
                    + "resolved by StageDatabase and BossDatabase.", context);
                return null;
            }

            if (stage != null && stage.BossData != null)
                return stage.BossData;

            string stageId = stage != null ? stage.StageId : "(unknown stage)";
            Debug.LogWarning($"Campaign stage '{stageId}' has no resolvable BossId in the Boss database; "
                + "using the development Boss configuration.", context);
            BossData fallback = config != null ? config.GetBoss("development_boss") : null;
            if (fallback == null)
                fallback = Resources.Load<BossData>("Campaign/Bosses/DevelopmentBoss");
            if (fallback == null)
                Debug.LogError("Development BossData is missing at Resources/Campaign/Bosses/DevelopmentBoss.", context);
            return fallback;
        }

        public BossData ResolveDevelopmentBossData(Object context = null)
        {
            GameConfigService config = GetGameConfigService();
            BossData fallback = config != null ? config.GetBoss("development_boss") : null;
            if (fallback == null)
                fallback = Resources.Load<BossData>("Campaign/Bosses/DevelopmentBoss");
            if (fallback == null)
                Debug.LogError("Development BossData is missing at Resources/Campaign/Bosses/DevelopmentBoss.", context);
            return fallback;
        }

        GameConfigService GetGameConfigService()
        {
            if (gameConfigService == null)
                gameConfigService = Resources.Load<GameConfigService>("Configuration/GameConfigService");
            return gameConfigService;
        }

        public void FinishBattle(bool victory)
        {
            if (!HasActiveCampaignBattle)
                return;

            if (victory)
                PrepareVictoryRewards();

            LeaveBattleForRegionMap();
        }

        public RewardPackage PrepareVictoryRewards()
        {
            if (!HasActiveCampaignBattle)
                return null;
            if (preparedVictoryRewards != null && preparedRewardStageId == SelectedStageId)
                return preparedVictoryRewards;

            RegionData region = null;
            StageData stage = catalog != null ? catalog.GetStage(SelectedStageId, out region) : null;
            if (stage == null || region == null)
            {
                Debug.LogError($"Campaign stage could not be resolved after battle: {SelectedStageId}", this);
                return null;
            }

            bool isFirstClear = !GameProgressService.IsStageCompleted(stage.StageId);
            GameConfigService config = GetGameConfigService();
            RuntimeLootResult loot = new LootService(config).Roll(stage, SelectedBossData, isFirstClear);
            RewardPackage rewards = new RewardPackage(stage.StableId,
                gold: 0, experience: 0, isFirstClear: isFirstClear, drops: loot.Drops);
            bool applied = new RewardService(SaveManager.Instance, config).Apply(rewards);
            GameProgressService.CompleteStage(catalog, region, stage);

            preparedRewardStageId = SelectedStageId;
            preparedVictoryRewards = applied
                ? rewards
                : new RewardPackage(stage.StableId, 0, 0, isFirstClear,
                    System.Array.Empty<RuntimeLootDrop>());
            return preparedVictoryRewards;
        }

        public void RetryCurrentBattle()
        {
            if (!HasActiveCampaignBattle || string.IsNullOrEmpty(SelectedStageId)
                || SelectedBossData == null)
            {
                Debug.LogError("Cannot retry because the active campaign stage or boss is unavailable.", this);
                return;
            }
            SceneManager.LoadScene(BattleSceneName);
        }

        public void LeaveBattleForRegionMap()
        {
            HasActiveCampaignBattle = false;
            SelectedBossData = null;
            preparedVictoryRewards = null;
            preparedRewardStageId = null;
            SceneManager.LoadScene(RegionMapSceneName);
        }

        public void LoadMainProgress()
        {
            SceneManager.LoadScene(MainProgressSceneName);
        }
    }
}
