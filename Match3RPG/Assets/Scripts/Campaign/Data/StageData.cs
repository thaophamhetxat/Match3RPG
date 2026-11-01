using System;
using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Campaign.Data
{
    [Serializable]
    public sealed class StageData
    {
        [SerializeField] string stageId;
        [SerializeField, Min(1)] int stageNumber;
        [SerializeField] string displayName;
        [SerializeField] string regionId;
        [SerializeField] bool isBossStage;
        [SerializeField] Vector2 regionMapNodePosition;
        [SerializeField] BossData bossData;
        [SerializeField] string lootTableId;

        public string StageId => stageId;
        public string StableId => stageId;
        public bool HasValidStableId => StableIdRules.IsValid(stageId);
        public int StageNumber => stageNumber;
        public string DisplayName => displayName;
        public string RegionId => regionId;
        public bool IsBossStage => isBossStage;
        public Vector2 RegionMapNodePosition => regionMapNodePosition;
        public BossData BossData => bossData;
        public string BossId => bossData != null ? bossData.StableId : string.Empty;
        public string LootTableId => lootTableId;

        public void Configure(string id, int number, string title, string owningRegionId,
            bool bossStage, Vector2 nodePosition, BossData boss = null)
        {
            stageId = id;
            stageNumber = number;
            displayName = title;
            regionId = owningRegionId;
            isBossStage = bossStage;
            regionMapNodePosition = nodePosition;
            bossData = boss;
        }

        public void SetLootTableId(string stableId)
        {
            lootTableId = stableId;
        }
    }
}
