using System.Threading;
using Match3RPG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Board
{
    public sealed class GemView : MonoBehaviour
    {
        const float GemVisualScale = 1.05f;

        [SerializeField] Image image;
        [SerializeField] Button button;
        [SerializeField] Image cellBackground;
        [SerializeField] Image horizontalFallback;
        [SerializeField] Image verticalFallback;
        [SerializeField] Image selection;
        [SerializeField] RectTransform visualRoot;
        SpecialGemIdleVFX specialIdleVFX;
        Image focusBrighten;
        GemFocusOutlineGraphic focusOutline;
        bool focused;
        static long nextRuntimeId;
        public BoardPos Position { get; private set; }
        public GemType Type { get; private set; }
        public GemSpecialType SpecialType { get; private set; }
        public long RuntimeId { get; private set; }
        public Image SpriteImage => image;
        public SpecialGemIdleVFX SpecialIdleVFX => specialIdleVFX;
        BoardManager owner;
        Sprite normalSprite;
        GemVisualDatabase visualDatabase;
        static readonly System.Collections.Generic.HashSet<string> LoggedMissingSpecials =
            new System.Collections.Generic.HashSet<string>();

        void Awake()
        {
            if (image == null) image = GetComponent<Image>();
            if (button == null) button = GetComponent<Button>();
            CacheVisualChildren();
        }

        public void ConfigureArt(Image cell, Image gem, Image horizontal,
            Image vertical, Image selectionOverlay,
            RectTransform gemVisualRoot, GemVisualDatabase database)
        {
            cellBackground = cell;
            image = gem;
            horizontalFallback = horizontal;
            verticalFallback = vertical;
            selection = selectionOverlay;
            visualRoot = gemVisualRoot;
            visualDatabase = database;
            EnsureSpecialIdleVFX();
            if (image != null) normalSprite = image.sprite;
            CacheVisualChildren();
            EnsureFocusVisuals();
            ConfigureSpriteImage();
            ResetVisualTransforms();
            RefreshSpecialVisual();
        }

        void OnRectTransformDimensionsChange() => ResetVisualTransforms();

        public void Initialize(BoardManager board, BoardPos pos, GemType type,
            GemSpecialType special, Sprite sprite, GemVisualDatabase database = null)
        {
            if (RuntimeId == 0) RuntimeId = Interlocked.Increment(ref nextRuntimeId);
            owner = board; Position = pos; Type = type; SpecialType = special;
            if (database != null) visualDatabase = database;
            if (image == null) image = GetComponent<Image>();
            if (button == null) button = GetComponent<Button>();
            if (image == null || button == null) return;
            EnsureFocusVisuals();
            ConfigureSpriteImage();
            ResetVisualTransforms();
            if (normalSprite == null) normalSprite = sprite;
            SetType(type, sprite);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => owner.Select(this));
        }

        public void SetPosition(BoardPos pos) => Position = pos;
        public void SetType(GemType type, Sprite sprite)
        {
            Type = type;
            normalSprite = sprite;
            RefreshSpecialVisual();
        }

        public void SetSpecialType(GemSpecialType special)
        {
            SpecialType = special;
            RefreshSpecialVisual();
        }

        public void SetState(GemType type, GemSpecialType special, Sprite sprite,
            bool playCreationFlash = false)
        {
            Type = type;
            SpecialType = special;
            normalSprite = sprite;
            RefreshSpecialVisual();
            if (playCreationFlash)
                specialIdleVFX?.PlayCreationFlash();
        }

        public void SetInteractable(bool value)
        {
            if (button != null) button.interactable = value;
        }

        public void SetSelected(bool value)
        {
            SetFocused(value);
        }

        public void SetFocused(bool value)
        {
            focused = value;
            if (focusBrighten != null)
                focusBrighten.enabled = value && focusBrighten.sprite != null;
            if (focusOutline != null) focusOutline.enabled = value;
            if (selection != null) selection.enabled = false;
            if (visualRoot != null)
                visualRoot.localScale = Vector3.one * GemVisualScale * (value ? 1.06f : 1f);
        }

        public void StopSpecialVFX()
        {
            if (specialIdleVFX != null)
                specialIdleVFX.StopIdleEffect();
        }

        void OnDestroy()
        {
            if (owner != null)
                owner.ClearSelectionForDestroyedGem(this);
            StopSpecialVFX();
        }

        void CacheVisualChildren()
        {
            if (visualRoot == null)
            {
                Transform child = transform.Find("GemVisualRoot");
                if (child != null) visualRoot = child as RectTransform;
            }
            if (cellBackground == null) cellBackground = FindImage("CellBackground");
            if (image == null) image = FindImage("GemSprite");
            if (horizontalFallback == null) horizontalFallback = FindImage("HorizontalFallback");
            if (verticalFallback == null) verticalFallback = FindImage("VerticalFallback");
            if (selection == null) selection = FindImage("Selection");
            if (focusBrighten == null) focusBrighten = FindImage("FocusBrighten");
            if (focusOutline == null)
            {
                Transform outline = transform.Find("GemVisualRoot/FocusOutline");
                if (outline != null)
                    focusOutline = outline.GetComponent<GemFocusOutlineGraphic>();
            }
            EnsureSpecialIdleVFX();
            if (horizontalFallback != null) horizontalFallback.enabled = false;
            if (verticalFallback != null) verticalFallback.enabled = false;
            if (selection != null) selection.enabled = false;
            ConfigureSpriteImage();
            SetDecorativeRaycast(cellBackground);
            SetDecorativeRaycast(image);
            SetDecorativeRaycast(horizontalFallback);
            SetDecorativeRaycast(verticalFallback);
            SetDecorativeRaycast(selection);
            SetDecorativeRaycast(focusBrighten);
            if (normalSprite == null && image != null) normalSprite = image.sprite;
        }

        void EnsureFocusVisuals()
        {
            if (visualRoot == null)
                return;

            if (focusBrighten == null)
            {
                Transform existing = visualRoot.Find("FocusBrighten");
                if (existing != null)
                    focusBrighten = existing.GetComponent<Image>();
            }
            if (focusBrighten == null)
            {
                GameObject brightenObject = new GameObject("FocusBrighten",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform brightenRect = brightenObject.GetComponent<RectTransform>();
                brightenRect.SetParent(visualRoot, false);
                focusBrighten = brightenObject.GetComponent<Image>();
            }
            ConfigureFocusRect(focusBrighten.rectTransform);
            focusBrighten.sprite = image != null ? image.sprite : null;
            focusBrighten.color = new Color(1f, 1f, 1f, .18f);
            focusBrighten.preserveAspect = true;
            focusBrighten.raycastTarget = false;
            focusBrighten.enabled = focused && focusBrighten.sprite != null;

            if (focusOutline == null)
            {
                Transform existing = visualRoot.Find("FocusOutline");
                if (existing != null)
                    focusOutline = existing.GetComponent<GemFocusOutlineGraphic>();
            }
            if (focusOutline == null)
            {
                GameObject outlineObject = new GameObject("FocusOutline",
                    typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(GemFocusOutlineGraphic));
                RectTransform outlineRect = outlineObject.GetComponent<RectTransform>();
                outlineRect.SetParent(visualRoot, false);
                focusOutline = outlineObject.GetComponent<GemFocusOutlineGraphic>();
            }
            ConfigureFocusRect(focusOutline.rectTransform);
            focusOutline.color = new Color(1f, .92f, .55f, .9f);
            focusOutline.raycastTarget = false;
            focusOutline.enabled = focused;
        }

        static void ConfigureFocusRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        void EnsureSpecialIdleVFX()
        {
            Transform parent = transform;
            if (specialIdleVFX == null)
            {
                Transform existing = parent.Find("SpecialAura");
                if (existing != null)
                    specialIdleVFX = existing.GetComponent<SpecialGemIdleVFX>();
            }

            if (specialIdleVFX == null)
            {
                GameObject aura = new GameObject("SpecialAura",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(SpecialGemIdleVFX));
                aura.transform.SetParent(parent, false);
                specialIdleVFX = aura.GetComponent<SpecialGemIdleVFX>();
            }
            else if (specialIdleVFX.transform.parent != parent)
                specialIdleVFX.transform.SetParent(parent, false);

            RectTransform auraRect = specialIdleVFX.rectTransform;
            auraRect.anchorMin = Vector2.zero;
            auraRect.anchorMax = Vector2.one;
            auraRect.pivot = new Vector2(.5f, .5f);
            auraRect.anchoredPosition = Vector2.zero;
            auraRect.offsetMin = Vector2.zero;
            auraRect.offsetMax = Vector2.zero;
            auraRect.SetAsFirstSibling();
            specialIdleVFX.raycastTarget = false;
        }

        Image FindImage(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        static void SetDecorativeRaycast(Image target)
        {
            if (target != null) target.raycastTarget = false;
        }

        void ConfigureSpriteImage()
        {
            if (image == null) return;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.fillAmount = 1f;
            image.raycastTarget = false;
        }

        void FitGemSpriteToCell()
        {
            if (image == null || image.rectTransform == null) return;
            RectTransform spriteRect = image.rectTransform;
            spriteRect.anchorMin = Vector2.zero;
            spriteRect.anchorMax = Vector2.one;
            spriteRect.pivot = new Vector2(.5f, .5f);
            spriteRect.anchoredPosition = Vector2.zero;
            spriteRect.offsetMin = Vector2.zero;
            spriteRect.offsetMax = Vector2.zero;
            spriteRect.localScale = Vector3.one;
            spriteRect.localRotation = Quaternion.identity;
        }

        void ResetVisualTransforms()
        {
            if (visualRoot != null)
            {
                RectTransform cellRect = transform as RectTransform;
                visualRoot.anchorMin = new Vector2(.5f, .5f);
                visualRoot.anchorMax = new Vector2(.5f, .5f);
                visualRoot.pivot = new Vector2(.5f, .5f);
                if (cellRect != null)
                    visualRoot.sizeDelta = cellRect.rect.size;
                visualRoot.anchoredPosition = Vector2.zero;
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale = Vector3.one * GemVisualScale * (focused ? 1.06f : 1f);
            }
            FitGemSpriteToCell();
            EnsureFocusVisuals();
        }

        public void SetVisualOffset(Vector2 offset)
        {
            if (visualRoot != null) visualRoot.anchoredPosition = offset;
        }

        public float GetVisualCenterErrorIn(RectTransform referenceRoot, RectTransform cellRect)
        {
            if (referenceRoot == null || cellRect == null || image == null)
                return float.PositiveInfinity;
            Vector3 cellCenter = referenceRoot.InverseTransformPoint(
                cellRect.TransformPoint(cellRect.rect.center));
            Vector3 spriteCenter = referenceRoot.InverseTransformPoint(
                image.rectTransform.TransformPoint(image.rectTransform.rect.center));
            return Vector2.Distance(cellCenter, spriteCenter);
        }

        public void RefreshSpecialVisual()
        {
            bool isLine = SpecialType == GemSpecialType.Line;
            bool needsSealedSprite = SpecialType == GemSpecialType.Line
                || SpecialType == GemSpecialType.AreaBomb;
            bool hasSpecialSprite = !needsSealedSprite
                || (visualDatabase != null && visualDatabase.HasSpecialSprite(Type, SpecialType));
            if (image != null)
            {
                Sprite mappedSprite = visualDatabase != null
                    ? visualDatabase.GetSprite(Type, SpecialType)
                    : needsSealedSprite ? null : normalSprite;
                image.sprite = mappedSprite;
            }
            if (focusBrighten != null)
                focusBrighten.sprite = image != null ? image.sprite : null;
            if (needsSealedSprite && !hasSpecialSprite)
            {
                string key = $"{Type}:{SpecialType}";
                if (LoggedMissingSpecials.Add(key))
                    Debug.LogWarning($"{Type} {SpecialType} sprite is missing. Expected mapping: "
                        + $"{GemVisualDatabase.ExpectedVisualMapping(Type, SpecialType)}. "
                        + "The regular Gem sprite will not be used as a fallback.", this);
            }
            if (horizontalFallback != null)
                horizontalFallback.enabled = isLine && !hasSpecialSprite;
            if (verticalFallback != null)
                verticalFallback.enabled = isLine && !hasSpecialSprite;
            if (selection != null && selection.enabled && selection.sprite == null) selection.enabled = false;
            EnsureSpecialIdleVFX();
            specialIdleVFX.SetSpecialType(SpecialType);
        }
    }
}
