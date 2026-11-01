using Match3RPG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Board
{
    public sealed class BoardSelectionOverlay : MonoBehaviour
    {
        const float ArrowSizeFraction = .24f;
        const float ArrowOffsetFraction = .525f;
        const float PulseDistance = 5f;
        const float PulseCycleSeconds = .7f;

        sealed class Arrow
        {
            public RectTransform Rect;
            public UITriangleGraphic Graphic;
            public Vector2 Direction;
            public Vector2 BasePosition;
        }

        RectTransform overlayRect;
        RectTransform viewportRect;
        RectTransform focusArrowsRect;
        readonly Arrow[] arrows = new Arrow[4];
        GemView focusedGem;
        float focusedCellSize;
        bool configured;

        public void Configure(RectTransform viewport)
        {
            overlayRect = transform as RectTransform;
            viewportRect = viewport;
            focusArrowsRect = CreateRect("FocusArrows", overlayRect);
            arrows[0] = CreateArrow("ArrowUp", Vector2.up);
            arrows[1] = CreateArrow("ArrowDown", Vector2.down);
            arrows[2] = CreateArrow("ArrowLeft", Vector2.left);
            arrows[3] = CreateArrow("ArrowRight", Vector2.right);
            configured = true;
            Hide();
        }

        Arrow CreateArrow(string name, Vector2 direction)
        {
            RectTransform arrowRect = CreateRect(name, focusArrowsRect);
            UITriangleGraphic graphic = arrowRect.gameObject.AddComponent<UITriangleGraphic>();
            graphic.color = new Color(1f, 1f, 1f, .95f);
            graphic.raycastTarget = false;
            Outline outline = arrowRect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.08f, .045f, .015f, .8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;
            float rotation = direction == Vector2.down ? 180f
                : direction == Vector2.left ? 90f
                : direction == Vector2.right ? -90f
                : 0f;
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            graphic.SetVerticesDirty();
            arrowRect.gameObject.SetActive(false);
            return new Arrow
            {
                Rect = arrowRect,
                Graphic = graphic,
                Direction = direction
            };
        }

        static RectTransform CreateRect(string name, RectTransform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        public void Focus(GemView gem, RectTransform cellRect, int rows, int columns)
        {
            if (!configured || gem == null || cellRect == null || viewportRect == null)
            {
                Hide();
                return;
            }

            focusedGem = gem;
            Vector3 cellCenter = cellRect.TransformPoint(cellRect.rect.center);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, cellCenter);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect, screenPoint, null, out Vector2 overlayLocalPoint))
            {
                Vector3 overlayCenter = overlayRect.InverseTransformPoint(cellCenter);
                overlayLocalPoint = new Vector2(overlayCenter.x, overlayCenter.y);
            }
            focusArrowsRect.anchoredPosition = overlayLocalPoint;
            Debug.Log($"Focus arrows positioned for Gem ({gem.Position.Row},{gem.Position.Col}): "
                + $"cellWorld={cellCenter}, anchoredPosition={focusArrowsRect.anchoredPosition}.",
                this);
            focusedCellSize = Mathf.Min(cellRect.rect.width, cellRect.rect.height);

            for (int index = 0; index < arrows.Length; index++)
            {
                Arrow arrow = arrows[index];
                Vector2 direction = arrow.Direction;
                bool visible = IsDirectionAvailable(gem.Position, rows, columns, direction);
                Vector2 arrowSize = Vector2.one * (focusedCellSize * ArrowSizeFraction);
                if (arrow.Rect.sizeDelta != arrowSize)
                {
                    arrow.Rect.sizeDelta = arrowSize;
                    arrow.Graphic.SetVerticesDirty();
                }
                arrow.BasePosition = direction * (focusedCellSize * ArrowOffsetFraction);
                arrow.Rect.anchoredPosition = arrow.BasePosition;
                arrow.Rect.gameObject.SetActive(visible);
            }
        }

        internal static bool IsDirectionAvailable(
            BoardPos position, int rows, int columns, Vector2 direction)
        {
            if (direction == Vector2.up) return position.Row > 0;
            if (direction == Vector2.down) return position.Row < rows - 1;
            if (direction == Vector2.left) return position.Col > 0;
            if (direction == Vector2.right) return position.Col < columns - 1;
            return false;
        }

        public void Hide()
        {
            focusedGem = null;
            for (int index = 0; index < arrows.Length; index++)
                if (arrows[index] != null)
                    arrows[index].Rect.gameObject.SetActive(false);
        }

        void Update()
        {
            if (focusedGem == null)
            {
                if (configured)
                    Hide();
                return;
            }

            float pulse = .5f - .5f * Mathf.Cos(
                Time.unscaledTime * (2f * Mathf.PI / PulseCycleSeconds));
            float alpha = Mathf.Lerp(.6f, 1f, pulse);
            float distance = Mathf.Lerp(0f, PulseDistance, pulse);
            foreach (Arrow arrow in arrows)
            {
                if (!arrow.Rect.gameObject.activeSelf)
                    continue;
                arrow.Rect.anchoredPosition = arrow.BasePosition
                    + arrow.Direction * distance;
                Color color = arrow.Graphic.color;
                color.a = alpha;
                arrow.Graphic.color = color;
            }
        }
    }
}
