using System;
using System.Collections;
using Match3RPG.Campaign.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Campaign
{
    public sealed class WorldMapRegionMarkerView : MonoBehaviour
    {
        static Sprite outerGlowSprite;
        static Sprite diamondSprite;
        static Sprite innerCoreSprite;
        static Sprite pulseRingSprite;
        static Sprite lockSprite;

        [SerializeField] string regionId;
        [SerializeField] Button button;
        [SerializeField] RectTransform markerVisual;
        [SerializeField] RectTransform pulseRect;
        Image outerGlow;
        Image pulse;
        Image body;
        Image core;
        Image lockIcon;
        Coroutine animationRoutine;
        Vector3 markerBaseScale;
        Vector3 pulseBaseScale;
        bool hasBaseScale;

        public string RegionId => regionId;

        public void SetRegionId(string value)
        {
            regionId = value;
        }

        public void Configure(RegionData region, CampaignNodeState state, bool current,
            Button hitArea, Sprite lockedMarker, Sprite availableMarker, Sprite completedMarker,
            Sprite glowSprite, Action<RegionData> selected)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));
            if (button == null)
                button = hitArea != null ? hitArea : GetComponent<Button>();
            if (markerVisual == null)
                markerVisual = transform.Find("MarkerVisual") as RectTransform;
            if (markerVisual == null)
                markerVisual = transform as RectTransform;
            if (outerGlow == null)
                outerGlow = markerVisual.Find("OuterGlow")?.GetComponent<Image>();
            if (pulseRect == null)
                pulseRect = markerVisual.Find("PulseEffect") as RectTransform;
            if (pulse == null && pulseRect != null)
                pulse = pulseRect.GetComponent<Image>();
            if (body == null)
                body = markerVisual.Find("MarkerBody")?.GetComponent<Image>();
            if (core == null)
                core = markerVisual.Find("InnerCore")?.GetComponent<Image>();
            if (lockIcon == null)
                lockIcon = markerVisual.Find("LockVisual")?.GetComponent<Image>();

            if (button == null || body == null || outerGlow == null || pulse == null
                || core == null || lockIcon == null)
            {
                Debug.LogError($"WorldMap marker visuals are incomplete for RegionId '{region.RegionId}'.", this);
                return;
            }

            regionId = region.RegionId;
            if (!hasBaseScale)
            {
                markerBaseScale = markerVisual.localScale;
                pulseBaseScale = pulseRect.localScale;
                hasBaseScale = true;
            }

            body.sprite = state == CampaignNodeState.Locked ? lockedMarker
                : state == CampaignNodeState.Completed ? completedMarker : availableMarker;
            body.color = Color.white;
            body.raycastTarget = false;
            core.color = current ? new Color32(220, 255, 255, 255)
                : state == CampaignNodeState.Locked ? new Color32(164, 174, 178, 255)
                : state == CampaignNodeState.Completed ? new Color32(255, 244, 198, 255)
                : new Color32(239, 244, 190, 255);
            outerGlow.sprite = glowSprite;
            outerGlow.color = current ? new Color(0.12f, 0.78f, 1f, 0.45f)
                : state == CampaignNodeState.Locked ? new Color(0.42f, 0.48f, 0.52f, 0.1f)
                : state == CampaignNodeState.Completed ? new Color(1f, 0.56f, 0.12f, 0.25f)
                : new Color(0.46f, 0.82f, 0.48f, 0.2f);
            pulse.color = current ? new Color(0.25f, 0.9f, 1f, 0.6f) : Color.clear;
            pulse.gameObject.SetActive(current);
            lockIcon.color = new Color32(200, 208, 210, 230);
            lockIcon.gameObject.SetActive(state == CampaignNodeState.Locked && !current);

            if (animationRoutine != null)
                StopCoroutine(animationRoutine);
            animationRoutine = null;
            markerVisual.localScale = markerBaseScale;
            pulseRect.localScale = pulseBaseScale;
            button.interactable = state != CampaignNodeState.Locked;
            button.onClick.RemoveAllListeners();
            if (state != CampaignNodeState.Locked)
                button.onClick.AddListener(() => selected?.Invoke(region));

            if (current || state == CampaignNodeState.Available)
                animationRoutine = StartCoroutine(AnimateMarker(current));
        }

        IEnumerator AnimateMarker(bool current)
        {
            float startedAt = Time.unscaledTime;
            while (true)
            {
                float duration = current ? 1f : 2.6f;
                float phase = Mathf.Repeat((Time.unscaledTime - startedAt) / duration, 1f);
                float wave = .5f - .5f * Mathf.Cos(phase * Mathf.PI * 2f);

                markerVisual.localScale = markerBaseScale * (1f + (current ? .12f : .04f) * wave);
                if (current)
                {
                    SetAlpha(outerGlow, Mathf.Lerp(.45f, .95f, wave));
                    pulseRect.localScale = pulseBaseScale * Mathf.Lerp(.5f, 1.4f, phase);
                    SetAlpha(pulse, .6f * (1f - phase));
                }

                yield return null;
            }
        }

        static float GetCurrentFloatOffset(float phase)
        {
            if (phase < .25f)
                return SmoothStep(phase / .25f) * 5f;
            if (phase < .5f)
                return (1f - SmoothStep((phase - .25f) / .25f)) * 5f;
            if (phase < .75f)
                return -SmoothStep((phase - .5f) / .25f) * 3f;
            return (1f - SmoothStep((phase - .75f) / .25f)) * -3f;
        }

        static float SmoothStep(float value)
        {
            return value * value * (3f - 2f * value);
        }

        static void SetAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        static Sprite GlowSprite()
        {
            if (outerGlowSprite == null)
                outerGlowSprite = CreateSprite("WorldMapMarkerGlow", 96, SpriteShape.Glow);
            return outerGlowSprite;
        }

        static Sprite DiamondSprite()
        {
            if (diamondSprite == null)
                diamondSprite = CreateSprite("WorldMapMarkerDiamond", 64, SpriteShape.Diamond);
            return diamondSprite;
        }

        static Sprite CoreSprite()
        {
            if (innerCoreSprite == null)
                innerCoreSprite = CreateSprite("WorldMapMarkerCore", 48, SpriteShape.Core);
            return innerCoreSprite;
        }

        static Sprite PulseSprite()
        {
            if (pulseRingSprite == null)
                pulseRingSprite = CreateSprite("WorldMapMarkerPulse", 64, SpriteShape.Ring);
            return pulseRingSprite;
        }

        static Sprite LockIconSprite()
        {
            if (lockSprite == null)
                lockSprite = CreateSprite("WorldMapMarkerLock", 48, SpriteShape.Lock);
            return lockSprite;
        }

        static Sprite CreateSprite(string spriteName, int size, SpriteShape shape)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = spriteName + "Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + .5f) / size * 2f - 1f;
                    float ny = (y + .5f) / size * 2f - 1f;
                    pixels[y * size + x] = GetPixel(shape, nx, ny, size);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = spriteName;
            return sprite;
        }

        static Color32 GetPixel(SpriteShape shape, float x, float y, int size)
        {
            switch (shape)
            {
                case SpriteShape.Glow:
                    float radiusSquared = x * x + y * y;
                    return new Color32(255, 255, 255,
                        (byte)Mathf.RoundToInt(Mathf.Exp(-radiusSquared * 5f) * 255f));
                case SpriteShape.Diamond:
                case SpriteShape.Core:
                    float diamondDistance = Mathf.Abs(x) + Mathf.Abs(y);
                    float diamondAlpha = Mathf.Clamp01((1.04f - diamondDistance) * size * .5f);
                    float facet = Mathf.Lerp(.46f, 1f, 1f - Mathf.Abs(x) * .62f);
                    facet *= Mathf.Lerp(.84f, 1f, (y + 1f) * .5f);
                    byte brightness = (byte)Mathf.RoundToInt(facet * 255f);
                    return new Color32(brightness, brightness, brightness,
                        (byte)Mathf.RoundToInt(diamondAlpha * 255f));
                case SpriteShape.Ring:
                    float ringDistance = Mathf.Abs(x) + Mathf.Abs(y);
                    float ringAlpha = 1f - Mathf.Clamp01(Mathf.Abs(ringDistance - .68f) * size * .5f);
                    return new Color32(255, 255, 255,
                        (byte)Mathf.RoundToInt(ringAlpha * 255f));
                default:
                    return GetLockPixel(x, y, size);
            }
        }

        static Color32 GetLockPixel(float x, float y, int size)
        {
            float bodyEdge = Mathf.Max(Mathf.Abs(x) - .65f, Mathf.Max(y - .02f, -.88f - y));
            bool body = bodyEdge <= 0f;

            float ellipse = (x * x) / (.48f * .48f) + ((y - .16f) * (y - .16f)) / (.52f * .52f);
            float innerEllipse = (x * x) / (.28f * .28f) + ((y - .16f) * (y - .16f)) / (.34f * .34f);
            bool shackle = ellipse <= 1f && innerEllipse >= 1f && y >= .08f;
            if (!body && !shackle)
                return new Color32(255, 255, 255, 0);

            float coverage = body
                ? Mathf.Clamp01(-bodyEdge * size * .5f + .5f)
                : Mathf.Clamp01(Mathf.Min(Mathf.Abs(ellipse - 1f), Mathf.Abs(innerEllipse - 1f)) * size * .5f + .5f);
            byte alpha = (byte)Mathf.RoundToInt(coverage * 255f);
            return new Color32(255, 255, 255, alpha);
        }

        enum SpriteShape
        {
            Glow,
            Diamond,
            Core,
            Ring,
            Lock
        }
    }
}
