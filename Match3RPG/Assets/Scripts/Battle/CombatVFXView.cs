using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Battle
{
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class CombatVFXView : MonoBehaviour
    {
        RectTransform rectTransform;
        Image image;

        void OnDisable()
        {
            StopAllCoroutines();
        }

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            image.raycastTarget = false;
        }

        public void Configure(Sprite sprite, Vector2 size, Vector2 position, float direction)
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            if (image == null)
                image = GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position;
            rectTransform.localScale = new Vector3(direction < 0f ? -1f : 1f, 1f, 1f);
            rectTransform.localRotation = Quaternion.identity;
        }

        public void BeginTransient(CombatVFXType type, float duration, Vector2 destination,
            bool travels, System.Action onComplete)
        {
            StartCoroutine(PlayTransient(type, duration, destination, travels, null, 0f, onComplete));
        }

        public void BeginFrameTransient(CombatVFXType type, Sprite[] frames, float frameRate,
            float duration, System.Action onComplete)
        {
            if (frames == null || frames.Length == 0)
            {
                onComplete?.Invoke();
                Destroy(gameObject);
                return;
            }

            image.sprite = frames[0];
            StartCoroutine(PlayTransient(type, duration, rectTransform.anchoredPosition,
                false, frames, frameRate, onComplete));
        }

        public void BeginPersistent(CombatVFXType type)
        {
            StartCoroutine(PulsePersistent(type));
        }

        IEnumerator PlayTransient(CombatVFXType type, float duration, Vector2 destination,
            bool travels, Sprite[] frames, float frameRate, System.Action onComplete)
        {
            Vector3 baseScale = rectTransform.localScale;
            Vector2 start = rectTransform.anchoredPosition;
            float elapsed = 0f;
            float safeDuration = Mathf.Max(.01f, duration);
            Color color = image.color;
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                if (frames != null && frames.Length > 0)
                {
                    int frameIndex = Mathf.Min(Mathf.FloorToInt(elapsed * frameRate), frames.Length - 1);
                    image.sprite = frames[frameIndex];
                }
                float scale = ScaleAt(type, t);
                rectTransform.localScale = new Vector3(baseScale.x * scale, baseScale.y * scale, 1f);
                if (travels)
                    rectTransform.anchoredPosition = Vector2.Lerp(start, destination, EaseOut(t));
                else if (type == CombatVFXType.Heal || type == CombatVFXType.LifeSteal)
                    rectTransform.anchoredPosition = start + Vector2.up * (24f * t);

                color.a = AlphaAt(type, t);
                image.color = color;
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, RotationAt(type, t));
                yield return null;
            }

            if (travels)
                rectTransform.anchoredPosition = destination;
            onComplete?.Invoke();
            Destroy(gameObject);
        }

        IEnumerator PulsePersistent(CombatVFXType type)
        {
            float phase = 0f;
            while (true)
            {
                phase += Time.unscaledDeltaTime;
                float pulse = (Mathf.Sin(phase * PulseSpeed(type)) + 1f) * .5f;
                float scale = Mathf.Lerp(.94f, 1.06f, pulse);
                rectTransform.localScale = new Vector3(scale, scale, 1f);
                Color color = image.color;
                color.a = type == CombatVFXType.Shield ? Mathf.Lerp(.55f, .82f, pulse)
                    : type == CombatVFXType.Poison ? Mathf.Lerp(.60f, .78f, pulse)
                    : type == CombatVFXType.RageReady ? Mathf.Lerp(.72f, 1f, pulse)
                    : Mathf.Lerp(.58f, .86f, pulse);
                image.color = color;
                if (type == CombatVFXType.Stun)
                    rectTransform.Rotate(0f, 0f, -Time.unscaledDeltaTime * 50f);
                else if (type == CombatVFXType.Poison)
                    rectTransform.Rotate(0f, 0f, Time.unscaledDeltaTime * 12f);
                yield return null;
            }
        }

        public void BeginFadeOut(float duration)
        {
            StartCoroutine(FadeOut(duration));
        }

        IEnumerator FadeOut(float duration)
        {
            Color original = image.color;
            float elapsed = 0f;
            float safeDuration = Mathf.Max(.01f, duration);
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                Color color = original;
                color.a *= 1f - Mathf.Clamp01(elapsed / safeDuration);
                image.color = color;
                yield return null;
            }
            Destroy(gameObject);
        }

        static float ScaleAt(CombatVFXType type, float t)
        {
            switch (type)
            {
                case CombatVFXType.Slash:
                    return t < .55f ? Mathf.Lerp(.75f, 1.1f, EaseOut(t / .55f))
                        : Mathf.Lerp(1.1f, 1f, (t - .55f) / .45f);
                case CombatVFXType.Heal:
                case CombatVFXType.LifeSteal:
                    return Mathf.Lerp(.72f, 1f, EaseOut(t));
                case CombatVFXType.Ultimate:
                    return t < .55f ? Mathf.Lerp(.6f, 1.2f, EaseOut(t / .55f))
                        : Mathf.Lerp(1.2f, 1f, (t - .55f) / .45f);
                case CombatVFXType.Fireball:
                case CombatVFXType.AirSlash:
                case CombatVFXType.Dash:
                    return Mathf.Lerp(.78f, 1f, EaseOut(t));
                default:
                    return Mathf.Lerp(.55f, 1.08f, EaseOut(Mathf.Min(1f, t * 2.5f)));
            }
        }

        static float AlphaAt(CombatVFXType type, float t)
        {
            if (type == CombatVFXType.Heal || type == CombatVFXType.LifeSteal)
                return t < .2f ? t / .2f : 1f - .55f * ((t - .2f) / .8f);
            if (type == CombatVFXType.Ultimate)
                return t < .55f ? Mathf.Lerp(.2f, 1f, t / .55f)
                    : 1f - .8f * ((t - .55f) / .45f);
            return t < .72f ? 1f : 1f - (t - .72f) / .28f;
        }

        static float RotationAt(CombatVFXType type, float t)
        {
            if (type == CombatVFXType.HitImpact || type == CombatVFXType.CriticalHit
                || type == CombatVFXType.ShieldHit || type == CombatVFXType.Ultimate)
                return Mathf.Lerp(-12f, 8f, t);
            return 0f;
        }

        static float PulseSpeed(CombatVFXType type)
        {
            switch (type)
            {
                case CombatVFXType.Stun: return 2.2f;
                case CombatVFXType.Burn: return 4.5f;
                case CombatVFXType.Freeze: return 1.5f;
                case CombatVFXType.Poison: return 1.8f;
                case CombatVFXType.RageReady: return 3.8f;
                default: return 2.4f;
            }
        }

        static float EaseOut(float t)
        {
            float inverse = 1f - t;
            return 1f - inverse * inverse;
        }
    }
}
