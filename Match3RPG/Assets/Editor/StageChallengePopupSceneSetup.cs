using Match3RPG.Campaign;
using Match3RPG.Configuration;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Match3RPG.Editor
{
    static class StageChallengePopupSceneSetup
    {
        const string ScenePath = "Assets/Scenes/RegionMap.unity";
        const string PopupSpritePath =
            "Assets/Art/UI/Campaign/StageInfo/StageChallengePopup.png";
        const string LayoutPath =
            "Assets/Resources/Configuration/StageChallengePopupLayout.asset";
        const string GameConfigPath =
            "Assets/Resources/Configuration/GameConfigService.asset";

        [InitializeOnLoadMethod]
        static void ScheduleMissingSceneSetup()
        {
            EditorApplication.delayCall += EnsureMissingSceneSetup;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += EnsureMissingSceneSetup;
        }

        [MenuItem("Tools/Campaign/Assign Stage Challenge Popup")]
        static void AssignAndSave()
        {
            ConfigureSpriteImporter(PopupSpritePath);
            Sprite popupSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PopupSpritePath);
            StageChallengePopupLayoutConfig layout =
                AssetDatabase.LoadAssetAtPath<StageChallengePopupLayoutConfig>(LayoutPath);
            GameConfigService gameConfig =
                AssetDatabase.LoadAssetAtPath<GameConfigService>(GameConfigPath);
            if (popupSprite == null || layout == null || gameConfig == null)
                throw new System.InvalidOperationException(
                    "Stage challenge popup art, reward icons, or layout config could not be loaded.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RegionMapController controller = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                controller = root.GetComponentInChildren<RegionMapController>(true);
                if (controller != null)
                    break;
            }
            if (controller == null)
                throw new System.InvalidOperationException(
                    "RegionMapController was not found in the RegionMap scene.");

            ApplySceneSetup(controller, scene, popupSprite, layout, gameConfig);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static void EnsureMissingSceneSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling)
                return;

            Sprite popupSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PopupSpritePath);
            StageChallengePopupLayoutConfig layout =
                AssetDatabase.LoadAssetAtPath<StageChallengePopupLayoutConfig>(LayoutPath);
            GameConfigService gameConfig =
                AssetDatabase.LoadAssetAtPath<GameConfigService>(GameConfigPath);
            if (popupSprite == null || layout == null || gameConfig == null)
            {
                Debug.LogError("Stage challenge popup scene setup could not load its art, layout, or configuration assets.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                RegionMapController controller = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    controller = root.GetComponentInChildren<RegionMapController>(true);
                    if (controller != null)
                        break;
                }
                if (controller == null)
                    throw new System.InvalidOperationException(
                        "RegionMapController was not found in the RegionMap scene.");

                var existing = new SerializedObject(controller);
                if (existing.FindProperty("regionMapCanvas").objectReferenceValue != null)
                    return;

                ApplySceneSetup(controller, scene, popupSprite, layout, gameConfig);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void ApplySceneSetup(RegionMapController controller, Scene scene,
            Sprite popupSprite, StageChallengePopupLayoutConfig layout,
            GameConfigService gameConfig)
        {
            var controllerObject = new SerializedObject(controller);
            controllerObject.FindProperty("stageChallengePopupSprite").objectReferenceValue = popupSprite;
            controllerObject.FindProperty("stageChallengeLayout").objectReferenceValue = layout;
            controllerObject.FindProperty("gameConfigService").objectReferenceValue = gameConfig;
            controllerObject.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            AssignSceneHierarchy(controller, scene, popupSprite, layout);
        }

        static void AssignSceneHierarchy(RegionMapController controller, Scene scene,
            Sprite popupSprite, StageChallengePopupLayoutConfig layout)
        {
            Canvas canvas = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                    break;
            }

            if (canvas == null)
                canvas = CreateCanvas(scene);
            EnsureEventSystem(scene);
            Transform safeArea = canvas.transform.Find("SafeArea");
            if (safeArea == null)
                safeArea = CreateSafeArea(canvas.transform);
            Transform stagePopup = safeArea.Find("StageInfoPopup");
            Transform overlay = canvas.transform.Find("StageInfoDimOverlay");
            if (stagePopup == null || overlay == null)
                CreateStagePopup(safeArea, canvas.transform, popupSprite, layout, out stagePopup, out overlay);

            Transform artwork = stagePopup.Find("PopupArtwork");
            Transform heroSlot = artwork.Find("HeroPreviewSlot");
            Transform bossSlot = artwork.Find("BossPreviewSlot");
            Button exit = stagePopup.Find("PopupArtwork/ExitHit").GetComponent<Button>();
            Button start = stagePopup.Find("PopupArtwork/StartHit").GetComponent<Button>();
            SerializedObject serialized = new SerializedObject(controller);
            SetObject(serialized, "regionMapCanvas", canvas);
            SetObject(serialized, "safeAreaRoot", safeArea.GetComponent<RectTransform>());
            SetObject(serialized, "stageInfoPopupRoot", stagePopup.GetComponent<RectTransform>());
            SetObject(serialized, "popupArtworkRoot", artwork.GetComponent<RectTransform>());
            SetObject(serialized, "popupArtworkImage", artwork.GetComponent<Image>());
            SetObject(serialized, "dimOverlayImage", overlay.GetComponent<Image>());
            SetObject(serialized, "heroPreviewImage", heroSlot.Find("HeroPreview").GetComponent<Image>());
            SetObject(serialized, "bossPreviewImage", bossSlot.Find("BossPreview").GetComponent<Image>());
            SetObject(serialized, "powerText", artwork.Find("CombatPower").GetComponent<TextMeshProUGUI>());
            SetObject(serialized, "popupEnemyName", artwork.Find("BossName").GetComponent<TextMeshProUGUI>());
            SetObject(serialized, "popupStageLabel", artwork.Find("StageLabel").GetComponent<TextMeshProUGUI>());
            SetObject(serialized, "rewardArea", artwork.Find("RewardContent").GetComponent<RectTransform>());
            SetObject(serialized, "exitHitButton", exit);
            SetObject(serialized, "startHitButton", start);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void SetObject(SerializedObject serialized, string field, Object value)
        {
            serialized.FindProperty(field).objectReferenceValue = value;
        }

        static Canvas CreateCanvas(Scene scene)
        {
            var canvasObject = new GameObject("RegionMapCanvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            return canvas;
        }

        static void EnsureEventSystem(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<EventSystem>(true) != null)
                    return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        static Transform CreateSafeArea(Transform canvas)
        {
            RectTransform rect = CreateRect("SafeArea", canvas, new Rect(0f, 0f, 1f, 1f));
            rect.gameObject.AddComponent<CampaignSafeArea>();
            return rect;
        }

        static void CreateStagePopup(Transform safeArea, Transform canvas, Sprite popupSprite,
            StageChallengePopupLayoutConfig layout, out Transform stagePopup, out Transform overlay)
        {
            RectTransform overlayRect = CreateRect("StageInfoDimOverlay", canvas,
                new Rect(0f, 0f, 1f, 1f));
            Canvas overlayCanvas = overlayRect.gameObject.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 5;
            overlayRect.gameObject.AddComponent<GraphicRaycaster>();
            Image overlayImage = overlayRect.gameObject.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, .65f);
            overlayImage.raycastTarget = true;
            overlayRect.gameObject.SetActive(false);
            overlay = overlayRect;

            RectTransform popupRect = CreateRect("StageInfoPopup", safeArea,
                new Rect(0f, 0f, 1f, 1f));
            Canvas popupCanvas = popupRect.gameObject.AddComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 10;
            popupRect.gameObject.AddComponent<GraphicRaycaster>();
            Image popupBlocker = popupRect.gameObject.AddComponent<Image>();
            popupBlocker.color = new Color(0f, 0f, 0f, 0f);
            popupBlocker.raycastTarget = true;
            popupRect.gameObject.SetActive(false);
            stagePopup = popupRect;

            float aspect = popupSprite.rect.width / popupSprite.rect.height;
            float width = Mathf.Min(1080f * layout.MaxWidthFraction,
                1920f * layout.MaxHeightFraction * aspect);
            float height = width / aspect;
            RectTransform artworkRect = CreateRect("PopupArtwork", popupRect,
                new Rect(.5f, .5f, 0f, 0f));
            artworkRect.sizeDelta = new Vector2(width, height);
            Image artworkImage = artworkRect.gameObject.AddComponent<Image>();
            artworkImage.sprite = popupSprite;
            artworkImage.color = Color.white;
            artworkImage.type = Image.Type.Simple;
            artworkImage.preserveAspect = true;
            artworkImage.raycastTarget = false;

            RectTransform heroSlot = CreateRect("HeroPreviewSlot", artworkRect, layout.HeroPreviewRect);
            CreatePreview(heroSlot, "HeroPreview");
            RectTransform bossSlot = CreateRect("BossPreviewSlot", artworkRect, layout.BossPreviewRect);
            CreatePreview(bossSlot, "BossPreview");
            CreateText(artworkRect, "CombatPower", layout.HeroPowerTextRect,
                layout.PowerFontSize, layout.TextColor, TextAlignmentOptions.MidlineLeft, layout);
            CreateText(artworkRect, "BossName", layout.BossNameTextRect,
                layout.BossNameFontSize, layout.TextColor, TextAlignmentOptions.MidlineLeft, layout);
            CreateText(artworkRect, "StageLabel", layout.StageLabelRect,
                layout.StageFontSize, layout.TextColor, TextAlignmentOptions.Center, layout);

            RectTransform rewards = CreateRect("RewardContent", artworkRect, layout.RewardAreaRect);
            HorizontalLayoutGroup group = rewards.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 8f;
            group.padding = new RectOffset(8, 8, 8, 8);
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            group.childScaleWidth = false;
            group.childScaleHeight = false;

            CreateHitButton(artworkRect, "ExitHit", new Rect(.075f, .132f, .425f, .094f));
            CreateHitButton(artworkRect, "StartHit", new Rect(.485f, .132f, .445f, .094f));
        }

        static RectTransform CreateRect(string name, Transform parent, Rect normalizedRect)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(normalizedRect.xMin, normalizedRect.yMin);
            rect.anchorMax = new Vector2(normalizedRect.xMax, normalizedRect.yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(.5f, .5f);
            return rect;
        }

        static void CreatePreview(Transform slot, string name)
        {
            RectTransform rect = CreateRect(name, slot, new Rect(0f, 0f, 1f, 1f));
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static void CreateText(Transform parent, string name, Rect rect, float fontSize,
            Color color, TextAlignmentOptions alignment, StageChallengePopupLayoutConfig layout)
        {
            RectTransform textRect = CreateRect(name, parent, rect);
            TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = Mathf.Clamp(fontSize, 14f, 40f);
            text.fontSizeMin = 14f;
            text.fontSizeMax = 40f;
            text.enableAutoSizing = true;
            text.alignment = alignment;
            text.color = color;
            text.outlineColor = layout.OutlineColor;
            text.outlineWidth = layout.OutlineWidth;
            text.raycastTarget = false;
        }

        static void CreateHitButton(Transform parent, string name, Rect rect)
        {
            RectTransform hitRect = CreateRect(name, parent, rect);
            Image image = hitRect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            Button button = hitRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
        }

        static void ConfigureSpriteImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"Texture importer not found: {path}");

            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !importer.alphaIsTransparency
                || importer.mipmapEnabled
                || importer.wrapMode != TextureWrapMode.Clamp;
            if (!changed)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }
}
