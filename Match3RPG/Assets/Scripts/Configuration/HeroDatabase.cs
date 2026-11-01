using System;
using System.Collections.Generic;
using Match3RPG.Campaign.Data;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Databases/Hero Database")]
    public sealed class HeroDatabase : StableIdDatabase<HeroData>
    {
        [SerializeField] HeroData[] heroes = Array.Empty<HeroData>();

        public IReadOnlyList<HeroData> Heroes => heroes ?? Array.Empty<HeroData>();

        protected override IEnumerable<HeroData> EnumerateDefinitions() => Heroes;
        protected override string GetStableId(HeroData definition) => definition.StableId;
    }
}
