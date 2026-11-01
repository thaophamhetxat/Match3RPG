using System;
using System.Collections.Generic;
using Match3RPG.Campaign;
using Match3RPG.Campaign.Data;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Match3RPG.Editor
{
    static class RegionMapPrefabTools
    {
        const string CatalogPath = "Assets/Resources/Campaign/CampaignCatalog.asset";
        const string OutputFolder = "Assets/Prefabs/RegionMaps";
        const string RegionMapScenePath = "Assets/Scenes/RegionMap.unity";

        [MenuItem("Tools/Campaign/Bake Region Map Layouts")]
        public static void BakeRegionMapLayouts()
        {
            CampaignCatalog catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            if (catalog == null)
                throw new InvalidOperationException($"Campaign catalog missing: {CatalogPath}");

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/Prefabs", "RegionMaps");
            float touchTarget = ReadStageTouchTarget();
            int bakedCount = 0;
            foreach (RegionData region in catalog.Regions)
            {
                if (region == null || region.MapSprite == null || region.Stages.Count == 0)
                    continue;

                string outputPath = $"{OutputFolder}/Region{GetRegionNumber(region)}_MapLayout.prefab";
                GameObject prefab = CreateRegionMapPrefab(region, catalog, touchTarget);
                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefab, outputPath);
                UnityEngine.Object.DestroyImmediate(prefab);
                if (savedPrefab == null)
                    throw new InvalidOperationException($"Failed to create region map prefab: {outputPath}");

                SerializedObject regionObject = new SerializedObject(region);
                regionObject.FindProperty("mapLayoutPrefab").objectReferenceValue = savedPrefab;
                regionObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(region);
                bakedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Baked {bakedCount} region map layout prefab(s) into {OutputFolder}.");
            ValidateRegionMapLayouts();
        }

        [MenuItem("Tools/Campaign/Validate Region Map Layouts")]
        public static void ValidateRegionMapLayouts()
        {
            CampaignCatalog catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            if (catalog == null)
                throw new InvalidOperationException($"Campaign catalog missing: {CatalogPath}");

            int errorCount = 0;
            foreach (RegionData region in catalog.Regions)
            {
                if (region == null || region.MapSprite == null || region.Stages.Count == 0)
                    continue;
                GameObject prefab = region.MapLayoutPrefab;
                if (prefab == null)
                {
                    Debug.LogError($"Region {region.RegionId} has no MapLayoutPrefab.", region);
                    errorCount++;
                    continue;
                }

                StageMarkerView[] markers = prefab.GetComponentsInChildren<StageMarkerView>(true);
                Dictionary<string, int> markerCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (StageMarkerView marker in markers)
                {
                    if (string.IsNullOrEmpty(marker.StageId))
                    {
                        Debug.LogError($"Region {region.RegionId} has a marker with no StageId.", prefab);
                        errorCount++;
                        continue;
                    }
                    markerCounts.TryGetValue(marker.StageId, out int count);
                    markerCounts[marker.StageId] = count + 1;
                }

                foreach (StageData stage in region.Stages)
                {
                    if (stage == null)
                        continue;
                    markerCounts.TryGetValue(stage.StageId, out int count);
                    if (count == 0)
                    {
                        Debug.LogError($"Region {region.RegionId} prefab is missing StageId '{stage.StageId}'.",
                            prefab);
                        errorCount++;
                    }
                    else if (count != 1)
                    {
                        Debug.LogError($"Region {region.RegionId} prefab has {count} markers for StageId '{stage.StageId}'.",
                            prefab);
                        errorCount++;
                    }
                    markerCounts.Remove(stage.StageId);
                }

                foreach (KeyValuePair<string, int> extra in markerCounts)
                {
                    Debug.LogError($"Region {region.RegionId} prefab has duplicate or unexpected StageId '{extra.Key}'.",
                        prefab);
                    errorCount++;
                }
            }

            if (errorCount == 0)
                Debug.Log("Region map layout validation passed.");
            else
                Debug.LogError($"Region map layout validation found {errorCount} error(s).");
        }

        static GameObject CreateRegionMapPrefab(RegionData region, CampaignCatalog catalog, float touchTarget)
        {
            GameObject root = new GameObject("MapContent", typeof(RectTransform), typeof(Canvas));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(.5f, 1f);
            rootRect.sizeDelta = new Vector2(region.MapSprite.rect.width, region.MapSprite.rect.height);

            RectTransform backgroundRect = CreateRect("RegionBackground", rootRect,
                new Vector2(.5f, 1f), new Vector2(.5f, 1f));
            backgroundRect.pivot = new Vector2(.5f, 1f);
            backgroundRect.sizeDelta = new Vector2(region.MapSprite.rect.width, region.MapSprite.rect.height);
            Image background = backgroundRect.gameObject.AddComponent<Image>();
            background.sprite = region.MapSprite;
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
            background.raycastTarget = false;

            RectTransform stageLayer = CreateRect("StagePathLayer", backgroundRect, Vector2.zero, Vector2.one);
            foreach (StageData stage in region.Stages)
            {
                if (stage == null)
                    continue;
                RectTransform nodeRect = CreateRect($"Stage{stage.StageNumber:00}", stageLayer,
                    new Vector2(stage.RegionMapNodePosition.x, 1f - stage.RegionMapNodePosition.y),
                    new Vector2(stage.RegionMapNodePosition.x, 1f - stage.RegionMapNodePosition.y));
                float markerScale = stage.IsBossStage ? 1.25f : 1f;
                float size = stage.IsBossStage ? touchTarget + 8f : touchTarget;
                nodeRect.sizeDelta = new Vector2(size, size);

                Image hitImage = nodeRect.gameObject.AddComponent<Image>();
                hitImage.color = new Color(1f, 1f, 1f, .001f);
                hitImage.raycastTarget = true;
                Button button = nodeRect.gameObject.AddComponent<Button>();
                button.targetGraphic = hitImage;
                button.transition = Selectable.Transition.ColorTint;
                button.colors = new ColorBlock
                {
                    normalColor = hitImage.color,
                    highlightedColor = new Color(1f, 1f, 1f, .045f),
                    pressedColor = new Color(1f, 1f, 1f, .08f),
                    selectedColor = hitImage.color,
                    disabledColor = hitImage.color,
                    colorMultiplier = 1f,
                    fadeDuration = .1f
                };

                CreateMarkerImage(nodeRect, "Platform", new Vector2(118f * markerScale, 70f * markerScale),
                    catalog.CampaignStagePlatform);
                Image glow = CreateMarkerImage(nodeRect, "Glow",
                    new Vector2(104f * markerScale, 86f * markerScale), catalog.CampaignStageGlow);
                glow.gameObject.SetActive(false);
                RectTransform markerRect = CreateRect("MarkerImage", nodeRect,
                    new Vector2(.5f, .5f), new Vector2(.5f, .5f));
                markerRect.sizeDelta = new Vector2(72f * markerScale, 88f * markerScale);
                Image marker = markerRect.gameObject.AddComponent<Image>();
                marker.sprite = stage.IsBossStage && catalog.CampaignBossStageMarker != null
                    ? catalog.CampaignBossStageMarker : catalog.CampaignAvailableStageMarker;
                marker.preserveAspect = true;
                marker.raycastTarget = false;

                RectTransform stateIconRect = CreateRect("StateIcon", markerRect,
                    new Vector2(.14f, .72f), new Vector2(.14f, .72f));
                stateIconRect.sizeDelta = new Vector2(26f, 26f);
                TextMeshProUGUI stateIcon = stateIconRect.gameObject.AddComponent<TextMeshProUGUI>();
                stateIcon.font = TMP_Settings.defaultFontAsset;
                stateIcon.text = string.Empty;
                stateIcon.alignment = TextAlignmentOptions.Center;
                stateIcon.verticalAlignment = VerticalAlignmentOptions.Middle;
                stateIcon.raycastTarget = false;

                StageMarkerView view = nodeRect.gameObject.AddComponent<StageMarkerView>();
                view.SetStageId(stage.StageId);
            }

            return root;
        }

        static Image CreateMarkerImage(Transform parent, string name, Vector2 size, Sprite sprite)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = sprite != null;
            image.raycastTarget = false;
            return image;
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        static float ReadStageTouchTarget()
        {
            Scene scene = SceneManager.GetSceneByPath(RegionMapScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(RegionMapScenePath, OpenSceneMode.Additive);

            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    RegionMapController controller = root.GetComponentInChildren<RegionMapController>(true);
                    if (controller == null)
                        continue;
                    SerializedObject serializedController = new SerializedObject(controller);
                    return serializedController.FindProperty("stageTouchTarget").floatValue;
                }
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }

            throw new InvalidOperationException("RegionMapController was not found in RegionMap.unity.");
        }

        static string GetRegionNumber(RegionData region)
        {
            string id = region.RegionId;
            int separator = id.LastIndexOf('_');
            return separator >= 0 && separator + 1 < id.Length ? id.Substring(separator + 1) : id;
        }
    }
}