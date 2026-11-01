using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Databases/Item Database")]
    public sealed class ItemDatabase : StableIdDatabase<ItemConfig>
    {
        [SerializeField] ItemConfig[] items = Array.Empty<ItemConfig>();

        public IReadOnlyList<ItemConfig> Items => items ?? Array.Empty<ItemConfig>();

        protected override IEnumerable<ItemConfig> EnumerateDefinitions() => Items;
        protected override string GetStableId(ItemConfig definition) => definition.StableId;
    }
}
