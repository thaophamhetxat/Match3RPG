using Match3RPG.Campaign.Data;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Configuration/Game Config Service")]
    public sealed class GameConfigService : ScriptableObject
    {
        [SerializeField] HeroDatabase heroDatabase;
        [SerializeField] BossDatabase bossDatabase;
        [SerializeField] RegionDatabase regionDatabase;
        [SerializeField] RegionBossRosterDatabase regionBossRosterDatabase;
        [SerializeField] StageDatabase stageDatabase;
        [SerializeField] ItemDatabase itemDatabase;
        [SerializeField] SkillDatabase skillDatabase;
        [SerializeField] PetDatabase petDatabase;
        [SerializeField] CompanionDatabase companionDatabase;
        [SerializeField] LootTableDatabase lootTableDatabase;
        [SerializeField] EquipmentSetDatabase equipmentSetDatabase;
        [SerializeField] LoadoutConfig loadoutConfig;

        public HeroDatabase Heroes => heroDatabase;
        public BossDatabase Bosses => bossDatabase;
        public RegionDatabase Regions => regionDatabase;
        public RegionBossRosterDatabase RegionBossRoster => regionBossRosterDatabase;
        public StageDatabase Stages => stageDatabase;
        public ItemDatabase Items => itemDatabase;
        public SkillDatabase Skills => skillDatabase;
        public PetDatabase Pets => petDatabase;
        public CompanionDatabase Companions => companionDatabase;
        public LootTableDatabase LootTables => lootTableDatabase;
        public EquipmentSetDatabase EquipmentSets => equipmentSetDatabase;
        public LoadoutConfig Loadout => loadoutConfig;

        public HeroData GetHero(string stableId) => heroDatabase != null ? heroDatabase.GetById(stableId) : null;
        public BossData GetBoss(string stableId) => bossDatabase != null ? bossDatabase.GetById(stableId) : null;
        public RegionData GetRegion(string stableId) => regionDatabase != null ? regionDatabase.GetById(stableId) : null;
        public StageData GetStage(string stableId) => stageDatabase != null ? stageDatabase.GetById(stableId) : null;
        public ItemConfig GetItem(string stableId) => itemDatabase != null ? itemDatabase.GetById(stableId) : null;
        public SkillConfig GetSkill(string stableId) => skillDatabase != null ? skillDatabase.GetById(stableId) : null;
        public PetConfig GetPet(string stableId) => petDatabase != null ? petDatabase.GetById(stableId) : null;
        public CompanionConfig GetCompanion(string stableId) =>
            companionDatabase != null ? companionDatabase.GetById(stableId) : null;
        public LootTableConfig GetLootTable(string stableId) =>
            lootTableDatabase != null ? lootTableDatabase.GetById(stableId) : null;
        public EquipmentSetConfig GetEquipmentSet(string stableId) =>
            equipmentSetDatabase != null ? equipmentSetDatabase.GetById(stableId) : null;
    }
}
