using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Battle
{
    public static class AfterimageEffect
    {
        public const int GhostCount = 4;
        public const float DefaultFadeDuration = 0.4f;

        // Spatial intervals at 1/5, 2/5, 3/5, 4/5 of the travel path.
        public static readonly float[] DistanceFractions = { 0.2f, 0.4f, 0.6f, 0.8f };

        // Alpha values (out of 255) ordered from nearest to the mover to farthest: 110 / 70 / 40 / 20.
        public static readonly float[] NearToFarAlphas =
        {
            110f / 255f,
            70f / 255f,
            40f / 255f,
            20f / 255f
        };

        public static float GetInitialAlphaForStep(int stepIndex)
        {
            int clampedStep = Mathf.Clamp(stepIndex, 0, GhostCount - 1);
            int nearToFarIndex = (GhostCount - 1) - clampedStep;
            return NearToFarAlphas[nearToFarIndex];
        }

        public static AfterimageGhostFade SpawnUIGhost(
            RectTransform sourceRect,
            Image sourceImage,
            Transform parent,
            int siblingIndex,
            float initialAlpha,
            float fadeDuration = DefaultFadeDuration,
            Vector2? anchoredPositionOverride = null,
            Vector3? localScaleOverride = null)
        {
            if (sourceRect == null || sourceImage == null || sourceImage.sprite == null || parent == null)
                return null;

            GameObject ghostObject = new GameObject(
                "AfterimageGhost",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(AfterimageGhostFade));

            RectTransform ghostRect = ghostObject.GetComponent<RectTransform>();
            ghostRect.SetParent(parent, false);
            ghostRect.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, Mathf.Max(0, parent.childCount - 1)));
            ghostRect.anchorMin = sourceRect.anchorMin;
            ghostRect.anchorMax = sourceRect.anchorMax;
            ghostRect.pivot = sourceRect.pivot;
            ghostRect.sizeDelta = sourceRect.sizeDelta;
            ghostRect.anchoredPosition = anchoredPositionOverride ?? sourceRect.anchoredPosition;
            ghostRect.localRotation = sourceRect.localRotation;
            ghostRect.localScale = localScaleOverride ?? sourceRect.localScale;

            Image ghostImage = ghostObject.GetComponent<Image>();
            ghostImage.sprite = sourceImage.sprite;
            ghostImage.preserveAspect = sourceImage.preserveAspect;
            ghostImage.type = sourceImage.type;
            ghostImage.raycastTarget = false;

            Color baseColor = sourceImage.color;
            float clampedAlpha = Mathf.Clamp01(initialAlpha);
            ghostImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, clampedAlpha);

            AfterimageGhostFade fade = ghostObject.GetComponent<AfterimageGhostFade>();
            fade.Initialize(ghostImage, clampedAlpha, fadeDuration);
            return fade;
        }
    }

    [DisallowMultipleComponent]
    public sealed class AfterimageGhostFade : MonoBehaviour
    {
        Image targetImage;
        float startAlpha;
        float fadeDuration;
        float elapsed;
        bool initialized;

        public void Initialize(Image image, float initialAlpha, float duration)
        {
            targetImage = image;
            startAlpha = Mathf.Clamp01(initialAlpha);
            fadeDuration = Mathf.Max(0.01f, duration);
            elapsed = 0f;
            initialized = targetImage != null;
        }

        void Update()
        {
            if (!initialized || targetImage == null)
            {
                Destroy(gameObject);
                return;
            }

            float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            Color c = targetImage.color;
            c.a = Mathf.Lerp(startAlpha, 0f, t);
            targetImage.color = c;

            if (t >= 1f)
                Destroy(gameObject);
        }

        void OnDisable()
        {
            if (gameObject.scene.isLoaded)
                Destroy(gameObject);
        }
    }
}
