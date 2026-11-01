using System;
using System.Collections.Generic;
using Match3RPG.Configuration;
using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Persistence
{
    public sealed class RewardService
    {
        readonly SaveManager saveManager;
        readonly Func<string, ItemConfig> getItem;

        public RewardService(SaveManager saveManager, GameConfigService configService)
            : this(saveManager, configService != null
                ? new Func<string, ItemConfig>(configService.GetItem)
                : null)
        {
        }

        public RewardService(SaveManager saveManager, Func<string, ItemConfig> itemResolver)
        {
            this.saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            getItem = itemResolver;
            EnsureSaveLoaded();
        }

        public bool Apply(RewardPackage package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            if (!StableIdRules.IsValid(package.StageId))
                throw new ArgumentException($"Invalid reward StageId '{package.StageId}'.", nameof(package));
            if (package.Gold < 0 || package.Experience < 0)
                throw new ArgumentOutOfRangeException(nameof(package),
                    "Gold and Experience rewards cannot be negative.");

            GameSaveData save = saveManager.CurrentSave;
            if (save.rewardedStageIds.Contains(package.StageId))
                return false;

            int updatedGold = checked(save.gold + package.Gold);
            int updatedExperience = checked(save.playerExperience + package.Experience);
            Dictionary<string, int> stackAdds = new Dictionary<string, int>(StringComparer.Ordinal);
            List<string> equipmentItemIds = new List<string>();

            foreach (RuntimeLootDrop drop in package.ItemDrops)
            {
                if (drop == null || string.IsNullOrEmpty(drop.ItemId))
                {
                    Debug.LogWarning($"Skipping empty item reward for Stage '{package.StageId}'.");
                    continue;
                }
                if (drop.FirstClearOnly && !package.IsFirstClear)
                    continue;
                if (drop.Quantity <= 0)
                {
                    Debug.LogWarning($"Skipping non-positive quantity reward for ItemId '{drop.ItemId}'.");
                    continue;
                }
                if (!StableIdRules.IsValid(drop.ItemId))
                {
                    Debug.LogWarning($"Skipping reward with invalid ItemId '{drop.ItemId}'.");
                    continue;
                }
                if (getItem == null)
                {
                    Debug.LogWarning($"Skipping ItemId '{drop.ItemId}': ItemDatabase is unavailable.");
                    continue;
                }

                ItemConfig item = getItem(drop.ItemId);
                if (item == null)
                {
                    Debug.LogWarning($"Skipping unknown ItemId '{drop.ItemId}' in Stage rewards.");
                    continue;
                }
                if (item.Type == ItemType.Equipment)
                {
                    if (!InventoryService.IsEquippable(item))
                    {
                        Debug.LogWarning($"Skipping invalid equipment reward ItemId '{drop.ItemId}'.");
                        continue;
                    }
                    for (int i = 0; i < drop.Quantity; i++)
                        equipmentItemIds.Add(drop.ItemId);
                }
                else
                {
                    stackAdds.TryGetValue(drop.ItemId, out int previous);
                    stackAdds[drop.ItemId] = checked(previous + drop.Quantity);
                }
            }

            Dictionary<string, InventoryStackSaveData> existingStacks =
                new Dictionary<string, InventoryStackSaveData>(StringComparer.Ordinal);
            foreach (InventoryStackSaveData stack in save.inventory.stacks)
            {
                if (stack == null || string.IsNullOrEmpty(stack.itemId))
                    continue;
                if (!existingStacks.TryAdd(stack.itemId, stack))
                    throw new InvalidOperationException(
                        $"Inventory contains duplicate stack entries for ItemId '{stack.itemId}'.");
            }
            foreach (KeyValuePair<string, int> stackAdd in stackAdds)
            {
                int currentQuantity = existingStacks.TryGetValue(stackAdd.Key,
                    out InventoryStackSaveData existing) ? existing.quantity : 0;
                checked { _ = currentQuantity + stackAdd.Value; }
            }

            List<ItemInstanceSaveData> equipmentInstances =
                new List<ItemInstanceSaveData>(equipmentItemIds.Count);
            HashSet<string> existingInstanceIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ItemInstanceSaveData instance in save.inventory.itemInstances)
                if (instance != null && !string.IsNullOrEmpty(instance.instanceId))
                    existingInstanceIds.Add(instance.instanceId);
            foreach (string itemId in equipmentItemIds)
            {
                string instanceId;
                do
                {
                    instanceId = Guid.NewGuid().ToString("N");
                }
                while (!existingInstanceIds.Add(instanceId));
                equipmentInstances.Add(new ItemInstanceSaveData
                {
                    instanceId = instanceId,
                    itemId = itemId,
                    level = 1,
                    upgradeLevel = 0,
                    locked = false
                });
            }

            GameSaveData updatedSave = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(save));
            if (updatedSave == null)
                throw new InvalidOperationException("Could not create a reward transaction save snapshot.");
            SaveMigration.Migrate(updatedSave);
            updatedSave.gold = updatedGold;
            updatedSave.playerExperience = updatedExperience;
            Dictionary<string, InventoryStackSaveData> updatedStacks =
                new Dictionary<string, InventoryStackSaveData>(StringComparer.Ordinal);
            foreach (InventoryStackSaveData stack in updatedSave.inventory.stacks)
                if (stack != null && !string.IsNullOrEmpty(stack.itemId))
                    updatedStacks.Add(stack.itemId, stack);
            foreach (KeyValuePair<string, int> stackAdd in stackAdds)
            {
                if (updatedStacks.TryGetValue(stackAdd.Key, out InventoryStackSaveData existing))
                    existing.quantity += stackAdd.Value;
                else
                {
                    InventoryStackSaveData addedStack = new InventoryStackSaveData
                    {
                        itemId = stackAdd.Key,
                        quantity = stackAdd.Value
                    };
                    updatedSave.inventory.stacks.Add(addedStack);
                    updatedStacks.Add(stackAdd.Key, addedStack);
                }
            }
            updatedSave.inventory.itemInstances.AddRange(equipmentInstances);
            updatedSave.rewardedStageIds.Add(package.StageId);
            saveManager.Save(updatedSave);
            return true;
        }

        void EnsureSaveLoaded()
        {
            if (saveManager.CurrentSave == null)
                saveManager.LoadOrCreate();
            GameSaveData save = saveManager.CurrentSave;
            save.rewardedStageIds = save.rewardedStageIds ?? new List<string>();
            save.inventory = save.inventory ?? new InventorySaveData();
            save.inventory.stacks = save.inventory.stacks ?? new List<InventoryStackSaveData>();
            save.inventory.itemInstances =
                save.inventory.itemInstances ?? new List<ItemInstanceSaveData>();
        }
    }
}
