using System;
using System.Collections.Generic;

namespace Match3RPG.Persistence
{
    [Serializable]
    public sealed class GameSaveData
    {
        public const string DefaultPlayerName = "Mặc Tử Vân";

        public int saveVersion;
        public string playerName = DefaultPlayerName;
        public string selectedHeroId = SaveMigration.StartingHeroId;
        public int playerExperience;
        public int gold;
        public List<HeroProgressSaveData> heroes = new List<HeroProgressSaveData>();
        public List<PetProgressSaveData> pets = new List<PetProgressSaveData>();
        public List<CompanionProgressSaveData> companions = new List<CompanionProgressSaveData>();
        public List<SkillProgressSaveData> skills = new List<SkillProgressSaveData>();
        public InventorySaveData inventory = new InventorySaveData();
        public StarChartGemInventorySaveData starChartGemInventory =
            new StarChartGemInventorySaveData();
        public List<string> rewardedStageIds = new List<string>();
        public PlayerLoadoutSaveData loadout = new PlayerLoadoutSaveData();
        public CampaignSaveData campaign = new CampaignSaveData();
        public SettingsSaveData settings = new SettingsSaveData();
    }

    [Serializable]
    public sealed class HeroProgressSaveData
    {
        public string heroId;
        public int level = 1;
        public int experience;
    }

    [Serializable]
    public sealed class PetProgressSaveData
    {
        public string petId;
        public int level = 1;
        public int experience;
    }

    [Serializable]
    public sealed class CompanionProgressSaveData
    {
        public string companionId;
        public int level = 1;
        public int experience;
    }

    [Serializable]
    public sealed class SkillProgressSaveData
    {
        public string skillId;
        public int level = 1;
        public bool unlocked;
    }

    [Serializable]
    public sealed class InventorySaveData
    {
        public List<InventoryStackSaveData> stacks = new List<InventoryStackSaveData>();
        public List<ItemInstanceSaveData> itemInstances = new List<ItemInstanceSaveData>();
    }

    [Serializable]
    public sealed class StarChartGemInventorySaveData
    {
        public int[] baseGemCounts = new int[7];
        public int standardDiamonds;
        public int specialDiamonds;
        public List<StarChartRealmProgressSaveData> realmProgress =
            new List<StarChartRealmProgressSaveData>();
    }

    [Serializable]
    public sealed class StarChartRealmProgressSaveData
    {
        public int realmIndex;
        public int[] litStarsPerBranch = new int[7];
    }

    [Serializable]
    public sealed class InventoryStackSaveData
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public sealed class ItemInstanceSaveData
    {
        public string instanceId;
        public string itemId;
        public int level = 1;
        public int upgradeLevel;
        public bool locked;
        public int experience;
    }

    [Serializable]
    public sealed class EquipmentLoadoutSaveData
    {
        public string slotId;
        public string itemInstanceId;
    }

    [Serializable]
    public sealed class PlayerLoadoutSaveData
    {
        public string heroId = SaveMigration.StartingHeroId;
        public List<EquipmentLoadoutSaveData> equipment = new List<EquipmentLoadoutSaveData>();
        public List<string> petIds = new List<string>();
        public string companionId = string.Empty;
        public List<string> skillIds = new List<string>();

        public string SelectedHeroId
        {
            get => heroId;
            set => heroId = value;
        }

        public string EquippedPetId
        {
            get => petIds != null && petIds.Count > 0 ? petIds[0] : string.Empty;
            set
            {
                petIds = petIds ?? new List<string>();
                petIds.Clear();
                if (!string.IsNullOrEmpty(value))
                    petIds.Add(value);
            }
        }

        public string EquippedCompanionId
        {
            get => companionId;
            set => companionId = value ?? string.Empty;
        }

        public List<string> EquippedSkillIds
        {
            get => skillIds;
            set => skillIds = value ?? new List<string>();
        }
    }

    [Serializable]
    public sealed class CampaignSaveData
    {
        public int highestUnlockedRegion = 1;
        public string lastSelectedRegionId = "region_01";
        public List<CampaignRegionProgressSaveData> regions =
            new List<CampaignRegionProgressSaveData>();
    }

    [Serializable]
    public sealed class CampaignRegionProgressSaveData
    {
        public string regionId;
        public int highestUnlockedStage = 1;
        public List<string> completedStageIds = new List<string>();
    }

    [Serializable]
    public sealed class SettingsSaveData
    {
        public bool musicEnabled = true;
        public bool soundEnabled = true;
        public bool vibrationEnabled = true;
    }
}
