using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [Serializable]
    public sealed class RegionBossRosterEntryConfig
    {
        [SerializeField] string regionId;
        [SerializeField] string bossId;
        [SerializeField] int order;
        [SerializeField] bool enabled = true;

        public string RegionId => regionId;
        public string BossId => bossId;
        public int Order => order;
        public bool Enabled => enabled;

        public RegionBossRosterEntryConfig(string owningRegionId, string stableBossId,
            int rosterOrder, bool isEnabled)
        {
            regionId = owningRegionId;
            bossId = stableBossId;
            order = rosterOrder;
            enabled = isEnabled;
        }
    }

    [CreateAssetMenu(menuName = "Match3 RPG/Databases/Region Boss Roster Database")]
    public sealed class RegionBossRosterDatabase : ScriptableObject
    {
        [SerializeField] BossDatabase bossDatabase;
        [SerializeField] RegionBossRosterEntryConfig[] entries =
            Array.Empty<RegionBossRosterEntryConfig>();

        readonly Dictionary<string, List<RegionBossRosterEntryConfig>> entriesByRegion =
            new Dictionary<string, List<RegionBossRosterEntryConfig>>(StringComparer.Ordinal);
        bool isInitialized;

        public IReadOnlyList<RegionBossRosterEntryConfig> Entries =>
            entries ?? Array.Empty<RegionBossRosterEntryConfig>();
        public BossDatabase BossDatabase => bossDatabase;

        void OnEnable() => RebuildCache();
        void OnValidate() => RebuildCache();

        public IReadOnlyList<RegionBossRosterEntryConfig> GetByRegionId(string regionId)
        {
            EnsureInitialized();
            return !string.IsNullOrEmpty(regionId) && entriesByRegion.TryGetValue(regionId, out var roster)
                ? roster
                : Array.Empty<RegionBossRosterEntryConfig>();
        }

        public IReadOnlyList<RegionBossRosterEntryConfig> GetEnabledByRegionId(string regionId)
        {
            IReadOnlyList<RegionBossRosterEntryConfig> roster = GetByRegionId(regionId);
            List<RegionBossRosterEntryConfig> enabledEntries =
                new List<RegionBossRosterEntryConfig>(roster.Count);
            foreach (RegionBossRosterEntryConfig entry in roster)
                if (entry.Enabled)
                    enabledEntries.Add(entry);
            return enabledEntries;
        }

        void RebuildCache()
        {
            entriesByRegion.Clear();
            foreach (RegionBossRosterEntryConfig entry in Entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.RegionId))
                    continue;
                if (!entriesByRegion.TryGetValue(entry.RegionId, out List<RegionBossRosterEntryConfig> roster))
                {
                    roster = new List<RegionBossRosterEntryConfig>();
                    entriesByRegion.Add(entry.RegionId, roster);
                }
                roster.Add(entry);
            }

            foreach (List<RegionBossRosterEntryConfig> roster in entriesByRegion.Values)
                roster.Sort((left, right) => left.Order.CompareTo(right.Order));
            isInitialized = true;
        }

        void EnsureInitialized()
        {
            if (!isInitialized)
                RebuildCache();
        }
    }
}
