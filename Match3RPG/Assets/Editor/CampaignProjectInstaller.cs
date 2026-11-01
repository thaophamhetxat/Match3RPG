using System.Collections.Generic;
using System.IO;
using Match3RPG.Campaign;
using Match3RPG.Campaign.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Match3RPG.Editor
{
    [InitializeOnLoad]
    public static class CampaignProjectInstaller
    {
        const string ResourcesFolder = "Assets/Resources";
        const string CampaignFolder = ResourcesFolder + "/Campaign";
        const string CampaignUiPath = "Assets/Art/UI/Campaign/";
        const string CampaignReferencePath = CampaignUiPath + "Reference/";
        const string RegionDataFolder = CampaignFolder + "/Regions";
        const string BossDataFolder = CampaignFolder + "/Bosses";
        const string CatalogPath = CampaignFolder + "/CampaignCatalog.asset";
        const string DevelopmentBossSpritePath = "Assets/Art/Bosses/Boss01/boss1_Idle.png";
        const string DevelopmentBossDataPath = BossDataFolder + "/DevelopmentBoss.asset";
        const string WorldScenePath = "Assets/Scenes/WorldMap.unity";
        const string RegionScenePath = "Assets/Scenes/RegionMap.unity";
        const string WorldMapSpritePath = "Assets/Art/Maps/World/WorldMap_Xianxia.png";
        const string LockedMarkerPath = "Assets/Art/UI/StageMarkers/Stage_Locked.png";
        const string AvailableMarkerPath = "Assets/Art/UI/StageMarkers/Stage_Green.png";
        const string CompletedMarkerPath = "Assets/Art/UI/StageMarkers/Stage_Gold.png";
        const string BossMarkerPath = "Assets/Art/UI/StageMarkers/Stage_Red.png";
        const string CampaignLockedMarkerPath = "Assets/Art/UI/StageMarkers/Stage_Locked.png";
        const string CampaignAvailableMarkerPath = "Assets/Art/UI/StageMarkers/Stage_Green.png";
        const string CampaignCompletedMarkerPath = "Assets/Art/UI/StageMarkers/Stage_Gold.png";
        const string CampaignBossMarkerPath = "Assets/Art/UI/StageMarkers/Stage_Red.png";
        const string CampaignGlowPath = "Assets/Art/UI/Campaign/StageMarkers/Stage_Glow.png";
        const string CampaignPlatformPath = "Assets/Art/UI/Campaign/StageMarkers/Stage_Platform.png";
        const string CampaignBackButtonPath = "Assets/Art/UI/Campaign/Navigation/Button_Back.png";
        const string CampaignRegionHeaderPath = "Assets/Art/UI/Campaign/Panels/Panel_TopBar.png";
        const string Region01HeaderPath = "Assets/Art/Maps/Regions/Region01/Region01_Header.png";
        const string Region01FooterPath = "Assets/Art/Maps/Regions/Region01/Region01_Footer.png";
        const string CampaignTooltipFramePath = "Assets/Art/UI/Campaign/Panels/Panel_BottomBar.png";

        static readonly string[] RegionIds =
        {
            "region_01", "region_02", "region_03", "region_04", "region_05"
        };
        static readonly string[] RegionNames =
        {
            "Celestial Immortal Sect", "Frozen Peaks", "Demon Volcano", "Thunder Valley", "Spirit Forest"
        };
        static readonly string[] MapPaths =
        {
            "Assets/Art/Maps/Regions/Region01/Region01_Map.png",
            "Assets/Art/Maps/Regions/Region02/Region02_FrozenPeaks.png",
            "Assets/Art/Maps/Regions/Region03/Region03_DemonVolcano.png",
            "Assets/Art/Maps/Regions/Region04/Region04_ThunderValley.png",
            "Assets/Art/Maps/Regions/Region05/Region05_SpiritForest.png"
        };
        static readonly string[][] BossSpritePaths =
        {
            new[]
            {
                "Assets/Art/Bosses/Region01/Boss_AbyssDragon.png",
                "Assets/Art/Bosses/Region01/Boss_ArcaneCultivator.png",
                "Assets/Art/Bosses/Region01/Boss_AzureRiverDragon.png",
                "Assets/Art/Bosses/Region01/Boss_CelestialSerpent.png"
            },
            new[]
            {
                "Assets/Art/Bosses/Region02/Boss_CloudDragonTurtle.png",
                "Assets/Art/Bosses/Region02/Boss_CrimsonScorpion.png",
                "Assets/Art/Bosses/Region02/Boss_EarthDrake.png",
                "Assets/Art/Bosses/Region02/Boss_FlameLion.png"
            },
            new[]
            {
                "Assets/Art/Bosses/Region03/Boss_FlameSerpent.png",
                "Assets/Art/Bosses/Region03/Boss_GoldenStoneGuardian.png",
                "Assets/Art/Bosses/Region03/Boss_IceCrystalBeast.png",
                "Assets/Art/Bosses/Region03/Boss_InfernoQilin.png"
            },
            new[]
            {
                "Assets/Art/Bosses/Region04/Boss_JadeForestDragon.png",
                "Assets/Art/Bosses/Region04/Boss_ShadowWyvern.png",
                "Assets/Art/Bosses/Region04/Boss_SpiritMountainTortoise.png",
                "Assets/Art/Bosses/Region04/Boss_ThunderGuardian.png"
            },
            System.Array.Empty<string>()
        };
        static readonly string[][] BossNames =
        {
            new[] { "Abyss Dragon", "Arcane Cultivator", "Azure River Dragon", "Celestial Serpent" },
            new[] { "Cloud Dragon Turtle", "Crimson Scorpion", "Earth Drake", "Flame Lion" },
            new[] { "Flame Serpent", "Golden Stone Guardian", "Ice Crystal Beast", "Inferno Qilin" },
            new[] { "Jade Forest Dragon", "Shadow Wyvern", "Spirit Mountain Tortoise", "Thunder Guardian" },
            new[] { "Spirit Forest Warden", "Spirit Forest Sovereign" }
        };
        static readonly Vector2[] WorldNodePositions =
        {
            new Vector2(.50f, .11f), new Vector2(.13f, .40f), new Vector2(.84f, .10f),
            new Vector2(.84f, .39f), new Vector2(.80f, .59f)
        };
        static readonly Color[] RegionColors =
        {
            new Color32(194, 157, 75, 255), new Color32(91, 179, 218, 255),
            new Color32(216, 86, 59, 255), new Color32(142, 113, 220, 255),
            new Color32(89, 183, 133, 255)
        };
        static readonly Vector2[][] StageNodePositions =
        {
            new[]
            {
                new Vector2(.413f, .799f), new Vector2(.492f, .750f), new Vector2(.350f, .695f),
                new Vector2(.385f, .612f), new Vector2(.589f, .554f), new Vector2(.298f, .500f),
                new Vector2(.585f, .426f), new Vector2(.385f, .348f), new Vector2(.615f, .228f),
                new Vector2(.417f, .120f)
            },
            new[]
            {
                new Vector2(.42f, .65f), new Vector2(.56f, .59f), new Vector2(.68f, .52f),
                new Vector2(.55f, .47f), new Vector2(.42f, .41f), new Vector2(.60f, .36f),
                new Vector2(.81f, .31f), new Vector2(.70f, .26f), new Vector2(.66f, .20f),
                new Vector2(.69f, .10f)
            },
            new[]
            {
                new Vector2(.41f, .73f), new Vector2(.56f, .65f), new Vector2(.60f, .61f),
                new Vector2(.43f, .50f), new Vector2(.57f, .42f), new Vector2(.42f, .35f),
                new Vector2(.56f, .28f), new Vector2(.70f, .20f), new Vector2(.61f, .15f),
                new Vector2(.62f, .09f)
            },
            new[]
            {
                new Vector2(.45f, .78f), new Vector2(.48f, .67f), new Vector2(.58f, .57f),
                new Vector2(.39f, .49f), new Vector2(.57f, .41f), new Vector2(.41f, .32f),
                new Vector2(.55f, .24f), new Vector2(.39f, .18f), new Vector2(.57f, .14f),
                new Vector2(.65f, .09f)
            },
            new[]
            {
                new Vector2(.45f, .69f), new Vector2(.55f, .57f), new Vector2(.43f, .45f),
                new Vector2(.59f, .39f), new Vector2(.45f, .34f), new Vector2(.70f, .28f),
                new Vector2(.43f, .22f), new Vector2(.67f, .17f), new Vector2(.61f, .13f),
                new Vector2(.62f, .09f)
            }
        };

        static CampaignProjectInstaller()
        {
            EditorApplication.delayCall += EnsureInstalled;
        }

        [MenuItem("Tools/Campaign/Generate Scenes and Data")]
        public static void GenerateCampaignProject()
        {
            EnsureInstalled(true);
        }

        [MenuItem("Tools/Campaign/Reset Campaign Progress")]
        static void ResetCampaignProgress()
        {
            CampaignCatalog catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"Campaign catalog is missing: {CatalogPath}");
                return;
            }

            GameProgressService.Reset(catalog);
            Debug.Log("Campaign progress reset to the first Region and Stage.");
        }

        static void EnsureInstalled() => EnsureInstalled(false);

        static void EnsureInstalled(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += () => EnsureInstalled(force);
                return;
            }

            bool missingInstall = !File.Exists(WorldScenePath) || !File.Exists(RegionScenePath)
                || CampaignUiReferencesMissing() || CampaignBossDataMissing()
                || CharacterContentMissing();
            if (!force && !missingInstall)
                return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CampaignCatalog catalog = EnsureCampaignData();
            if (catalog == null)
                return;

            if (CharacterContentMissing())
                CharacterContentBuilder.Build();
            EnsureMapScene(WorldScenePath, "WorldMap", catalog, true);
            EnsureMapScene(RegionScenePath, "RegionMap", catalog, false);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Campaign scenes, data, and build order are ready.");
        }

        static CampaignCatalog EnsureCampaignData()
        {
            EnsureCampaignUiImportSettings();
            EnsureFolder("Assets", "Resources");
            EnsureFolder(ResourcesFolder, "Campaign");
            EnsureFolder(CampaignFolder, "Regions");
            EnsureFolder(CampaignFolder, "Bosses");

            Sprite worldMap = LoadMapSprite(WorldMapSpritePath);
            if (worldMap == null)
                return null;
            Sprite lockedMarker = LoadOptionalMarker(LockedMarkerPath);
            Sprite availableMarker = LoadOptionalMarker(AvailableMarkerPath);
            Sprite completedMarker = LoadOptionalMarker(CompletedMarkerPath);
            Sprite bossMarker = LoadOptionalMarker(BossMarkerPath);
            Sprite campaignLockedMarker = LoadOptionalMarker(CampaignLockedMarkerPath);
            Sprite campaignAvailableMarker = LoadOptionalMarker(CampaignAvailableMarkerPath);
            Sprite campaignCompletedMarker = LoadOptionalMarker(CampaignCompletedMarkerPath);
            Sprite campaignBossMarker = LoadOptionalMarker(CampaignBossMarkerPath);
            Sprite campaignGlow = LoadOptionalMarker(CampaignGlowPath);
            Sprite campaignPlatform = LoadOptionalMarker(CampaignPlatformPath);
            Sprite campaignBackButton = LoadOptionalMarker(CampaignBackButtonPath);
            Sprite campaignRegionHeader = LoadOptionalMarker(CampaignRegionHeaderPath);
            Sprite campaignTooltipFrame = LoadOptionalMarker(CampaignTooltipFramePath);
            Sprite developmentBossPortrait = LoadBossSprite(DevelopmentBossSpritePath);
            EnsureDevelopmentBossData(developmentBossPortrait);

            RegionData[] regions = new RegionData[RegionIds.Length];
            for (int index = 0; index < regions.Length; index++)
            {
                string assetPath = $"{RegionDataFolder}/{RegionIds[index]}.asset";
                RegionData region = AssetDatabase.LoadAssetAtPath<RegionData>(assetPath);
                Sprite map = LoadMapSprite(MapPaths[index]);
                if (map == null)
                    return null;
                Sprite header = index == 0 ? LoadMapSprite(Region01FooterPath) : null;
                Sprite footer = index == 0 ? LoadMapSprite(Region01HeaderPath) : null;

                if (region == null)
                    region = ScriptableObject.CreateInstance<RegionData>();

                StageData[] stages = CreateStages(index, region.Stages, developmentBossPortrait);
                string nextId = index + 1 < RegionIds.Length ? RegionIds[index + 1] : string.Empty;
                region.Configure(RegionIds[index], RegionNames[index], map, WorldNodePositions[index],
                    stages, nextId, RegionColors[index], index == RegionIds.Length - 1,
                    header, footer);
                if (AssetDatabase.GetAssetPath(region) == string.Empty)
                    AssetDatabase.CreateAsset(region, assetPath);
                EditorUtility.SetDirty(region);
                regions[index] = region;
            }

            CampaignCatalog catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CampaignCatalog>();
                catalog.Configure(worldMap, regions, lockedMarker, availableMarker,
                    completedMarker, bossMarker, campaignLockedMarker, campaignAvailableMarker,
                    campaignCompletedMarker, campaignBossMarker, campaignGlow, campaignPlatform,
                    campaignBackButton, campaignRegionHeader, campaignTooltipFrame);
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            else
            {
                catalog.Configure(worldMap, regions, lockedMarker, availableMarker,
                    completedMarker, bossMarker, campaignLockedMarker, campaignAvailableMarker,
                    campaignCompletedMarker, campaignBossMarker, campaignGlow, campaignPlatform,
                    campaignBackButton, campaignRegionHeader, campaignTooltipFrame);
                EditorUtility.SetDirty(catalog);
            }
            AssetDatabase.SaveAssets();
            ValidateBossDataReferences(catalog);
            return catalog;
        }

        static void ValidateBossDataReferences(CampaignCatalog campaignCatalog)
        {
            int stageCount = 0;
            HashSet<string> bossIds = new HashSet<string>();
            foreach (RegionData region in campaignCatalog.Regions)
            {
                if (region == null)
                {
                    Debug.LogError("Campaign catalog contains a missing Region while validating BossData.");
                    continue;
                }

                StageData previousStage = null;
                foreach (StageData stage in region.Stages)
                {
                    if (stage == null || stage.BossData == null)
                    {
                        Debug.LogError($"Campaign stage has no BossData: {region.RegionId}");
                        continue;
                    }

                    stageCount++;
                    if (string.IsNullOrWhiteSpace(stage.BossData.BossId)
                        || !bossIds.Add(stage.BossData.BossId))
                        Debug.LogError($"Campaign stage BossData has an empty or duplicate BossId: {stage.BossData.BossId}");
                    if (stage.BossData.Portrait == null
                        || AssetDatabase.LoadAssetAtPath<Sprite>(
                            AssetDatabase.GetAssetPath(stage.BossData.Portrait)) == null)
                        Debug.LogError($"Campaign BossData portrait did not resolve to a Sprite: {stage.BossData.BossId}");
                    if (stage.IsBossStage && previousStage != null
                        && stage.BossData == previousStage.BossData)
                        Debug.LogError($"Region Boss stage must have a dedicated BossData asset: {stage.StageId}");
                    previousStage = stage;
                }
            }

            if (stageCount != RegionIds.Length * 10)
                Debug.LogError($"Expected {RegionIds.Length * 10} configured campaign stages, found {stageCount}.");
            else
                Debug.Log($"Validated {stageCount} StageData-to-BossData references and Boss portraits.");
        }

        static StageData[] CreateStages(int regionIndex, IReadOnlyList<StageData> existingStages,
            Sprite developmentBossPortrait)
        {
            StageData[] stages = new StageData[10];
            for (int stageIndex = 0; stageIndex < stages.Length; stageIndex++)
            {
                int stageNumber = stageIndex + 1;
                StageData existing = existingStages != null && stageIndex < existingStages.Count
                    ? existingStages[stageIndex]
                    : null;
                BossData bossData = existing != null ? existing.BossData : null;
                if (bossData == null)
                    bossData = CreateStageBossData(regionIndex, stageNumber, developmentBossPortrait);
                stages[stageIndex] = new StageData();
                stages[stageIndex].Configure(
                    $"{RegionIds[regionIndex]}_stage_{stageNumber:00}",
                    stageNumber,
                    $"Stage {stageNumber}",
                    RegionIds[regionIndex],
                    stageNumber == 10,
                    StageNodePositions[regionIndex][stageIndex],
                    bossData);
            }
            return stages;
        }

        static BossData CreateStageBossData(int regionIndex, int stageNumber,
            Sprite developmentBossPortrait)
        {
            Sprite portrait = developmentBossPortrait;
            string displayName;
            string[] regionSprites = BossSpritePaths[regionIndex];
            if (regionSprites.Length > 0)
            {
                int spriteIndex = stageNumber == 10
                    ? regionSprites.Length - 1
                    : (stageNumber - 1) % (regionSprites.Length - 1);
                portrait = LoadBossSprite(regionSprites[spriteIndex]);
                if (portrait == null)
                {
                    Debug.LogWarning($"Boss sprite unavailable for {RegionIds[regionIndex]} Stage {stageNumber}; using the development Boss portrait.");
                    portrait = developmentBossPortrait;
                }
                displayName = BossNames[regionIndex][spriteIndex];
            }
            else
            {
                displayName = stageNumber == 10
                    ? BossNames[regionIndex][1]
                    : BossNames[regionIndex][0];
            }

            string bossId = $"{RegionIds[regionIndex]}_boss_{stageNumber:00}";
            string assetPath = $"{BossDataFolder}/{bossId}.asset";
            BossData bossData = AssetDatabase.LoadAssetAtPath<BossData>(assetPath);
            if (bossData != null)
                return bossData;

            int maxHp = 1000 + regionIndex * 150 + (stageNumber - 1) * 75;
            int attack = 20 + regionIndex * 2 + stageNumber - 1;
            BossStatusEffectCapability capabilities = GetRegionStatusCapability(regionIndex);
            bossData = ScriptableObject.CreateInstance<BossData>();
            bossData.Configure(bossId, displayName, portrait, maxHp, attack, 100, 0,
                5, 1, true, 2f, capabilities);
            AssetDatabase.CreateAsset(bossData, assetPath);
            EditorUtility.SetDirty(bossData);
            return bossData;
        }

        static BossData EnsureDevelopmentBossData(Sprite developmentBossPortrait)
        {
            BossData bossData = AssetDatabase.LoadAssetAtPath<BossData>(DevelopmentBossDataPath);
            if (bossData != null)
                return bossData;

            bossData = ScriptableObject.CreateInstance<BossData>();
            bossData.Configure("development_boss", "Development Boss",
                developmentBossPortrait, 1000, 20, 100, 0, 5, 1, true, 2f,
                BossStatusEffectCapability.None);
            AssetDatabase.CreateAsset(bossData, DevelopmentBossDataPath);
            EditorUtility.SetDirty(bossData);
            return bossData;
        }

        static BossStatusEffectCapability GetRegionStatusCapability(int regionIndex)
        {
            switch (regionIndex)
            {
                case 0: return BossStatusEffectCapability.Poison;
                case 1: return BossStatusEffectCapability.Freeze;
                case 2: return BossStatusEffectCapability.Burn;
                case 3: return BossStatusEffectCapability.Stun;
                default: return BossStatusEffectCapability.Weaken;
            }
        }

        static Sprite LoadBossSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Object[] importedAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (Object importedAsset in importedAssets)
                    if (importedAsset is Sprite importedSprite)
                    {
                        sprite = importedSprite;
                        break;
                    }
            }
            if (sprite == null)
                Debug.LogWarning($"Boss artwork is missing or did not import as a Sprite: {assetPath}");
            return sprite;
        }

        static Sprite LoadMapSprite(string assetPath)
        {
            return LoadSprite(assetPath, true);
        }

        static Sprite LoadOptionalMarker(string assetPath)
        {
            return LoadSprite(assetPath, false);
        }

        static bool CampaignUiReferencesMissing()
        {
            CampaignCatalog catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            return catalog == null || catalog.CampaignLockedStageMarker == null
                || catalog.CampaignAvailableStageMarker == null
                || catalog.CampaignCompletedStageMarker == null
                || catalog.CampaignBossStageMarker == null
                || catalog.CampaignStageGlow == null
                || catalog.CampaignStagePlatform == null
                || catalog.CampaignBackButton == null
                || catalog.CampaignRegionHeader == null
                || catalog.CampaignTooltipFrame == null;
        }

        static bool CampaignBossDataMissing()
        {
            if (!File.Exists(DevelopmentBossDataPath))
                return true;
            CampaignCatalog catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            if (catalog == null)
                return true;
            foreach (StageData stage in catalog.EnumerateStages())
                if (stage.BossData == null)
                    return true;
            return false;
        }

        static void EnsureCampaignUiImportSettings()
        {
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D",
                new[] { CampaignUiPath.TrimEnd('/') });
            int spriteCount = 0;
            foreach (string guid in textureGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)
                    || assetPath.StartsWith(CampaignReferencePath, System.StringComparison.Ordinal))
                    continue;

                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogError($"Campaign UI texture has no TextureImporter: {assetPath}");
                    continue;
                }
                if (ConfigureSpriteImporter(importer))
                    importer.SaveAndReimport();

                importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || !importer.alphaIsTransparency
                    || importer.filterMode != FilterMode.Bilinear
                    || importer.wrapMode != TextureWrapMode.Clamp
                    || importer.mipmapEnabled)
                {
                    Debug.LogError($"Campaign UI texture import settings are invalid: {assetPath}");
                    continue;
                }

                if (AssetDatabase.LoadAssetAtPath<Sprite>(assetPath) == null)
                {
                    Debug.LogError($"Campaign UI texture did not produce a Sprite: {assetPath}");
                    continue;
                }
                spriteCount++;
            }

            Debug.Log($"Validated {spriteCount} Campaign UI sprites (Reference excluded).");
        }

        static bool ConfigureSpriteImporter(TextureImporter importer)
        {
            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !importer.alphaIsTransparency
                || importer.filterMode != FilterMode.Bilinear
                || importer.wrapMode != TextureWrapMode.Clamp
                || importer.mipmapEnabled;
            if (!changed)
                return false;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            return true;
        }

        static Sprite LoadSprite(string assetPath, bool required)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                if (required)
                    Debug.LogError($"Campaign map texture was not imported: {assetPath}");
                else
                    Debug.LogWarning($"Optional campaign stage marker is missing; runtime placeholder will be used: {assetPath}");
                return null;
            }

            if (ConfigureSpriteImporter(importer))
                importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                if (required)
                    Debug.LogError($"Campaign map texture did not produce a Sprite: {assetPath}");
                else
                    Debug.LogWarning($"Optional campaign stage marker did not produce a Sprite; runtime placeholder will be used: {assetPath}");
            }
            return sprite;
        }

        static void EnsureMapScene(string path, string rootName, CampaignCatalog catalog, bool worldMap)
        {
            if (File.Exists(path))
                return;

            Scene activeScene = SceneManager.GetActiveScene();
            NewSceneMode mode;
            if (activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.path))
                mode = NewSceneMode.Additive;
            else if (Application.isBatchMode)
                mode = NewSceneMode.Single;
            else
            {
                Debug.LogError($"Save the active scene before generating {rootName}.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            GameObject root = new GameObject(rootName);
            if (worldMap)
                root.AddComponent<WorldMapController>().Configure(catalog);
            else
                root.AddComponent<RegionMapController>().Configure(catalog);
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            SceneManager.MoveGameObjectToScene(root, scene);
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
        }

        static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(WorldScenePath, true),
                new EditorBuildSettingsScene(RegionScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
            };

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path == WorldScenePath || existing.path == RegionScenePath
                    || existing.path == "Assets/Scenes/Game.unity")
                    continue;
                scenes.Add(existing);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static bool CharacterContentMissing()
        {
            HeroData hero = AssetDatabase.LoadAssetAtPath<HeroData>(
                "Assets/Resources/Characters/Heroes/Hero2.asset");
            BossData boss02 = AssetDatabase.LoadAssetAtPath<BossData>(
                "Assets/Resources/Campaign/Bosses/region_01_boss_02.asset");
            BossData boss01 = AssetDatabase.LoadAssetAtPath<BossData>(
                "Assets/Resources/Campaign/Bosses/region_01_boss_01.asset");
            return hero == null || hero.AnimationSet == null
                || boss01 == null || boss01.AnimationSet == null
                || boss02 == null || boss02.AnimationSet == null;
        }

        static void EnsureFolder(string parent, string folderName)
        {
            string path = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
