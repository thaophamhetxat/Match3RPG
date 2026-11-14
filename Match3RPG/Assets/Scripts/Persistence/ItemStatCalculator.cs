using System;
using System.Collections.Generic;
using Match3RPG.Configuration;

namespace Match3RPG.Persistence
{
    public struct ItemStatValues
    {
        public int AttackBonus { get; }
        public int MaxHealthBonus { get; }
        public float AttackPercentBonus { get; }
        public float MaxHealthPercentBonus { get; }
        public float DefensePercentBonus { get; }
        public float CritRatePercentBonus { get; }
        public int SpeedBonus { get; }
        public float CritDamagePercentBonus { get; }
        public int RagePerGemBonus { get; }

        public ItemStatValues(
            int attackBonus,
            int maxHealthBonus,
            float attackPercentBonus = 0f,
            float maxHealthPercentBonus = 0f,
            float defensePercentBonus = 0f,
            float critRatePercentBonus = 0f,
            int speedBonus = 0,
            float critDamagePercentBonus = 0f,
            int ragePerGemBonus = 0)
        {
            AttackBonus = attackBonus;
            MaxHealthBonus = maxHealthBonus;
            AttackPercentBonus = attackPercentBonus;
            MaxHealthPercentBonus = maxHealthPercentBonus;
            DefensePercentBonus = defensePercentBonus;
            CritRatePercentBonus = critRatePercentBonus;
            SpeedBonus = speedBonus;
            CritDamagePercentBonus = critDamagePercentBonus;
            RagePerGemBonus = ragePerGemBonus;
        }
    }

    public static class ItemStatCalculator
    {
        public static ItemStatValues Calculate(ItemConfig item)
        {
            return Calculate(item, 0);
        }

        public static ItemStatValues Calculate(ItemConfig item, int upgradeLevel)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            int clampedUpgradeLevel = Math.Max(0, upgradeLevel);
            double upgradeMultiplier = 1.0 + 0.1 * clampedUpgradeLevel;

            int attackBonus = ScaleIntBonus(item.AttackBonus, clampedUpgradeLevel);
            int maxHealthBonus = ScaleIntBonus(item.MaxHealthBonus, clampedUpgradeLevel);
            float attackPercentBonus = ScaleFloatBonus(item.AttackPercentBonus, upgradeMultiplier);
            float maxHealthPercentBonus = ScaleFloatBonus(item.MaxHealthPercentBonus, upgradeMultiplier);
            float defensePercentBonus = ScaleFloatBonus(item.DefensePercentBonus, upgradeMultiplier);
            float critRatePercentBonus = ScaleFloatBonus(item.CritRatePercentBonus, upgradeMultiplier);
            int speedBonus = ScaleIntBonus(item.SpeedBonus, clampedUpgradeLevel);
            float critDamagePercentBonus = ScaleFloatBonus(item.CritDamagePercentBonus, upgradeMultiplier);
            int ragePerGemBonus = ScaleIntBonus(item.RagePerGemBonus, clampedUpgradeLevel);

            return new ItemStatValues(
                attackBonus,
                maxHealthBonus,
                attackPercentBonus,
                maxHealthPercentBonus,
                defensePercentBonus,
                critRatePercentBonus,
                speedBonus,
                critDamagePercentBonus,
                ragePerGemBonus);
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
            float attackPercentBonus = 0f;
            float maxHealthPercentBonus = 0f;
            float defensePercentBonus = 0f;
            float critRatePercentBonus = 0f;
            int speedBonus = 0;
            float critDamagePercentBonus = 0f;
            int ragePerGemBonus = 0;

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

                ItemStatValues values = Calculate(item, instance.upgradeLevel);
                attackBonus = checked(attackBonus + values.AttackBonus);
                healthBonus = checked(healthBonus + values.MaxHealthBonus);
                attackPercentBonus += values.AttackPercentBonus;
                maxHealthPercentBonus += values.MaxHealthPercentBonus;
                defensePercentBonus += values.DefensePercentBonus;
                critRatePercentBonus += values.CritRatePercentBonus;
                speedBonus = checked(speedBonus + values.SpeedBonus);
                critDamagePercentBonus += values.CritDamagePercentBonus;
                ragePerGemBonus = checked(ragePerGemBonus + values.RagePerGemBonus);
            }

            return new ItemStatValues(
                attackBonus,
                healthBonus,
                attackPercentBonus,
                maxHealthPercentBonus,
                defensePercentBonus,
                critRatePercentBonus,
                speedBonus,
                critDamagePercentBonus,
                ragePerGemBonus);
        }

        static int ScaleIntBonus(int baseValue, int upgradeLevel)
        {
            if (baseValue <= 0)
                return 0;
            long scaled = ((long)baseValue * (10L + Math.Max(0, upgradeLevel))) / 10L;
            return scaled >= int.MaxValue ? int.MaxValue : (int)scaled;
        }

        static float ScaleFloatBonus(float baseValue, double upgradeMultiplier)
        {
            if (baseValue <= 0f)
                return 0f;
            return (float)Math.Round(baseValue * upgradeMultiplier, 4);
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
