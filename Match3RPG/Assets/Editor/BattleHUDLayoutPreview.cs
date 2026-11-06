using System;
using System.Collections.Generic;
using System.Linq;
using Match3RPG.Configuration;
using Match3RPG.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Match3RPG.Editor
{
    public static class BattleHUDLayoutPreview
    {
        const string ConfigAssetPath = "Assets/Resources/Configuration/BattleHUDLayoutConfig.asset";
        const string ConfigResourcePath = "Configuration/BattleHUDLayoutConfig";
        const string MenuRoot = "Match3RPG/Battle HUD/";

        [MenuItem(MenuRoot + "Build Preview")]
        public static void BuildPreview()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Battle HUD preview can only be built while not in Play Mode.");
                return;
            }

            BattleHUDPreviewMarker.CleanupScenePreview();
            var config = ResolveConfig();
            var scene = SceneManager.GetActiveScene();

            var existingLayer = GetPreviewLayer();
            Canvas canvas;
            RectTransform contentRoot;
            RectTransform battleHudLayer;
            if (existingLayer != null)
            {
                battleHudLayer = existingLayer as RectTransform;
                canvas = battleHudLayer.GetComponentInParent<Canvas>();
                if (battleHudLayer.GetComponent<BattleHUDPreviewMarker>() == null)
                    battleHudLayer.gameObject.AddComponent<BattleHUDPreviewMarker>();
            }
            else
            {
                canvas = EnsureCanvas();
                contentRoot = EnsureRect(canvas.transform, "BattleContentRoot");
                battleHudLayer = EnsureRect(contentRoot, "BattleHUDLayer");
                canvas.gameObject.AddComponent<BattleHUDPreviewMarker>();
            }

            var values = config != null ? config.GetValues() : BattleHUDLayoutConfig.DefaultValues;
            var warnings = BuildFallbackWarnings(values);
            if (warnings.Count > 0)
            {
                Debug.LogWarning("Battle HUD preview fallback values applied: " + string.Join("; ", warnings));
            }

            var heroHp = CreatePreviewBar(battleHudLayer, "HeroHPBar", values.HeroHpBarPosition,
                values.HeroHpBarSize, ResolveBarSprite("HeroHPBar"), 500, 500,
                values.HpValueStyle, values.HpFillInset);
            var heroSoul = CreatePreviewBar(battleHudLayer, values.HeroSoulHpChannel, "HeroSoulHPBar",
                ResolveBarSprite("HeroSoulHPBar"), 100, 100, values.RageValueStyle, values.RageFillInset);
            var heroRage = CreatePreviewBar(battleHudLayer, values.HeroRageChannel, "HeroRageBar",
                ResolveBarSprite("HeroRageBar"), 100, 100, values.RageValueStyle, values.RageFillInset);
            var bossHp = CreatePreviewBar(battleHudLayer, "BossHPBar",
                new Vector2(-values.HeroHpBarPosition.x, values.HeroHpBarPosition.y),
                values.HeroHpBarSize, ResolveBarSprite("BossHPBar"), 1000, 1000,
                values.HpValueStyle, values.HpFillInset);
            var bossSoul = CreatePreviewBar(battleHudLayer, values.BossSoulHpChannel, "BossSoulHPBar",
                ResolveBarSprite("BossSoulHPBar"), 100, 100, values.RageValueStyle, values.RageFillInset);
            var bossRage = CreatePreviewBar(battleHudLayer, values.BossRageChannel, "BossRageBar",
                ResolveBarSprite("BossRageBar"), 100, 100, values.RageValueStyle, values.RageFillInset);

            heroHp.SetAsLastSibling();
            heroSoul.SetAsLastSibling();
            heroRage.SetAsLastSibling();
            bossHp.SetAsLastSibling();
            bossSoul.SetAsLastSibling();
            bossRage.SetAsLastSibling();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Battle HUD preview built in scene '" + scene.name + "'.");
        }

        [MenuItem(MenuRoot + "Clear Preview")]
        public static void ClearPreview()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Battle HUD preview can only be cleared while not in Play Mode.");
                return;
            }

            var markers = UnityEngine.Object.FindObjectsByType<BattleHUDPreviewMarker>(FindObjectsSortMode.None);
            if (markers == null || markers.Length == 0)
            {
                Debug.Log("No Battle HUD preview objects found in the active scene.");
                return;
            }

            foreach (var marker in markers)
            {
                if (marker == null)
                    continue;

                GameObject root = marker.gameObject;
                if (root == null)
                    continue;

                if (root.name == "Canvas")
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    continue;
                }

                if (root.name == "BattleHUDLayer")
                {
                    ClearPreviewChildren(root.transform);
                    continue;
                }

                Transform previewRoot = root.transform.parent;
                if (previewRoot != null && previewRoot.name == "BattleHUDLayer")
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Battle HUD preview cleared from scene.");
        }

        [MenuItem(MenuRoot + "Save Preview Layout to Config")]
        public static void SavePreviewLayoutToConfig()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Battle HUD preview layout can only be saved while not in Play Mode.");
                return;
            }

            var config = ResolveConfig();
            if (config == null)
            {
                Debug.LogError("BattleHUDLayoutConfig.asset could not be found. Expected at 'Assets/Resources/Configuration/BattleHUDLayoutConfig.asset'.");
                return;
            }

            var layer = GetPreviewLayer();
            if (layer == null)
            {
                Debug.LogError("No Battle HUD preview was found. Use 'Match3RPG > Battle HUD > Build Preview' first.");
                return;
            }

            var heroHp = GetBarRect(layer, "HeroHPBar");
            var heroSoul = GetBarRect(layer, "HeroSoulHPBar");
            var heroRage = GetBarRect(layer, "HeroRageBar");
            var bossHp = GetBarRect(layer, "BossHPBar");
            if (heroHp == null || heroSoul == null || heroRage == null || bossHp == null)
            {
                Debug.LogError("The preview is incomplete: missing one or more track bars (HeroHPBar, HeroSoulHPBar, HeroRageBar, BossHPBar).");
                return;
            }

            var previous = config.GetValues();
            var heroPos = heroHp.anchoredPosition;
            var heroSize = heroHp.sizeDelta;
            var bossPos = bossHp.anchoredPosition;
            var mirroredBossPos = new Vector2(-heroPos.x, heroPos.y);
            var heroSoulChannel = GetNormalizedRect(heroSoul);
            var heroRageChannel = GetNormalizedRect(heroRage);
            var bossOffset = bossPos - mirroredBossPos;
            var bossRageOffset = Vector2.zero;

            SerializedObject serialized = new SerializedObject(config);
            SetVector2(serialized, "heroHpBarPosition", heroPos);
            SetVector2(serialized, "heroHpBarSize", heroSize);
            SetRect(serialized, "heroSoulHpChannel", heroSoulChannel);
            SetRect(serialized, "heroRageChannel", heroRageChannel);
            SetVector2(serialized, "bossHpBarOffsetReferencePixels", bossOffset);
            SetVector2(serialized, "bossRageOffsetNormalized", bossRageOffset);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log("Saved Battle HUD preview layout to BattleHUDLayoutConfig.asset. " +
                "HeroHPBar: old position=" + previous.HeroHpBarPosition + " size=" + previous.HeroHpBarSize + " => new position=" + heroPos + " size=" + heroSize + ". " +
                "BossHPBar: old mirrored reference=" + previous.BossHpBarPosition + " => new boss offset=" + bossOffset + ". " +
                "HeroSoulHPBar: old channel=" + previous.HeroSoulHpChannel + " => new channel=" + heroSoulChannel + ". " +
                "HeroRageBar: old channel=" + previous.HeroRageChannel + " => new channel=" + heroRageChannel + ". " +
                "BossRageOffsetNormalized: old=" + previous.BossRageChannel + " => new offset=" + bossRageOffset + ".");
        }

        static Canvas EnsureCanvas()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (existing != null)
            {
                var contentRoot = existing.transform.Find("BattleContentRoot");
                var battleHudLayer = contentRoot != null ? contentRoot.Find("BattleHUDLayer") : null;
                if (battleHudLayer != null)
                    return existing;
            }

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            return canvas;
        }

        static RectTransform EnsureRect(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
                return child as RectTransform ?? child.gameObject.AddComponent<RectTransform>();

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            return rect;
        }

        static BattleHUDLayoutConfig ResolveConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<BattleHUDLayoutConfig>(ConfigAssetPath);
            return config ?? Resources.Load<BattleHUDLayoutConfig>(ConfigResourcePath);
        }

        static List<string> BuildFallbackWarnings(BattleHUDLayoutValues values)
        {
            var warnings = new List<string>();
            AppendValueWarning(values.HeroHpChannel, BattleHUDLayoutConfig.DefaultValues.HeroHpChannel, warnings, "heroHpChannel");
            AppendValueWarning(values.HeroSoulHpChannel, BattleHUDLayoutConfig.DefaultValues.HeroSoulHpChannel, warnings, "heroSoulHpChannel");
            AppendValueWarning(values.HeroRageChannel, BattleHUDLayoutConfig.DefaultValues.HeroRageChannel, warnings, "heroRageChannel");
            AppendValueWarning(values.BossHpChannel, BattleHUDLayoutConfig.DefaultValues.BossHpChannel, warnings, "bossHpChannel");
            AppendValueWarning(values.BossSoulHpChannel, BattleHUDLayoutConfig.DefaultValues.BossSoulHpChannel, warnings, "bossSoulHpChannel");
            AppendValueWarning(values.BossRageChannel, BattleHUDLayoutConfig.DefaultValues.BossRageChannel, warnings, "bossRageChannel");
            return warnings;
        }

        static void AppendValueWarning(Rect current, Rect fallback, List<string> warnings, string fieldName)
        {
            if (current.width <= 0f || current.height <= 0f)
                warnings.Add(fieldName + " was zero, using " + fallback);
        }

        static Transform GetPreviewLayer()
        {
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                return null;

            var contentRoot = canvas.transform.Find("BattleContentRoot");
            if (contentRoot == null)
                return null;

            var layer = contentRoot.Find("BattleHUDLayer");
            if (layer == null)
                return null;

            return layer;
        }

        static RectTransform GetBarRect(Transform layer, string name)
        {
            if (layer == null)
                return null;

            var child = layer.Find(name);
            return child as RectTransform;
        }

        static Rect GetNormalizedRect(RectTransform barRect)
        {
            if (barRect == null)
                return default;

            var parent = barRect.parent as RectTransform;
            if (parent == null)
                return new Rect(barRect.anchorMin.x, barRect.anchorMin.y, barRect.anchorMax.x - barRect.anchorMin.x, barRect.anchorMax.y - barRect.anchorMin.y);

            var minX = Mathf.Clamp(barRect.anchorMin.x, 0f, 1f);
            var minY = Mathf.Clamp(barRect.anchorMin.y, 0f, 1f);
            var maxX = Mathf.Clamp(barRect.anchorMax.x, 0f, 1f);
            var maxY = Mathf.Clamp(barRect.anchorMax.y, 0f, 1f);
            return new Rect(minX, minY, Mathf.Max(0f, maxX - minX), Mathf.Max(0f, maxY - minY));
        }

        static void SetRect(SerializedObject serialized, string propertyName, Rect value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
                return;

            property.FindPropertyRelative("x").floatValue = value.x;
            property.FindPropertyRelative("y").floatValue = value.y;
            property.FindPropertyRelative("width").floatValue = value.width;
            property.FindPropertyRelative("height").floatValue = value.height;
        }

        static void SetVector2(SerializedObject serialized, string propertyName, Vector2 value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
                return;

            property.vector2Value = value;
        }

        static void ClearPreviewChildren(Transform previewLayer)
        {
            if (previewLayer == null)
                return;

            for (int i = previewLayer.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(previewLayer.GetChild(i).gameObject);
            }
        }

        static Transform CreatePreviewBar(Transform parent, string name, Vector2 anchoredPosition, Vector2 size,
            Sprite sprite, int value, int max, BattleHUDTextStyle style, float fillInset)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            var track = go.AddComponent<Image>();
            track.sprite = sprite;
            track.type = Image.Type.Sliced;
            track.fillCenter = true;
            track.color = new Color32(42, 46, 61, 255);
            track.raycastTarget = false;

            var slider = go.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.targetGraphic = track;
            slider.interactable = false;
            slider.minValue = 0;
            slider.maxValue = max;
            slider.value = value;
            slider.direction = name.Contains("Boss") ? Slider.Direction.RightToLeft : Slider.Direction.LeftToRight;

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.one * fillInset;
            fillAreaRect.offsetMax = Vector2.one * -fillInset;
            fillAreaRect.localScale = Vector3.one;
            var mask = fillArea.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;
            mask.softness = Vector2Int.zero;

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.localScale = Vector3.one;

            var fillImage = fill.AddComponent<Image>();
            fillImage.sprite = sprite;
            fillImage.type = Image.Type.Sliced;
            fillImage.fillCenter = true;
            fillImage.color = new Color(1f, 1f, 1f, 1f);
            fillImage.raycastTarget = false;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = name.Contains("Boss") ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
            slider.fillRect = fillRect;
            slider.targetGraphic = track;

            var valueContainer = new GameObject("Value", typeof(RectTransform));
            valueContainer.transform.SetParent(go.transform, false);
            var valueRect = valueContainer.GetComponent<RectTransform>();
            valueRect.anchorMin = Vector2.zero;
            valueRect.anchorMax = Vector2.one;
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
            valueRect.localScale = Vector3.one;

            var valueText = valueContainer.AddComponent<TextMeshProUGUI>();
            valueText.text = value + " / " + max;
            valueText.raycastTarget = false;
            valueText.alignment = style.Alignment;
            valueText.fontSize = style.FontSize;
            valueText.enableAutoSizing = style.AutoSize;
            valueText.fontSizeMin = style.AutoSizeMin;
            valueText.fontSizeMax = style.AutoSizeMax;
            valueText.color = style.Color;
            valueText.outlineColor = style.OutlineColor;
            valueText.outlineWidth = style.OutlineWidth;
            valueText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            valueText.verticalAlignment = VerticalAlignmentOptions.Middle;
            valueText.enableWordWrapping = false;
            valueText.overflowMode = TextOverflowModes.Overflow;

            return rect;
        }

        static Transform CreatePreviewBar(Transform parent, Rect normalizedChannel, string name, Sprite sprite,
            int value, int max, BattleHUDTextStyle style, float fillInset)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = normalizedChannel.min;
            rect.anchorMax = normalizedChannel.max;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            var track = go.AddComponent<Image>();
            track.sprite = sprite;
            track.type = Image.Type.Sliced;
            track.fillCenter = true;
            track.color = new Color32(42, 46, 61, 255);
            track.raycastTarget = false;

            var slider = go.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.targetGraphic = track;
            slider.interactable = false;
            slider.minValue = 0;
            slider.maxValue = max;
            slider.value = value;
            slider.direction = name.Contains("Boss") ? Slider.Direction.RightToLeft : Slider.Direction.LeftToRight;

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.one * fillInset;
            fillAreaRect.offsetMax = Vector2.one * -fillInset;
            fillAreaRect.localScale = Vector3.one;
            fillArea.AddComponent<RectMask2D>();

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.sprite = sprite;
            fillImage.type = Image.Type.Sliced;
            fillImage.fillCenter = true;
            fillImage.color = Color.white;
            fillImage.raycastTarget = false;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = name.Contains("Boss") ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
            slider.fillRect = fillRect;

            var valueContainer = new GameObject("Value", typeof(RectTransform));
            valueContainer.transform.SetParent(go.transform, false);
            var valueRect = valueContainer.GetComponent<RectTransform>();
            valueRect.anchorMin = Vector2.zero;
            valueRect.anchorMax = Vector2.one;
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
            var valueText = valueContainer.AddComponent<TextMeshProUGUI>();
            valueText.text = value + " / " + max;
            valueText.raycastTarget = false;
            valueText.alignment = style.Alignment;
            valueText.fontSize = style.FontSize;
            valueText.enableAutoSizing = style.AutoSize;
            valueText.fontSizeMin = style.AutoSizeMin;
            valueText.fontSizeMax = style.AutoSizeMax;
            valueText.color = style.Color;
            valueText.outlineColor = style.OutlineColor;
            valueText.outlineWidth = style.OutlineWidth;
            valueText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            valueText.verticalAlignment = VerticalAlignmentOptions.Middle;
            valueText.enableWordWrapping = false;
            valueText.overflowMode = TextOverflowModes.Overflow;
            return rect;
        }

        static Sprite ResolveBarSprite(string barName)
        {
            string assetPath = barName switch
            {
                "HeroHPBar" => "Assets/Art/Battle/BarHP/Bar_HPNormal_Hero.png",
                "BossHPBar" => "Assets/Art/Battle/BarHP/Bar_HPNormal_Boss.png",
                "HeroSoulHPBar" => "Assets/Art/Battle/BarHP/Bar_HPSoul.png",
                "BossSoulHPBar" => "Assets/Art/Battle/BarHP/Bar_HPSoul.png",
                "HeroRageBar" => "Assets/Art/Battle/BarHP/Bar_Rage.png",
                "BossRageBar" => "Assets/Art/Battle/BarHP/Bar_Rage.png",
                _ => null
            };

            if (!string.IsNullOrEmpty(assetPath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                    return sprite;
            }

            string resourceName = barName switch
            {
                "HeroHPBar" => "Battle/BarHP/Bar_HPNormal_Hero",
                "BossHPBar" => "Battle/BarHP/Bar_HPNormal_Boss",
                "HeroSoulHPBar" => "Battle/BarHP/Bar_HPSoul",
                "BossSoulHPBar" => "Battle/BarHP/Bar_HPSoul",
                "HeroRageBar" => "Battle/BarHP/Bar_Rage",
                "BossRageBar" => "Battle/BarHP/Bar_Rage",
                _ => null
            };

            return string.IsNullOrEmpty(resourceName) ? null : Resources.Load<Sprite>(resourceName);
        }
    }
}
