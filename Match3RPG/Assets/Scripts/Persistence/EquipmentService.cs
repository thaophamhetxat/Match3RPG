using System;
using System.Collections.Generic;
using Match3RPG.Configuration;
using UnityEngine;

namespace Match3RPG.Persistence
{
    public sealed class EquipmentService
    {
        readonly SaveManager saveManager;
        readonly Func<string, ItemConfig> getItem;

        public EquipmentService(SaveManager saveManager, GameConfigService configService)
            : this(saveManager, configService != null
                ? new Func<string, ItemConfig>(configService.GetItem)
                : throw new ArgumentNullException(nameof(configService)))
        {
        }

        public EquipmentService(SaveManager saveManager, Func<string, ItemConfig> itemResolver)
        {
            this.saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            getItem = itemResolver ?? throw new ArgumentNullException(nameof(itemResolver));
            EnsureSaveLoaded();
        }

        public bool Equip(string instanceId, EquipmentSlot slot)
        {
            ItemInstanceSaveData instance = FindInstance(instanceId);
            if (!TryGetValidSlot(slot, out string slotId)
                || instance == null
                || FindSlotEntry(slotId) != null)
                return false;

            ItemConfig item = getItem(instance.itemId);
            if (!InventoryService.IsEquippable(item)
                || item.EquipmentSlot != slot
                || IsInstanceEquippedAnywhere(instanceId))
                return false;

            saveManager.CurrentSave.loadout.equipment.Add(
                new EquipmentLoadoutSaveData { slotId = slotId, itemInstanceId = instanceId });
            SaveChanges();
            return true;
        }

        public bool Unequip(EquipmentSlot slot)
        {
            if (!TryGetValidSlot(slot, out string slotId))
                return false;

            List<EquipmentLoadoutSaveData> equipment = saveManager.CurrentSave.loadout.equipment;
            EquipmentLoadoutSaveData entry = FindSlotEntry(slotId);
            if (entry == null)
                return false;

            equipment.Remove(entry);
            SaveChanges();
            return true;
        }

        public IReadOnlyList<EquipmentLoadoutSaveData> GetEquipmentLoadout()
        {
            return saveManager.CurrentSave.loadout.equipment.AsReadOnly();
        }

        public bool ValidateEquipmentLoadout(out IReadOnlyList<string> errors)
        {
            List<string> validationErrors = new List<string>();
            HashSet<string> usedInstances = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> usedSlots = new HashSet<string>(StringComparer.Ordinal);

            foreach (EquipmentLoadoutSaveData entry in saveManager.CurrentSave.loadout.equipment)
            {
                if (entry == null)
                {
                    validationErrors.Add("Equipment loadout contains a null entry.");
                    continue;
                }

                if (!TryParseSlot(entry.slotId, out EquipmentSlot slot))
                {
                    validationErrors.Add($"Equipment loadout has an invalid slot ID '{entry.slotId}'.");
                    continue;
                }

                if (!usedSlots.Add(entry.slotId))
                    validationErrors.Add($"Equipment slot '{entry.slotId}' is used more than once.");

                if (string.IsNullOrEmpty(entry.itemInstanceId))
                {
                    validationErrors.Add($"Equipment slot '{entry.slotId}' has no item instance ID.");
                    continue;
                }

                if (!usedInstances.Add(entry.itemInstanceId))
                    validationErrors.Add(
                        $"Equipment instance '{entry.itemInstanceId}' is assigned to multiple slots.");

                ItemInstanceSaveData instance = FindInstance(entry.itemInstanceId);
                if (instance == null)
                {
                    validationErrors.Add(
                        $"Equipment instance '{entry.itemInstanceId}' does not exist in inventory.");
                    continue;
                }

                ItemConfig item = getItem(instance.itemId);
                if (item == null)
                {
                    validationErrors.Add(
                        $"Item definition '{instance.itemId}' for instance '{instance.instanceId}' was not found.");
                    continue;
                }

                if (!InventoryService.IsEquippable(item))
                    validationErrors.Add($"Item '{instance.itemId}' is not equippable.");
                else if (item.EquipmentSlot != slot)
                    validationErrors.Add(
                        $"Item '{instance.itemId}' requires slot '{ToSlotId(item.EquipmentSlot)}', "
                        + $"not '{entry.slotId}'.");
            }

            errors = validationErrors.AsReadOnly();
            return validationErrors.Count == 0;
        }

        bool IsInstanceEquippedAnywhere(string instanceId)
        {
            foreach (EquipmentLoadoutSaveData entry in saveManager.CurrentSave.loadout.equipment)
                if (entry != null
                    && string.Equals(entry.itemInstanceId, instanceId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        EquipmentLoadoutSaveData FindSlotEntry(string slotId)
        {
            foreach (EquipmentLoadoutSaveData entry in saveManager.CurrentSave.loadout.equipment)
                if (entry != null && string.Equals(entry.slotId, slotId, StringComparison.Ordinal))
                    return entry;
            return null;
        }

        ItemInstanceSaveData FindInstance(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return null;
            foreach (ItemInstanceSaveData instance in saveManager.CurrentSave.inventory.itemInstances)
                if (instance != null && string.Equals(instance.instanceId, instanceId, StringComparison.Ordinal))
                    return instance;
            return null;
        }

        void EnsureSaveLoaded()
        {
            if (saveManager == null)
                throw new ArgumentNullException(nameof(saveManager));
            if (getItem == null)
                throw new ArgumentNullException(nameof(getItem));
            if (saveManager.CurrentSave == null)
                saveManager.LoadOrCreate();

            GameSaveData save = saveManager.CurrentSave;
            save.inventory = save.inventory ?? new InventorySaveData();
            save.inventory.itemInstances = save.inventory.itemInstances ?? new List<ItemInstanceSaveData>();
            save.loadout = save.loadout ?? new PlayerLoadoutSaveData();
            save.loadout.equipment = save.loadout.equipment ?? new List<EquipmentLoadoutSaveData>();
        }

        void SaveChanges()
        {
            saveManager.SaveNow();
        }

        static bool TryGetValidSlot(EquipmentSlot slot, out string slotId)
        {
            slotId = ToSlotId(slot);
            return slotId != null;
        }

        static bool TryParseSlot(string slotId, out EquipmentSlot slot)
        {
            switch (slotId)
            {
                case "weapon": slot = EquipmentSlot.Weapon; return true;
                case "head": slot = EquipmentSlot.Head; return true;
                case "body": slot = EquipmentSlot.Body; return true;
                case "accessory": slot = EquipmentSlot.Accessory; return true;
                case "gloves": slot = EquipmentSlot.Gloves; return true;
                case "feet": slot = EquipmentSlot.Feet; return true;
                default: slot = EquipmentSlot.None; return false;
            }
        }

        static string ToSlotId(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: return "weapon";
                case EquipmentSlot.Head: return "head";
                case EquipmentSlot.Body: return "body";
                case EquipmentSlot.Accessory: return "accessory";
                case EquipmentSlot.Gloves: return "gloves";
                case EquipmentSlot.Feet: return "feet";
                default: return null;
            }
        }
    }
}
