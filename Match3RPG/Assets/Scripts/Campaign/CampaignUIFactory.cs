using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Match3RPG.Campaign
{
    static class CampaignUIFactory
    {
        static Sprite fallbackMarkerSprite;

        public static Canvas CreateCanvas(string objectName)
        {
            Screen.orientation = ScreenOrientation.Portrait;
            GameObject canvasObject = new GameObject(objectName, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            InputSystemUIInputModule input = eventSystem.AddComponent<InputSystemUIInputModule>();
            input.AssignDefaultActions();
        }

        public static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject item = new GameObject(name, typeof(RectTransform));
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static Image CreateImage(Transform parent, string name, Color color, Sprite sprite = null,
            bool raycastTarget = false)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = sprite != null ? Color.white : color;
            image.sprite = sprite;
            image.preserveAspect = sprite != null;
            image.raycastTarget = raycastTarget;
            return image;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string value, int fontSize,
            Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one,
                new Vector2(6f, 4f), new Vector2(-6f, -4f));
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(10f, fontSize * .65f);
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;
            if (text.font == null)
                Debug.LogError("TextMesh Pro default font asset is not configured.", text);
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 size,
            Color normalColor, Color textColor, int fontSize)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, Vector2.zero);
            rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = normalColor;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = normalColor,
                highlightedColor = Color.Lerp(normalColor, Color.white, .18f),
                pressedColor = Color.Lerp(normalColor, Color.black, .18f),
                selectedColor = Color.Lerp(normalColor, Color.white, .12f),
                disabledColor = Color.Lerp(normalColor, Color.black, .55f),
                colorMultiplier = 1f,
                fadeDuration = .12f
            };
            CreateText(rect, "Label", label, fontSize, textColor);
            return button;
        }

        public static Button CreateStyledButton(Transform parent, string name, string label,
            Vector2 size, Color normalColor, Color textColor, Color borderColor, int fontSize)
        {
            Button button = CreateButton(parent, name, label, size, normalColor, textColor, fontSize);
            Outline outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2f, -2f);
            return button;
        }

        public static Button CreateImageButton(Transform parent, string name, Sprite sprite,
            Vector2 size, Color tint)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = tint;
            image.preserveAspect = true;
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(.92f, .98f, 1f, 1f),
                pressedColor = new Color(.72f, .82f, .9f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(.55f, .55f, .55f, 1f),
                colorMultiplier = 1f,
                fadeDuration = .12f
            };
            return button;
        }

        public static Image CreateCenteredImage(Transform parent, string name, Vector2 size,
            Sprite sprite, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = sprite != null;
            image.raycastTarget = false;
            return image;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Vector2 size,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        public static RectTransform CreateFullScreenRect(Transform parent, string name)
        {
            return CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        public static RectTransform CreateSafeAreaRoot(Transform parent)
        {
            RectTransform safeArea = CreateFullScreenRect(parent, "SafeArea");
            safeArea.gameObject.AddComponent<CampaignSafeArea>();
            return safeArea;
        }

        public static Sprite GetFallbackMarkerSprite()
        {
            if (fallbackMarkerSprite != null)
                return fallbackMarkerSprite;

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "CampaignMarkerFallback";
            Color32[] pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * .5f, (size - 1) * .5f);
            float radius = size * .47f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + .5f - distance) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            fallbackMarkerSprite = Sprite.Create(texture,
                new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), 100f);
            fallbackMarkerSprite.name = "CampaignMarkerFallback";
            return fallbackMarkerSprite;
        }
    }
}
