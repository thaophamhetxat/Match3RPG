using System;
using System.Collections.Generic;
using System.IO;
using Match3RPG.Configuration;
using Match3RPG.Persistence;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    public static class InventoryEquipmentRegressionTests
    {
        [MenuItem("Tools/Match3RPG/Save/Run Inventory and Equipment Tests")]
        public static void RunAll()
        {
            string testDirectory = Path.Combine(
                Path.GetTempPath(), "Match3RPG_InventoryTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectory);

            ItemConfig stackItem = CreateItem("potion_red", ItemType.Consumable, EquipmentSlot.None);
            ItemConfig equipmentItem = CreateItem("iron_sword", ItemType.Equipment, EquipmentSlot.Weapon);
            try
            {
                TestEmptyInventory(testDirectory, stackItem, equipmentItem);
                TestStackQuantitySaveReload(testDirectory, stackItem);
                TestDistinctEquipmentInstances(testDirectory, equipmentItem);
                TestInvalidSlotRejected(testDirectory, equipmentItem);
                TestDuplicateEquipmentUsageRejected(testDirectory, equipmentItem);
                Debug.Log("Inventory/equipment regression tests passed: empty inventory, stack save/reload, "
                    + "distinct equipment instances, invalid slot rejection, duplicate usage rejection.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stackItem);
                UnityEngine.Object.DestroyImmediate(equipmentItem);
                if (Directory.Exists(testDirectory))
                    Directory.Delete(testDirectory, true);
            }
        }

        static void TestEmptyInventory(string root, ItemConfig stackItem, ItemConfig equipmentItem)
        {
            SaveManager manager = CreateManager(root, "empty");
            InventoryService inventory = CreateInventory(manager, stackItem, equipmentItem);
            Require(inventory.GetQuantity(stackItem.StableId) == 0, "Empty stack inventory was not empty.");
            Require(manager.CurrentSave.inventory.itemInstances.Count == 0,
                "Empty equipment inventory was not empty.");
            Require(!inventory.HasItem(stackItem.StableId), "Empty inventory unexpectedly has an item.");
        }

        static void TestStackQuantitySaveReload(string root, ItemConfig stackItem)
        {
            SaveManager manager = CreateManager(root, "stack");
            InventoryService inventory = CreateInventory(manager, stackItem);
            Require(inventory.AddStackItem(stackItem.StableId, 7), "Failed to add stack items.");
            Require(inventory.RemoveStackItem(stackItem.StableId, 2), "Failed to remove stack items.");

            SaveManager reloadedManager = new SaveManager(Path.GetDirectoryName(manager.MainFilePath));
            InventoryService reloadedInventory = CreateInventory(reloadedManager, stackItem);
            Require(reloadedInventory.GetQuantity(stackItem.StableId) == 5,
                "Stack quantity did not survive save/reload.");
        }

        static void TestDistinctEquipmentInstances(string root, ItemConfig equipmentItem)
        {
            SaveManager manager = CreateManager(root, "instances");
            InventoryService inventory = CreateInventory(manager, equipmentItem);
            ItemInstanceSaveData first = inventory.AddEquipmentInstance(equipmentItem.StableId);
            ItemInstanceSaveData second = inventory.AddEquipmentInstance(equipmentItem.StableId);

            Require(first.itemId == second.itemId, "Equipment test instances did not share an ItemId.");
            Require(!string.IsNullOrEmpty(first.instanceId)
                && !string.Equals(first.instanceId, second.instanceId, StringComparison.Ordinal),
                "Equipment instances were not given distinct InstanceIds.");
            Require(inventory.GetEquipmentInstance(first.instanceId) != null
                && inventory.GetEquipmentInstance(second.instanceId) != null,
                "A distinct equipment instance could not be resolved.");

            SaveManager reloadedManager = new SaveManager(Path.GetDirectoryName(manager.MainFilePath));
            InventoryService reloadedInventory = CreateInventory(reloadedManager, equipmentItem);
            Require(reloadedInventory.GetEquipmentInstance(first.instanceId) != null
                && reloadedInventory.GetEquipmentInstance(second.instanceId) != null
                && reloadedManager.CurrentSave.inventory.itemInstances.Count == 2,
                "Distinct equipment instances did not survive save/reload.");
        }

        static void TestInvalidSlotRejected(string root, ItemConfig equipmentItem)
        {
            SaveManager manager = CreateManager(root, "invalid-slot");
            InventoryService inventory = CreateInventory(manager, equipmentItem);
            ItemInstanceSaveData instance = inventory.AddEquipmentInstance(equipmentItem.StableId);
            EquipmentService equipment = new EquipmentService(manager, id =>
                id == equipmentItem.StableId ? equipmentItem : null);

            Require(!equipment.Equip(instance.instanceId, EquipmentSlot.Head),
                "Equipment was accepted in a slot different from its configured slot.");
            Require(equipment.GetEquipmentLoadout().Count == 0,
                "Rejected equipment was still added to the loadout.");
        }

        static void TestDuplicateEquipmentUsageRejected(string root, ItemConfig equipmentItem)
        {
            SaveManager manager = CreateManager(root, "duplicate-usage");
            InventoryService inventory = CreateInventory(manager, equipmentItem);
            ItemInstanceSaveData instance = inventory.AddEquipmentInstance(equipmentItem.StableId);
            EquipmentService equipment = new EquipmentService(manager, id =>
                id == equipmentItem.StableId ? equipmentItem : null);

            Require(equipment.Equip(instance.instanceId, EquipmentSlot.Weapon),
                "Valid equipment could not be equipped.");
            Require(!equipment.Equip(instance.instanceId, EquipmentSlot.Weapon),
                "An already-equipped instance was accepted a second time.");

            manager.CurrentSave.loadout.equipment.Add(new EquipmentLoadoutSaveData
            {
                slotId = "head",
                itemInstanceId = instance.instanceId
            });
            Require(!equipment.ValidateEquipmentLoadout(out IReadOnlyList<string> errors),
                "Duplicate equipment instance usage was not reported.");
            Require(ContainsError(errors, "assigned to multiple slots"),
                "Loadout validation did not identify the duplicate instance.");
        }

        static SaveManager CreateManager(string root, string name)
        {
            SaveManager manager = new SaveManager(Path.Combine(root, name));
            manager.CreateNewSave();
            return manager;
        }

        static InventoryService CreateInventory(SaveManager manager, params ItemConfig[] items)
        {
            Dictionary<string, ItemConfig> definitions = new Dictionary<string, ItemConfig>(
                StringComparer.Ordinal);
            foreach (ItemConfig item in items)
                definitions.Add(item.StableId, item);
            return new InventoryService(manager, id =>
                definitions.TryGetValue(id, out ItemConfig item) ? item : null);
        }

        static ItemConfig CreateItem(string id, ItemType type, EquipmentSlot slot)
        {
            ItemConfig item = ScriptableObject.CreateInstance<ItemConfig>();
            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("stableId").stringValue = id;
            serialized.FindProperty("itemType").enumValueIndex = (int)type;
            serialized.FindProperty("equipmentSlot").enumValueIndex = (int)slot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        static bool ContainsError(IReadOnlyList<string> errors, string expectedText)
        {
            foreach (string error in errors)
                if (error.IndexOf(expectedText, StringComparison.Ordinal) >= 0)
                    return true;
            return false;
        }

        static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
