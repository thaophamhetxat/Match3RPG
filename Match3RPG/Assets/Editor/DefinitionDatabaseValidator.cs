using System;
using System.Collections.Generic;
using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Core;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    public static class DefinitionDatabaseValidator
    {
        [MenuItem("Tools/Match3RPG/Validate Definition Databases")]
        public static void ValidateAll()
        {
            List<string> warnings = new List<string>();
            List<string> failures = CollectFailures(warnings);
            foreach (string warning in warnings)
                Debug.LogWarning(warning);
            foreach (string failure in failures)
                Debug.LogError(failure);

            if (failures.Count > 0)
                throw new InvalidOperationException(
                    $"Definition validation failed with {failures.Count} error(s).");

            Debug.Log("Definition validation passed.");
        }

        static List<string> CollectFailures(List<string> warnings)
        {
            var failures = new List<string>();
            List<HeroData> heroes = LoadAssets<HeroData>();
            List<BossData> bosses = LoadAssets<BossData>();
            List<RegionData> regions = LoadAssets<RegionData>();
            List<ItemConfig> items = LoadAssets<ItemConfig>();
            List<SkillConfig> skills = LoadAssets<SkillConfig>();
            List<PetConfig> pets = LoadAssets<PetConfig>();
            List<CompanionConfig> companions = LoadAssets<CompanionConfig>();
            List<LootTableConfig> lootTables = LoadAssets<LootTableConfig>();
            List<EquipmentSetConfig> equipmentSets = LoadAssets<EquipmentSetConfig>();

            HashSet<string> heroIds = ValidateAssetIds(heroes, hero => hero.StableId, "Hero", failures);
            HashSet<string> bossIds = ValidateAssetIds(bosses, boss => boss.StableId, "Boss", failures);
            HashSet<string> regionIds = ValidateAssetIds(regions, region => region.StableId, "Region", failures);
            HashSet<string> itemIds = ValidateAssetIds(items, item => item.StableId, "Item", failures);
            HashSet<string> skillIds = ValidateAssetIds(skills, skill => skill.StableId, "Skill", failures);
            ValidateAssetIds(pets, pet => pet.StableId, "Pet", failures);
            ValidateAssetIds(companions, companion => companion.StableId, "Companion", failures);
            ValidateAssetIds(lootTables, table => table.StableId, "Loot table", failures);
            ValidateAssetIds(equipmentSets, equipmentSet => equipmentSet.StableId,
                "Equipment set", failures);

            ValidateHeroDefinitions(heroes, failures);
            ValidateBossDefinitions(bosses, failures);
            ValidateRegionMapSprites(regions, warnings);
            Dictionary<string, HashSet<string>> bossRegions =
                ValidateStages(regions, regionIds, bossIds, failures,
                    out Dictionary<string, HashSet<string>> stageBossesByRegion);
            ValidateRosters(LoadAssets<RegionBossRosterDatabase>(), regionIds, bossIds,
                bossRegions, stageBossesByRegion, failures, warnings);
            ValidateItems(items, failures);
            ValidateLootTables(lootTables, itemIds, failures);
            ValidateLoadouts(LoadAssets<LoadoutConfig>(), heroIds, itemIds, skillIds, failures);
            ValidateEquipmentSets(equipmentSets, itemIds, failures);
            ValidateAutoSkills(pets, pet => pet.AutoSkillIds, "Pet", skillIds, failures);
            ValidateAutoSkills(companions, companion => companion.AutoSkillIds,
                "Companion", skillIds, failures);

            ValidateDatabases(LoadAssets<HeroDatabase>(), database => database.Heroes,
                hero => hero != null ? hero.StableId : null, "Hero database", failures);
            ValidateDatabases(LoadAssets<BossDatabase>(), database => database.Bosses,
                boss => boss != null ? boss.StableId : null, "Boss database", failures);
            ValidateDatabases(LoadAssets<RegionDatabase>(), database => database.Regions,
                region => region != null ? region.StableId : null, "Region database", failures);
            ValidateDatabases(LoadAssets<ItemDatabase>(), database => database.Items,
                item => item != null ? item.StableId : null, "Item database", failures);
            ValidateDatabases(LoadAssets<SkillDatabase>(), database => database.Skills,
                skill => skill != null ? skill.StableId : null, "Skill database", failures);
            ValidateDatabases(LoadAssets<PetDatabase>(), database => database.Pets,
                pet => pet != null ? pet.StableId : null, "Pet database", failures);
            ValidateDatabases(LoadAssets<CompanionDatabase>(), database => database.Companions,
                companion => companion != null ? companion.StableId : null,
                "Companion database", failures);
            ValidateDatabases(LoadAssets<LootTableDatabase>(), database => database.LootTables,
                table => table != null ? table.StableId : null,
                "Loot table database", failures);
            ValidateDatabases(LoadAssets<EquipmentSetDatabase>(), database => database.EquipmentSets,
                set => set != null ? set.StableId : null,
                "Equipment set database", failures);
            ValidateStageDatabases(LoadAssets<StageDatabase>(), failures);
            ValidateCampaignCatalogs(LoadAssets<CampaignCatalog>(), regionIds, failures);

            return failures;
        }

        static void ValidateRegionMapSprites(IReadOnlyList<RegionData> regions,
            List<string> warnings)
        {
            foreach (RegionData region in regions)
                if (region.MapSprite == null)
                    warnings.Add($"RegionConfig '{region.RegionId}' is missing RegionMapSprite. "
                        + $"Asset: {AssetPath(region)}");
        }

        static void ValidateCampaignCatalogs(IReadOnlyList<CampaignCatalog> catalogs,
            HashSet<string> knownRegionIds, List<string> failures)
        {
            foreach (CampaignCatalog catalog in catalogs)
            {
                var regionIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (RegionData region in catalog.Regions)
                {
                    if (region == null)
                    {
                        failures.Add($"{AssetPath(catalog)}: CampaignCatalog contains an empty Region entry.");
                        continue;
                    }
                    if (!knownRegionIds.Contains(region.RegionId))
                        failures.Add($"{AssetPath(catalog)}: RegionId '{region.RegionId}' does not resolve "
                            + "to a RegionConfig asset.");
                    if (!regionIds.Add(region.RegionId))
                        failures.Add($"{AssetPath(catalog)}: Duplicate RegionId '{region.RegionId}'.");
                }
            }
        }

        static void ValidateHeroDefinitions(IReadOnlyList<HeroData> heroes, List<string> failures)
        {
            foreach (HeroData hero in heroes)
            {
                if (hero.VisualScale <= 0f || float.IsNaN(hero.VisualScale) ||
                    float.IsInfinity(hero.VisualScale))
                    failures.Add($"{AssetPath(hero)}: Hero VisualScale must be greater than zero.");
            }
        }

        static void ValidateBossDefinitions(IReadOnlyList<BossData> bosses, List<string> failures)
        {
            foreach (BossData boss in bosses)
            {
                if (boss.VisualScale <= 0f || float.IsNaN(boss.VisualScale) ||
                    float.IsInfinity(boss.VisualScale))
                    failures.Add($"{AssetPath(boss)}: Boss VisualScale must be greater than zero.");
                if (boss.MaxHP <= 0)
                    failures.Add($"{AssetPath(boss)}: Boss MaxHP must be greater than zero.");
                if (boss.Attack < 0)
                    failures.Add($"{AssetPath(boss)}: Boss Attack cannot be negative.");
            }
        }

        static Dictionary<string, HashSet<string>> ValidateStages(
            IReadOnlyList<RegionData> regions, HashSet<string> regionIds,
            HashSet<string> bossIds, List<string> failures,
            out Dictionary<string, HashSet<string>> stageBossesByRegion)
        {
            var stageIds = new HashSet<string>(StringComparer.Ordinal);
            var bossRegions = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            stageBossesByRegion = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (RegionData region in regions)
            {
                if (region.Stages == null)
                    continue;
                foreach (StageData stage in region.Stages)
                {
                    if (stage == null)
                        continue;

                    string path = AssetPath(region);
                    ValidateId(stage.StableId, $"Stage in {path}", path, stageIds, failures);
                    if (string.IsNullOrEmpty(stage.RegionId) || !regionIds.Contains(stage.RegionId))
                        failures.Add($"{path}: Stage '{stage.StableId}' has invalid RegionId '{stage.RegionId}'.");
                    else if (!string.Equals(stage.RegionId, region.RegionId, StringComparison.Ordinal))
                        failures.Add($"{path}: Stage '{stage.StableId}' RegionId '{stage.RegionId}' "
                            + $"does not match owning Region '{region.RegionId}'.");

                    if (stage.IsBossStage && stage.BossData == null)
                    {
                        failures.Add($"{path}: Boss stage '{stage.StableId}' has no BossId/reference.");
                        continue;
                    }
                    if (stage.BossData == null)
                        continue;

                    string bossId = stage.BossId;
                    if (!StableIdRules.IsValid(bossId) || !bossIds.Contains(bossId))
                    {
                        failures.Add($"{path}: Stage '{stage.StableId}' has invalid BossId '{bossId}'.");
                        continue;
                    }

                    if (!bossRegions.TryGetValue(bossId, out HashSet<string> mappedRegions))
                    {
                        mappedRegions = new HashSet<string>(StringComparer.Ordinal);
                        bossRegions.Add(bossId, mappedRegions);
                    }
                    mappedRegions.Add(region.RegionId);

                    if (!stageBossesByRegion.TryGetValue(region.RegionId,
                        out HashSet<string> regionBossIds))
                    {
                        regionBossIds = new HashSet<string>(StringComparer.Ordinal);
                        stageBossesByRegion.Add(region.RegionId, regionBossIds);
                    }
                    regionBossIds.Add(bossId);
                }
            }
            return bossRegions;
        }

        static void ValidateRosters(IReadOnlyList<RegionBossRosterDatabase> databases,
            HashSet<string> regionIds, HashSet<string> bossIds,
            Dictionary<string, HashSet<string>> bossRegions,
            Dictionary<string, HashSet<string>> stageBossesByRegion,
            List<string> failures, List<string> warnings)
        {
            var enabledBossesByRegion = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (RegionBossRosterDatabase database in databases)
            {
                var bossDatabaseIds = new HashSet<string>(StringComparer.Ordinal);
                if (database.BossDatabase == null)
                    failures.Add($"{AssetPath(database)}: Region Boss roster has no BossDatabase reference.");
                else
                    foreach (BossData boss in database.BossDatabase.Bosses)
                        if (boss != null)
                            bossDatabaseIds.Add(boss.StableId);

                var ordersByRegion = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
                foreach (RegionBossRosterEntryConfig entry in database.Entries)
                {
                    if (entry == null)
                    {
                        failures.Add($"{AssetPath(database)}: Roster contains an empty entry.");
                        continue;
                    }

                    string regionId = entry.RegionId;
                    string bossId = entry.BossId;
                    if (!StableIdRules.IsValid(regionId) || !regionIds.Contains(regionId))
                        failures.Add($"{AssetPath(database)}: Roster has invalid RegionId '{regionId}'.");
                    if (!StableIdRules.IsValid(bossId) || !bossIds.Contains(bossId))
                        failures.Add($"{AssetPath(database)}: Roster has invalid BossId '{bossId}'.");
                    if (database.BossDatabase != null && !bossDatabaseIds.Contains(bossId))
                        failures.Add($"{AssetPath(database)}: Roster BossId '{bossId}' is missing "
                            + "from its referenced BossDatabase.");

                    if (!string.IsNullOrEmpty(regionId))
                    {
                        if (!ordersByRegion.TryGetValue(regionId, out HashSet<int> orders))
                        {
                            orders = new HashSet<int>();
                            ordersByRegion.Add(regionId, orders);
                        }
                        if (!orders.Add(entry.Order))
                            failures.Add($"{AssetPath(database)}: Duplicate roster order {entry.Order} "
                                + $"in Region '{regionId}'.");
                    }

                    if (!string.IsNullOrEmpty(bossId) && !string.IsNullOrEmpty(regionId) &&
                        bossRegions.TryGetValue(bossId, out HashSet<string> mappedRegions) &&
                        mappedRegions.Count > 0 && !mappedRegions.Contains(regionId))
                        failures.Add($"{AssetPath(database)}: Roster BossId '{bossId}' belongs to "
                            + $"a different Region than '{regionId}'.");

                    if (entry.Enabled && !string.IsNullOrEmpty(regionId)
                        && !string.IsNullOrEmpty(bossId))
                    {
                        if (!enabledBossesByRegion.TryGetValue(regionId,
                            out HashSet<string> enabledBossIds))
                        {
                            enabledBossIds = new HashSet<string>(StringComparer.Ordinal);
                            enabledBossesByRegion.Add(regionId, enabledBossIds);
                        }
                        if (!enabledBossIds.Add(bossId))
                            failures.Add($"{AssetPath(database)}: BossId '{bossId}' is enabled more "
                                + $"than once in Region '{regionId}'.");
                    }
                }
            }

            foreach (KeyValuePair<string, HashSet<string>> regionEntry in stageBossesByRegion)
                foreach (string bossId in regionEntry.Value)
                    if (!enabledBossesByRegion.TryGetValue(regionEntry.Key,
                        out HashSet<string> enabledBossIds) || !enabledBossIds.Contains(bossId))
                        failures.Add($"Stage BossId '{bossId}' assigned in Region '{regionEntry.Key}' "
                            + "is missing from its enabled Region Boss roster.");

            foreach (KeyValuePair<string, HashSet<string>> rosterEntry in enabledBossesByRegion)
                foreach (string bossId in rosterEntry.Value)
                    if (!stageBossesByRegion.TryGetValue(rosterEntry.Key,
                        out HashSet<string> assignedBossIds) || !assignedBossIds.Contains(bossId))
                        warnings.Add($"Enabled roster BossId '{bossId}' in Region '{rosterEntry.Key}' "
                            + "is not assigned to any Stage.");
        }

        static void ValidateItems(IReadOnlyList<ItemConfig> items, List<string> failures)
        {
            foreach (ItemConfig item in items)
            {
                if (!Enum.IsDefined(typeof(ItemType), item.Type))
                    failures.Add($"{AssetPath(item)}: Item has an undefined ItemType.");
                if (!Enum.IsDefined(typeof(ItemRarity), item.Rarity))
                    failures.Add($"{AssetPath(item)}: Item has an undefined ItemRarity.");
                if (!Enum.IsDefined(typeof(EquipmentSlot), item.EquipmentSlot))
                {
                    failures.Add($"{AssetPath(item)}: Item has an undefined EquipmentSlot.");
                    continue;
                }

                bool isEquipment = item.Type == ItemType.Equipment;
                bool hasEquipmentSlot = item.EquipmentSlot != EquipmentSlot.None;
                if (isEquipment != hasEquipmentSlot)
                    failures.Add($"{AssetPath(item)}: Equipment items require a slot and "
                        + "non-equipment items must use EquipmentSlot.None.");
            }
        }

        static void ValidateLootTables(IReadOnlyList<LootTableConfig> lootTables,
            HashSet<string> itemIds, List<string> failures)
        {
            foreach (LootTableConfig table in lootTables)
            {
                foreach (LootTableEntryConfig entry in table.Entries)
                {
                    if (entry == null)
                    {
                        failures.Add($"{AssetPath(table)}: Loot table contains an empty entry.");
                        continue;
                    }
                    if (!StableIdRules.IsValid(entry.ItemId) || !itemIds.Contains(entry.ItemId))
                        failures.Add($"{AssetPath(table)}: Loot entry has invalid ItemId '{entry.ItemId}'.");
                    if (float.IsNaN(entry.DropChance) || float.IsInfinity(entry.DropChance) ||
                        entry.DropChance < 0f || entry.DropChance > 1f)
                        failures.Add($"{AssetPath(table)}: DropChance for ItemId '{entry.ItemId}' "
                            + "must be between 0 and 1.");
                    if (entry.MinimumQuantity <= 0
                        || entry.MaximumQuantity < entry.MinimumQuantity)
                        failures.Add($"{AssetPath(table)}: quantity range for ItemId "
                            + $"'{entry.ItemId}' must be positive and ordered.");
                }
            }
        }

        static void ValidateLoadouts(IReadOnlyList<LoadoutConfig> loadouts,
            HashSet<string> heroIds, HashSet<string> itemIds, HashSet<string> skillIds,
            List<string> failures)
        {
            foreach (LoadoutConfig loadout in loadouts)
            {
                if (!string.IsNullOrEmpty(loadout.HeroId) && !heroIds.Contains(loadout.HeroId))
                    failures.Add($"{AssetPath(loadout)}: Loadout has invalid HeroId '{loadout.HeroId}'.");
                ValidateReferences(loadout.ItemIds, itemIds, "ItemId", loadout, failures);
                ValidateReferences(loadout.SkillIds, skillIds, "SkillId", loadout, failures);
            }
        }

        static void ValidateEquipmentSets(IReadOnlyList<EquipmentSetConfig> equipmentSets,
            HashSet<string> itemIds, List<string> failures)
        {
            foreach (EquipmentSetConfig equipmentSet in equipmentSets)
                ValidateReferences(equipmentSet.ItemIds, itemIds, "ItemId", equipmentSet, failures);
        }

        static void ValidateAutoSkills<T>(IReadOnlyList<T> definitions,
            Func<T, string[]> getAutoSkillIds, string definitionType,
            HashSet<string> skillIds, List<string> failures) where T : StableIdConfig
        {
            foreach (T definition in definitions)
            {
                string[] autoSkillIds = getAutoSkillIds(definition);
                ValidateReferences(autoSkillIds, skillIds, "AutoSkillId", definition,
                    failures, definitionType);
            }
        }

        static void ValidateReferences<T>(IReadOnlyList<string> references, HashSet<string> knownIds,
            string idType, T owner, List<string> failures, string ownerType = null)
            where T : UnityEngine.Object
        {
            foreach (string referenceId in references)
                if (!StableIdRules.IsValid(referenceId) || !knownIds.Contains(referenceId))
                    failures.Add($"{AssetPath(owner)}: {ownerType ?? owner.GetType().Name} "
                        + $"has invalid {idType} '{referenceId}'.");
        }

        static void ValidateStageDatabases(IReadOnlyList<StageDatabase> databases,
            List<string> failures)
        {
            foreach (StageDatabase database in databases)
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                if (database.RegionDatabase == null)
                {
                    failures.Add($"{AssetPath(database)}: Stage database has no RegionDatabase.");
                    continue;
                }
                foreach (RegionData region in database.RegionDatabase.Regions)
                {
                    if (region == null || region.Stages == null)
                        continue;
                    foreach (StageData stage in region.Stages)
                    {
                        if (stage != null)
                            ValidateId(stage.StableId, "Stage", AssetPath(region), ids, failures);
                    }
                }
            }
        }

        static void ValidateDatabases<TDatabase, TDefinition>(
            IReadOnlyList<TDatabase> databases,
            Func<TDatabase, IEnumerable<TDefinition>> getDefinitions,
            Func<TDefinition, string> getStableId,
            string databaseType,
            List<string> failures)
            where TDatabase : UnityEngine.Object
            where TDefinition : UnityEngine.Object
        {
            foreach (TDatabase database in databases)
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (TDefinition definition in getDefinitions(database))
                {
                    if (definition == null)
                    {
                        failures.Add($"{AssetPath(database)}: {databaseType} contains an empty entry.");
                        continue;
                    }
                    string stableId = getStableId(definition);
                    ValidateId(stableId, databaseType, AssetPath(database), ids, failures);
                }
            }
        }

        static HashSet<string> ValidateAssetIds<T>(IReadOnlyList<T> assets,
            Func<T, string> getStableId, string definitionType, List<string> failures)
            where T : UnityEngine.Object
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (T asset in assets)
                ValidateId(getStableId(asset), definitionType, AssetPath(asset), ids, failures);
            return ids;
        }

        static void ValidateId(string stableId, string definitionType, string path,
            HashSet<string> ids, List<string> failures)
        {
            if (!StableIdRules.IsValid(stableId))
            {
                failures.Add($"{path}: {definitionType} has missing or invalid stable ID '{stableId}'.");
                return;
            }
            if (!ids.Add(stableId))
                failures.Add($"{path}: Duplicate {definitionType} stable ID '{stableId}'.");
        }

        static List<T> LoadAssets<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var assets = new List<T>(guids.Length);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    assets.Add(asset);
            }
            return assets;
        }

        static string AssetPath(UnityEngine.Object asset)
        {
            return asset != null ? AssetDatabase.GetAssetPath(asset) : "<missing asset>";
        }
    }
}
