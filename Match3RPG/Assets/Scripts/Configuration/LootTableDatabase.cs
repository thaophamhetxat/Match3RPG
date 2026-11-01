using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Databases/Loot Table Database")]
    public sealed class LootTableDatabase : StableIdDatabase<LootTableConfig>
    {
        [SerializeField] LootTableConfig[] lootTables = Array.Empty<LootTableConfig>();

        public IReadOnlyList<LootTableConfig> LootTables =>
            lootTables ?? Array.Empty<LootTableConfig>();

        protected override IEnumerable<LootTableConfig> EnumerateDefinitions() => LootTables;
        protected override string GetStableId(LootTableConfig definition) => definition.StableId;
    }
}
