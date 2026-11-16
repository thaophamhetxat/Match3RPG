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
        public float DamagePercentBonus { get; }

        public ItemStatValues(
            int attackBonus,
            int maxHealthBonus,
            float attackPercentBonus = 0f,
            float maxHealthPercentBonus = 0f,
            float defensePercentBonus = 0f,
            float critRatePercentBonus = 0f,
            int speedBonus = 0,
            float critDamagePercentBonus = 0f,
            int ragePerGemBonus = 0,
            float damagePercentBonus = 0f)
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
            DamagePercentBonus = damagePercentBonus;
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
            return Calculate(item, upgradeLevel, 0);
        }

        public static ItemStatValues Calculate(ItemConfig item, int upgradeLevel, int qualityTier)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            int clampedUpgradeLevel = Math.Max(0, upgradeLevel);
            int clampedQualityTier = Math.Min(3, Math.Max(0, qualityTier));
            double upgradeMultiplier = 1.0 + 0.1 * clampedUpgradeLevel + 0.1 * clampedQualityTier;

            int attackBonus = ScaleIntBonus(item.AttackBonus, clampedUpgradeLevel, clampedQualityTier);
            int maxHealthBonus = ScaleIntBonus(item.MaxHealthBonus, clampedUpgradeLevel, clampedQualityTier);
            float attackPercentBonus = ScaleFloatBonus(item.AttackPercentBonus, upgradeMultiplier);
            float maxHealthPercentBonus = ScaleFloatBonus(item.MaxHealthPercentBonus, upgradeMultiplier);
            float defensePercentBonus = ScaleFloatBonus(item.DefensePercentBonus, upgradeMultiplier);
            float critRatePercentBonus = ScaleFloatBonus(item.CritRatePercentBonus, upgradeMultiplier);
            int speedBonus = ScaleIntBonus(item.SpeedBonus, clampedUpgradeLevel, clampedQualityTier);
            float critDamagePercentBonus = ScaleFloatBonus(item.CritDamagePercentBonus, upgradeMultiplier);
            int ragePerGemBonus = ScaleIntBonus(item.RagePerGemBonus, clampedUpgradeLevel, clampedQualityTier);
            float damagePercentBonus = ScaleFloatBonus(item.DamagePercentBonus, upgradeMultiplier);

            return new ItemStatValues(
                attackBonus,
                maxHealthBonus,
                attackPercentBonus,
                maxHealthPercentBonus,
                defensePercentBonus,
                critRatePercentBonus,
                speedBonus,
                critDamagePercentBonus,
                ragePerGemBonus,
                damagePercentBonus);
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
            float damagePercentBonus = 0f;

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

                int clampedQualityTier = Math.Min(3, Math.Max(0, instance.qualityTier));
                ItemStatValues values = Calculate(item, instance.upgradeLevel, clampedQualityTier);
                attackBonus = checked(attackBonus + values.AttackBonus);
                healthBonus = checked(healthBonus + values.MaxHealthBonus);
                attackPercentBonus += values.AttackPercentBonus;
                maxHealthPercentBonus += values.MaxHealthPercentBonus;
                defensePercentBonus += values.DefensePercentBonus;
                critRatePercentBonus += values.CritRatePercentBonus;
                speedBonus = checked(speedBonus + values.SpeedBonus);
                critDamagePercentBonus += values.CritDamagePercentBonus;
                ragePerGemBonus = checked(ragePerGemBonus + values.RagePerGemBonus);
                damagePercentBonus += values.DamagePercentBonus;
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
                ragePerGemBonus,
                damagePercentBonus);
        }

        static int ScaleIntBonus(int baseValue, int upgradeLevel, int qualityTier = 0)
        {
            if (baseValue <= 0)
                return 0;
            int clampedUpgrade = Math.Max(0, upgradeLevel);
            int clampedQuality = Math.Min(3, Math.Max(0, qualityTier));
            long scaled = ((long)baseValue * (10L + clampedUpgrade + clampedQuality)) / 10L;
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
