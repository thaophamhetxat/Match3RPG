using System;
using System.Collections.Generic;
using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    public static class CampaignConfigIntegrationInstaller
    {
        const string ConfigurationFolder = "Assets/Resources/Configuration";
        const string BossDatabasePath = ConfigurationFolder + "/BossDatabase.asset";
        const string RegionDatabasePath = ConfigurationFolder + "/RegionDatabase.asset";
        const string StageDatabasePath = ConfigurationFolder + "/StageDatabase.asset";
        const string RegionBossRosterPath = ConfigurationFolder + "/RegionBossRosterDatabase.asset";
        const string GameConfigServicePath = ConfigurationFolder + "/GameConfigService.asset";
        const string CampaignCatalogPath = "Assets/Resources/Campaign/CampaignCatalog.asset";
        const string HeroDatabasePath = ConfigurationFolder + "/HeroDatabase.asset";
        const string Hero2Path = "Assets/Resources/Characters/Heroes/Hero2.asset";

        [MenuItem("Tools/Match3RPG/Install Region Boss Stage Config Integration")]
        public static void Install()
        {
            CampaignCatalog catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignCatalogPath);
            if (catalog == null)
                throw new InvalidOperationException(
                    $"CampaignCatalog was not found at {CampaignCatalogPath}.");

            if (!AssetDatabase.IsValidFolder(ConfigurationFolder))
                AssetDatabase.CreateFolder("Assets/Resources", "Configuration");
            BossData[] bosses = LoadBossDefinitions();
            BossDatabase bossDatabase = LoadOrCreate<BossDatabase>(BossDatabasePath);
            ConfigureBossDatabase(bossDatabase, bosses);

            HeroData hero2 = AssetDatabase.LoadAssetAtPath<HeroData>(Hero2Path);
            if (hero2 == null)
                throw new InvalidOperationException($"Hero2 was not found at {Hero2Path}.");
            HeroDatabase heroDatabase = LoadOrCreate<HeroDatabase>(HeroDatabasePath);
            ConfigureHeroDatabase(heroDatabase, hero2);

            RegionDatabase regionDatabase = LoadOrCreate<RegionDatabase>(RegionDatabasePath);
            SetObjectReference(regionDatabase, "campaignCatalog", catalog);

            StageDatabase stageDatabase = LoadOrCreate<StageDatabase>(StageDatabasePath);
            SetObjectReference(stageDatabase, "regionDatabase", regionDatabase);

            RegionBossRosterDatabase rosterDatabase =
                LoadOrCreate<RegionBossRosterDatabase>(RegionBossRosterPath);
            RegionBossRosterEntryConfig[] entries = BuildRosterEntries(catalog);
            ConfigureRosterDatabase(rosterDatabase, bossDatabase, entries);

            GameConfigService configService = LoadOrCreate<GameConfigService>(GameConfigServicePath);
            SetObjectReference(configService, "heroDatabase", heroDatabase);
            SetObjectReference(configService, "bossDatabase", bossDatabase);
            SetObjectReference(configService, "regionDatabase", regionDatabase);
            SetObjectReference(configService, "stageDatabase", stageDatabase);
            SetObjectReference(configService, "regionBossRosterDatabase", rosterDatabase);

            EditorUtility.SetDirty(bossDatabase);
            EditorUtility.SetDirty(heroDatabase);
            EditorUtility.SetDirty(regionDatabase);
            EditorUtility.SetDirty(stageDatabase);
            EditorUtility.SetDirty(rosterDatabase);
            EditorUtility.SetDirty(configService);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Campaign config integration installed. Hero definitions=1; "
                + $"Boss definitions={bosses.Length}; "
                + $"Regions={catalog.Regions.Count}; Stage mappings={CountStageMappings(catalog)}; "
                + $"enabled roster entries={entries.Length}.");
        }

        static BossData[] LoadBossDefinitions()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:BossData");
            List<BossData> bosses = new List<BossData>(assetGuids.Length);
            foreach (string assetGuid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                BossData boss = AssetDatabase.LoadAssetAtPath<BossData>(path);
                if (boss != null)
                    bosses.Add(boss);
            }
            bosses.Sort((left, right) => string.CompareOrdinal(left.StableId, right.StableId));
            return bosses.ToArray();
        }

        static RegionBossRosterEntryConfig[] BuildRosterEntries(CampaignCatalog catalog)
        {
            List<RegionBossRosterEntryConfig> entries = new List<RegionBossRosterEntryConfig>();
            foreach (RegionData region in catalog.Regions)
            {
                if (region == null || region.Stages == null)
                    continue;

                HashSet<string> seenBossIds = new HashSet<string>(StringComparer.Ordinal);
                int order = 0;
                foreach (StageData stage in region.Stages)
                {
                    string bossId = stage != null ? stage.BossId : string.Empty;
                    if (string.IsNullOrEmpty(bossId) || !seenBossIds.Add(bossId))
                        continue;
                    entries.Add(new RegionBossRosterEntryConfig(region.RegionId, bossId, order++, true));
                }
            }
            return entries.ToArray();
        }

        static int CountStageMappings(CampaignCatalog catalog)
        {
            int count = 0;
            foreach (RegionData region in catalog.Regions)
                if (region != null && region.Stages != null)
                    foreach (StageData stage in region.Stages)
                        if (stage != null && !string.IsNullOrEmpty(stage.BossId))
                            count++;
            return count;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void ConfigureBossDatabase(BossDatabase database, BossData[] bosses)
        {
            SerializedObject serialized = new SerializedObject(database);
            SerializedProperty definitions = serialized.FindProperty("bosses");
            definitions.arraySize = bosses.Length;
            for (int index = 0; index < bosses.Length; index++)
                definitions.GetArrayElementAtIndex(index).objectReferenceValue = bosses[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureHeroDatabase(HeroDatabase database, HeroData hero)
        {
            SerializedObject serialized = new SerializedObject(database);
            SerializedProperty definitions = serialized.FindProperty("heroes");
            definitions.arraySize = 1;
            definitions.GetArrayElementAtIndex(0).objectReferenceValue = hero;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureRosterDatabase(RegionBossRosterDatabase database,
            BossDatabase bossDatabase, RegionBossRosterEntryConfig[] entries)
        {
            SerializedObject serialized = new SerializedObject(database);
            serialized.FindProperty("bossDatabase").objectReferenceValue = bossDatabase;
            SerializedProperty definitions = serialized.FindProperty("entries");
            definitions.arraySize = entries.Length;
            for (int index = 0; index < entries.Length; index++)
            {
                SerializedProperty entry = definitions.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("regionId").stringValue = entries[index].RegionId;
                entry.FindPropertyRelative("bossId").stringValue = entries[index].BossId;
                entry.FindPropertyRelative("order").intValue = entries[index].Order;
                entry.FindPropertyRelative("enabled").boolValue = entries[index].Enabled;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetObjectReference(UnityEngine.Object target, string propertyName,
            UnityEngine.Object reference)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException(
                    $"Serialized property '{propertyName}' was not found on {target.GetType().Name}.");
            property.objectReferenceValue = reference;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
