using System;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [Serializable]
    public sealed class LootTableEntryConfig
    {
        [SerializeField] string itemId;
        [SerializeField, Min(0)] int minimumQuantity;
        [SerializeField, Min(0)] int maximumQuantity;
        [SerializeField, Range(0f, 1f)] float dropChance;
        [SerializeField] bool firstClearOnly;

        public string ItemId => itemId;
        public int MinimumQuantity => minimumQuantity;
        public int MaximumQuantity => maximumQuantity;
        public float DropChance => dropChance;
        public bool FirstClearOnly => firstClearOnly;

        public void Configure(string stableItemId, int minimum, int maximum,
            float chance, bool onlyOnFirstClear = false)
        {
            itemId = stableItemId;
            minimumQuantity = minimum;
            maximumQuantity = maximum;
            dropChance = chance;
            firstClearOnly = onlyOnFirstClear;
        }
    }

    [CreateAssetMenu(menuName = "Match3 RPG/Definitions/Loot Table")]
    public sealed class LootTableConfig : StableIdConfig
    {
        [SerializeField] LootTableEntryConfig[] entries = Array.Empty<LootTableEntryConfig>();

        public LootTableEntryConfig[] Entries => entries ?? Array.Empty<LootTableEntryConfig>();

        public void Configure(string stableId, params LootTableEntryConfig[] tableEntries)
        {
            SetStableId(stableId);
            entries = tableEntries ?? Array.Empty<LootTableEntryConfig>();
        }
    }
}
