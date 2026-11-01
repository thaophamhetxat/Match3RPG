using System;
using System.Collections.Generic;
using Match3RPG.Campaign.Data;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Databases/Region Database")]
    public sealed class RegionDatabase : StableIdDatabase<RegionData>
    {
        [SerializeField] CampaignCatalog campaignCatalog;

        public CampaignCatalog CampaignCatalog => campaignCatalog;
        public IReadOnlyList<RegionData> Regions =>
            campaignCatalog != null ? campaignCatalog.Regions : Array.Empty<RegionData>();

        protected override IEnumerable<RegionData> EnumerateDefinitions() => Regions;
        protected override string GetStableId(RegionData definition) => definition.StableId;
    }
}
