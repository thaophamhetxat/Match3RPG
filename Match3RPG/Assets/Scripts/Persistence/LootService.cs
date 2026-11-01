using System;
using System.Collections.Generic;
using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Persistence
{
    public sealed class LootService
    {
        readonly Func<string, LootTableConfig> getLootTable;
        readonly Func<string, ItemConfig> getItem;
        readonly Func<float> rollValue;
        readonly Func<int, int, int> rollQuantity;

        public LootService(GameConfigService configService)
            : this(configService != null ? configService.GetLootTable : null,
                configService != null ? configService.GetItem : null,
                () => UnityEngine.Random.value,
                (minimum, maximum) => UnityEngine.Random.Range(minimum, checked(maximum + 1)))
        {
        }

        public LootService(Func<string, LootTableConfig> lootTableResolver,
            Func<string, ItemConfig> itemResolver,
            Func<float> randomValue = null,
            Func<int, int, int> quantityRandom = null)
        {
            getLootTable = lootTableResolver;
            getItem = itemResolver;
            rollValue = randomValue ?? (() => UnityEngine.Random.value);
            rollQuantity = quantityRandom
                ?? ((minimum, maximum) => UnityEngine.Random.Range(minimum, checked(maximum + 1)));
        }

        public RuntimeLootResult Roll(StageData stage, BossData boss, bool isFirstClear)
        {
            string tableId = stage != null && !string.IsNullOrEmpty(stage.LootTableId)
                ? stage.LootTableId
                : boss != null ? boss.DefaultLootTableId : string.Empty;
            if (string.IsNullOrEmpty(tableId))
                return new RuntimeLootResult(string.Empty, Array.Empty<RuntimeLootDrop>());
            if (!StableIdRules.IsValid(tableId))
            {
                Debug.LogWarning($"Invalid LootTableId '{tableId}'; no item loot was rolled.");
                return new RuntimeLootResult(tableId, Array.Empty<RuntimeLootDrop>());
            }
            if (getLootTable == null)
            {
                Debug.LogWarning($"LootTableDatabase is unavailable; skipping LootTable '{tableId}'.");
                return new RuntimeLootResult(tableId, Array.Empty<RuntimeLootDrop>());
            }

            LootTableConfig table = getLootTable(tableId);
            if (table == null)
            {
                Debug.LogWarning($"LootTableId '{tableId}' could not be resolved; no item loot was rolled.");
                return new RuntimeLootResult(tableId, Array.Empty<RuntimeLootDrop>());
            }

            List<RuntimeLootDrop> drops = new List<RuntimeLootDrop>();
            foreach (LootTableEntryConfig entry in table.Entries)
            {
                if (entry == null)
                {
                    Debug.LogWarning($"LootTable '{tableId}' contains an empty entry.", table);
                    continue;
                }
                if (entry.FirstClearOnly && !isFirstClear)
                    continue;
                if (!StableIdRules.IsValid(entry.ItemId))
                {
                    Debug.LogWarning($"LootTable '{tableId}' contains invalid ItemId "
                        + $"'{entry.ItemId}'; skipping entry.", table);
                    continue;
                }
                if (getItem == null || getItem(entry.ItemId) == null)
                {
                    Debug.LogWarning($"LootTable '{tableId}' references unknown ItemId "
                        + $"'{entry.ItemId}'; skipping entry.", table);
                    continue;
                }
                if (float.IsNaN(entry.DropChance) || float.IsInfinity(entry.DropChance)
                    || entry.DropChance < 0f || entry.DropChance > 1f)
                {
                    Debug.LogWarning($"LootTable '{tableId}' has invalid DropChance "
                        + $"for ItemId '{entry.ItemId}'; skipping entry.", table);
                    continue;
                }
                if (entry.MinimumQuantity <= 0
                    || entry.MaximumQuantity < entry.MinimumQuantity)
                {
                    Debug.LogWarning($"LootTable '{tableId}' has invalid quantity range "
                        + $"for ItemId '{entry.ItemId}'; skipping entry.", table);
                    continue;
                }

                float roll = rollValue();
                if (float.IsNaN(roll) || float.IsInfinity(roll) || roll < 0f || roll >= 1f)
                    throw new InvalidOperationException($"Loot random source returned out-of-range value {roll}.");
                if (roll >= entry.DropChance)
                    continue;

                int quantity = rollQuantity(entry.MinimumQuantity, entry.MaximumQuantity);
                if (quantity < entry.MinimumQuantity || quantity > entry.MaximumQuantity)
                    throw new InvalidOperationException(
                        $"Loot quantity source returned {quantity} outside "
                        + $"{entry.MinimumQuantity}..{entry.MaximumQuantity}.");
                drops.Add(new RuntimeLootDrop(entry.ItemId, quantity, entry.FirstClearOnly));
            }

            return new RuntimeLootResult(table.StableId, drops);
        }
    }
}
