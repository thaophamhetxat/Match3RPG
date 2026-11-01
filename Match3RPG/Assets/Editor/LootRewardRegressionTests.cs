using System;
using System.IO;
using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Persistence;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    public static class LootRewardRegressionTests
    {
        [MenuItem("Tools/Match3RPG/Run Loot Reward Tests")]
        public static void RunAll()
        {
            string testDirectory = Path.Combine(Path.GetTempPath(),
                "Match3RPG_LootRewardTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectory);
            ItemConfig stackItem = CreateItem("material_01", ItemType.Material);
            ItemConfig equipmentItem = CreateItem("equipment_01", ItemType.Equipment,
                EquipmentSlot.Weapon);
            BossData boss = ScriptableObject.CreateInstance<BossData>();
            LootTableEntryConfig stackDrop = CreateEntry("material_01", 2, 3, 1f);
            LootTableEntryConfig equipmentDrop = CreateEntry("equipment_01", 2, 2, 1f);
            LootTableEntryConfig firstClearDrop = CreateEntry("material_01", 5, 5, 1f, true);
            LootTableConfig table = ScriptableObject.CreateInstance<LootTableConfig>();
            table.Configure("loot_test", stackDrop, equipmentDrop, firstClearDrop);
            try
            {
                TestNoLootTable(boss, stackItem, table);
                TestStageAndBossTablePriority(boss, stackItem, table);
                TestStackAndEquipmentRewards(testDirectory, stackItem, equipmentItem,
                    boss, table);
                TestInvalidItemId(boss, table);
                TestFirstClearAndDuplicateVictory(testDirectory, stackItem, boss, table);
                Debug.Log("Loot and reward regressions passed: no table, table priority, "
                    + "stack/equipment drops, invalid ItemId, first-clear reward, "
                    + "and duplicate victory protection.");
            }
            finally
            {
                if (Directory.Exists(testDirectory))
                    Directory.Delete(testDirectory, true);
                UnityEngine.Object.DestroyImmediate(stackItem);
                UnityEngine.Object.DestroyImmediate(equipmentItem);
                UnityEngine.Object.DestroyImmediate(boss);
                UnityEngine.Object.DestroyImmediate(table);
            }
        }

        static void TestNoLootTable(BossData boss, ItemConfig item, LootTableConfig table)
        {
            StageData stage = CreateStage("region_01_stage_01", string.Empty, null);
            LootService noTables = new LootService(_ => null, _ => item,
                () => 0f, (minimum, _) => minimum);
            Require(noTables.Roll(stage, boss, true).Drops.Count == 0,
                "A missing LootTableDatabase produced item loot.");

            LootService emptyTableDatabase = new LootService(_ => null, _ => item,
                () => 0f, (minimum, _) => minimum);
            Require(emptyTableDatabase.Roll(stage, boss, true).Drops.Count == 0,
                "An empty LootTableDatabase was not handled as no loot.");
        }

        static void TestStageAndBossTablePriority(BossData boss, ItemConfig item,
            LootTableConfig table)
        {
            boss.SetDefaultLootTableId("loot_test");
            StageData stage = CreateStage("region_01_stage_02", "missing_stage_table", boss);
            LootService service = CreateLootService(table, _ => item);
            RuntimeLootResult selected = service.Roll(stage, boss, true);
            Require(selected.LootTableId == "missing_stage_table" && selected.Drops.Count == 0,
                "A non-empty Stage LootTableId did not take priority over Boss default.");

            stage.SetLootTableId(string.Empty);
            selected = service.Roll(stage, boss, true);
            Require(selected.LootTableId == "loot_test",
                "Boss DefaultLootTableId was not used when Stage LootTableId was empty.");
        }

        static void TestStackAndEquipmentRewards(string root, ItemConfig stackItem,
            ItemConfig equipmentItem, BossData boss, LootTableConfig table)
        {
            StageData stage = CreateStage("region_01_stage_03", "loot_test", boss);
            RuntimeLootResult loot = CreateLootService(table, id =>
                id == stackItem.StableId ? stackItem
                : id == equipmentItem.StableId ? equipmentItem
                : null).Roll(stage, boss, true);
            Require(loot.Drops.Count == 3,
                "Expected the stack, equipment, and first-clear drops from the test table.");

            SaveManager saveManager = NewManager(root, "stack-equipment");
            RewardService rewards = new RewardService(saveManager, id =>
                id == stackItem.StableId ? stackItem
                : id == equipmentItem.StableId ? equipmentItem
                : null);
            Require(rewards.Apply(new RewardPackage(stage.StableId, 0, 0, true, loot.Drops)),
                "First reward application did not apply.");
            Require(saveManager.CurrentSave.inventory.stacks.Count == 1
                && saveManager.CurrentSave.inventory.stacks[0].quantity == 7,
                "Stack drops were not combined into the saved stack quantity.");
            Require(saveManager.CurrentSave.inventory.itemInstances.Count == 2
                && saveManager.CurrentSave.inventory.itemInstances[0].instanceId
                    != saveManager.CurrentSave.inventory.itemInstances[1].instanceId,
                "Equipment drops did not create distinct equipment instances.");
        }

        static void TestInvalidItemId(BossData boss, LootTableConfig table)
        {
            LootTableEntryConfig invalidEntry = CreateEntry("INVALID_Item", 1, 1, 1f);
            LootTableConfig invalidTable = ScriptableObject.CreateInstance<LootTableConfig>();
            invalidTable.Configure("loot_invalid_item", invalidEntry);
            try
            {
                StageData stage = CreateStage("region_01_stage_04",
                    invalidTable.StableId, boss);
                LootService service = CreateLootService(invalidTable, _ =>
                {
                    throw new InvalidOperationException(
                        "Invalid ItemId should be rejected before resolving an item.");
                });
                Require(service.Roll(stage, boss, true).Drops.Count == 0,
                    "Loot service accepted an invalid ItemId.");
                Require(table.StableId == "loot_test",
                    "Valid LootTable was altered while testing invalid ItemId validation.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(invalidTable);
            }
        }

        static void TestFirstClearAndDuplicateVictory(string root,
            ItemConfig stackItem, BossData boss, LootTableConfig table)
        {
            StageData stage = CreateStage("region_01_stage_05", "loot_test", boss);
            RuntimeLootResult firstClearLoot = CreateLootService(table,
                id => id == stackItem.StableId ? stackItem : null)
                .Roll(stage, boss, true);
            RuntimeLootResult repeatLoot = CreateLootService(table,
                id => id == stackItem.StableId ? stackItem : null)
                .Roll(stage, boss, false);
            Require(firstClearLoot.Drops.Count == 2
                && repeatLoot.Drops.Count == 1
                && repeatLoot.Drops[0].Quantity == 2,
                "FirstClearOnly entries did not respect the first-clear state.");

            SaveManager saveManager = NewManager(root, "first-clear-repeat");
            RewardService rewards = new RewardService(saveManager, _ => stackItem);
            RewardPackage firstClearPackage = new RewardPackage(stage.StableId, 13, 17,
                true, firstClearLoot.Drops);
            Require(rewards.Apply(firstClearPackage), "First-clear reward was not applied.");
            int quantityAfterFirst = saveManager.CurrentSave.inventory.stacks[0].quantity;
            Require(!rewards.Apply(firstClearPackage),
                "Repeated victory applied the same Stage reward twice.");
            Require(saveManager.CurrentSave.gold == 13
                && saveManager.CurrentSave.playerExperience == 17
                && saveManager.CurrentSave.inventory.stacks[0].quantity == quantityAfterFirst
                && saveManager.CurrentSave.rewardedStageIds.Count == 1,
                "Repeated victory duplicated currencies, items, or reward tracking.");

            SaveManager nonFirstClearSave = NewManager(root, "non-first-clear");
            RewardService nonFirstClearRewards = new RewardService(nonFirstClearSave, _ => stackItem);
            Require(nonFirstClearRewards.Apply(new RewardPackage(
                "region_01_stage_06", 0, 0, false, firstClearLoot.Drops)),
                "Non-first-clear rewards were not processed.");
            Require(nonFirstClearSave.CurrentSave.inventory.stacks[0].quantity == 2,
                "FirstClearOnly item was granted on a repeat clear.");
        }

        static LootService CreateLootService(LootTableConfig table,
            Func<string, ItemConfig> itemResolver)
        {
            return new LootService(id => id == table.StableId ? table : null,
                itemResolver, () => 0f, (minimum, _) => minimum);
        }

        static StageData CreateStage(string stageId, string lootTableId, BossData boss)
        {
            StageData stage = new StageData();
            stage.Configure(stageId, 1, stageId, "region_01", false, Vector2.zero, boss);
            stage.SetLootTableId(lootTableId);
            return stage;
        }

        static LootTableEntryConfig CreateEntry(string itemId, int minimum, int maximum,
            float chance, bool firstClearOnly = false)
        {
            LootTableEntryConfig entry = new LootTableEntryConfig();
            entry.Configure(itemId, minimum, maximum, chance, firstClearOnly);
            return entry;
        }

        static ItemConfig CreateItem(string stableId, ItemType type,
            EquipmentSlot slot = EquipmentSlot.None)
        {
            ItemConfig item = ScriptableObject.CreateInstance<ItemConfig>();
            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("stableId").stringValue = stableId;
            serialized.FindProperty("itemType").enumValueIndex = (int)type;
            serialized.FindProperty("equipmentSlot").enumValueIndex = (int)slot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        static SaveManager NewManager(string root, string name)
        {
            SaveManager manager = new SaveManager(Path.Combine(root, name));
            manager.LoadOrCreate();
            return manager;
        }

        static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
