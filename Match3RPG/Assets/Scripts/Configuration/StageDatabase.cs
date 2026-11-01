using System;
using System.Collections.Generic;
using Match3RPG.Campaign.Data;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Databases/Stage Database")]
    public sealed class StageDatabase : ScriptableObject
    {
        [SerializeField] RegionDatabase regionDatabase;

        readonly Dictionary<string, StageData> stagesById =
            new Dictionary<string, StageData>(StringComparer.Ordinal);
        bool isInitialized;

        public RegionDatabase RegionDatabase => regionDatabase;
        public int Count
        {
            get
            {
                EnsureInitialized();
                return stagesById.Count;
            }
        }

        void OnEnable() => RebuildCache();
        void OnValidate() => RebuildCache();

        public StageData GetById(string stableId)
        {
            return TryGetById(stableId, out StageData stage) ? stage : null;
        }

        public bool TryGetBoss(string stageId, BossDatabase bossDatabase,
            out StageData stage, out BossData boss)
        {
            stage = GetById(stageId);
            boss = stage != null && bossDatabase != null
                ? bossDatabase.GetById(stage.BossId)
                : null;
            return stage != null && boss != null;
        }

        public bool TryGetById(string stableId, out StageData stage)
        {
            EnsureInitialized();
            if (!string.IsNullOrEmpty(stableId))
                return stagesById.TryGetValue(stableId, out stage);

            stage = null;
            return false;
        }

        void RebuildCache()
        {
            stagesById.Clear();
            if (regionDatabase != null)
            {
                foreach (RegionData region in regionDatabase.Regions)
                {
                    if (region == null || region.Stages == null)
                        continue;
                    foreach (StageData stage in region.Stages)
                    {
                        if (stage == null)
                            continue;
                        if (!Match3RPG.Core.StableIdRules.IsValid(stage.StableId))
                        {
                            Debug.LogError($"Invalid or missing stage ID in {region.name}.", this);
                            continue;
                        }
                        if (!stagesById.TryAdd(stage.StableId, stage))
                            Debug.LogError($"Duplicate stage ID '{stage.StableId}' in {name}.", this);
                    }
                }
            }
            isInitialized = true;
        }

        void EnsureInitialized()
        {
            if (!isInitialized)
                RebuildCache();
        }
    }
}
