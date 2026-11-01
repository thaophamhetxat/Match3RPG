using System;
using System.Collections.Generic;
using Match3RPG.Configuration;

namespace Match3RPG.Persistence
{
    public struct ItemStatValues
    {
        public int AttackBonus { get; }
        public int MaxHealthBonus { get; }

        public ItemStatValues(int attackBonus, int maxHealthBonus)
        {
            AttackBonus = attackBonus;
            MaxHealthBonus = maxHealthBonus;
        }
    }

    public static class ItemStatCalculator
    {
        public static ItemStatValues Calculate(ItemConfig item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            return new ItemStatValues(item.AttackBonus, item.MaxHealthBonus);
        }

        public static ItemStatValues CalculateEquipped(
            PlayerLoadoutSaveData loadout,
            InventorySaveData inventory,
            Func<string, ItemConfig> itemResolver)
        {
            if (loadout == null)
                throw new ArgumentNullException(nameof(loadout));
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));
            if (itemResolver == null)
                throw new ArgumentNullException(nameof(itemResolver));

            int attackBonus = 0;
            int healthBonus = 0;
            HashSet<string> usedInstances = new HashSet<string>(StringComparer.Ordinal);
            foreach (EquipmentLoadoutSaveData equipment in
                loadout.equipment ?? new List<EquipmentLoadoutSaveData>())
            {
                if (equipment == null || string.IsNullOrEmpty(equipment.itemInstanceId)
                    || !usedInstances.Add(equipment.itemInstanceId))
                    continue;

                ItemInstanceSaveData instance = FindInstance(inventory.itemInstances,
                    equipment.itemInstanceId);
                ItemConfig item = instance != null ? itemResolver(instance.itemId) : null;
                if (!InventoryService.IsEquippable(item))
                    continue;

                ItemStatValues values = Calculate(item);
                attackBonus = checked(attackBonus + values.AttackBonus);
                healthBonus = checked(healthBonus + values.MaxHealthBonus);
            }

            return new ItemStatValues(attackBonus, healthBonus);
        }

        static ItemInstanceSaveData FindInstance(
            IReadOnlyList<ItemInstanceSaveData> instances, string instanceId)
        {
            if (instances == null)
                return null;
            foreach (ItemInstanceSaveData instance in instances)
                if (instance != null && string.Equals(instance.instanceId, instanceId,
                    StringComparison.Ordinal))
                    return instance;
            return null;
        }
    }
}
