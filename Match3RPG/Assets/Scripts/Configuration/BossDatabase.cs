using System;
using System.Collections.Generic;
using Match3RPG.Campaign.Data;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Databases/Boss Database")]
    public sealed class BossDatabase : StableIdDatabase<BossData>
    {
        [SerializeField] BossData[] bosses = Array.Empty<BossData>();

        public IReadOnlyList<BossData> Bosses => bosses ?? Array.Empty<BossData>();

        protected override IEnumerable<BossData> EnumerateDefinitions() => Bosses;
        protected override string GetStableId(BossData definition) => definition.StableId;
    }
}
