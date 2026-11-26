using System;
using System.Collections;
using Match3RPG.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Battle
{
    public sealed class RockSpikeSkillController : MonoBehaviour
    {
        public const float DefaultWidthBossRatio = 2.0f; // TẠM: rộng = 2.0 × chiều rộng battle của boss
        public const float FrameRate = 30f;
        public const float CastReleaseNormalizedTime = 0.48f; // F12 -> F13 (12/25 = 0.48, ấn thành)
        public const float DefaultSpriteAspect = 392f / 634f; // Tỉ lệ ô 634×392 của sheet 4438×2352

        [Header("Play-test Skill Level (TẠM: chỉ Lv1)")]
        [SerializeField, Range(1, 1)] int skillLevel = SkillConfig.DefaultLevel;

        [Header("Rock Spike Eruption (Thạch Thứ)")]
        [SerializeField, Min(0.2f)] float widthBossRatio = DefaultWidthBossRatio; // TẠM

        RectTransform effectLayer;
        GameObject activeEruption;
        bool isRunning;

        public bool IsRunning => isRunning;
        public int SkillLevel
        {
            get => Mathf.Clamp(skillLevel, 1, 1);
            set => skillLevel = Mathf.Clamp(value, 1, 1);
        }

        void OnDisable()
        {
            StopAndClear();
        }

        public void StopAndClear()
        {
            isRunning = false;
            if (activeEruption != null)
            {
                Destroy(activeEruption);
                activeEruption = null;
            }
        }

        public static float MeasureBossBattleWidth(CharacterView boss)
        {
            if (boss == null || boss.VisualRect == null)
                return 394.74f;

            RectTransform visualRect = boss.VisualRect;
            float unscaled = Mathf.Min(visualRect.rect.width, visualRect.rect.height);
            if (unscaled <= 1f)
                unscaled = 464.4f;
            float scaleX = Mathf.Abs(visualRect.localScale.x);
            if (scaleX <= 0.01f)
                scaleX = 0.85f;
            return unscaled * scaleX;
        }

        public IEnumerator ExecuteSkill(
            CharacterView hero,
            CharacterView boss,
            SkillConfig config,
            Func<IEnumerator> onImpact)
        {
            if (isRunning)
                yield break;

            isRunning = true;
            Sprite[] frames = ResolveFrames(config);
            if (frames == null || frames.Length == 0)
            {
                Debug.LogError($"[RockSpikeSkill] Missing sprite frames at {SkillConfig.RockSpikeAssetPath}.", this);
                isRunning = false;
                yield break;
            }

            bool heroCastDone = false;
            bool castReleased = false;
            Coroutine heroRoutine = null;

            try
            {
                if (hero != null)
                {
                    heroRoutine = StartCoroutine(RunHeroSkillInPlace(
                        hero,
                        () => castReleased = true,
                        () => heroCastDone = true));

                    while (!castReleased && !heroCastDone)
                        yield return null;
                }

                EnsureEffectLayer(hero, boss);
                if (effectLayer != null)
                {
                    yield return PlayRockSpikeEruption(boss, frames, onImpact);
                }
                else if (onImpact != null)
                {
                    yield return onImpact();
                }

                while (heroRoutine != null && !heroCastDone)
                    yield return null;
            }
            finally
            {
                if (activeEruption != null)
                {
                    Destroy(activeEruption);
                    activeEruption = null;
                }
                isRunning = false;
            }
        }

        static Sprite[] ResolveFrames(SkillConfig config)
        {
            if (config != null && config.EffectFrames != null
                && config.EffectFrames.Length == SkillConfig.RockSpikeFrameCount
                && config.EffectFrames[0] != null)
                return config.EffectFrames;

            return SkillConfig.ResolveRockSpikeFrames();
        }

        static IEnumerator RunHeroSkillInPlace(
            CharacterView hero,
            Action onCastRelease,
            Action onFinished)
        {
            try
            {
                yield return hero.PlaySkillInPlaceWithoutLunge(CastReleaseNormalizedTime, onCastRelease);
            }
            finally
            {
                onCastRelease?.Invoke();
                onFinished?.Invoke();
            }
        }

        IEnumerator PlayRockSpikeEruption(
            CharacterView boss,
            Sprite[] frames,
            Func<IEnumerator> onImpact)
        {
            Vector2 bossGroundLocal = ResolveBossGroundLocal(boss, new Vector2(270f, -150f));
            Sprite sampleSprite = frames.Length > SkillConfig.RockSpikePeakFrameIndex
                ? frames[SkillConfig.RockSpikePeakFrameIndex]
                : frames[0];

            float bossWidth = MeasureBossBattleWidth(boss);
            float displayWidth = bossWidth * Mathf.Max(0.2f, widthBossRatio); // TẠM: 2.0 × chiều rộng boss
            float spriteHeightOverWidth = sampleSprite != null && sampleSprite.rect.width > 0f
                ? sampleSprite.rect.height / sampleSprite.rect.width
                : DefaultSpriteAspect;
            Vector2 spikeSize = new Vector2(displayWidth, displayWidth * spriteHeightOverWidth);

            Vector2 normalizedPivot = sampleSprite != null && sampleSprite.rect.width > 0f
                && sampleSprite.rect.height > 0f
                ? new Vector2(
                    sampleSprite.pivot.x / sampleSprite.rect.width,
                    sampleSprite.pivot.y / sampleSprite.rect.height)
                : new Vector2(0.5f, SkillConfig.RockSpikePivotY);

            GameObject eruptionObj = new GameObject(
                "RockSpikeEruption",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            activeEruption = eruptionObj;

            RectTransform eruptionRect = eruptionObj.GetComponent<RectTransform>();
            eruptionRect.SetParent(effectLayer, false);
            eruptionRect.anchorMin = new Vector2(0.5f, 0.5f);
            eruptionRect.anchorMax = new Vector2(0.5f, 0.5f);
            eruptionRect.pivot = normalizedPivot;
            eruptionRect.sizeDelta = spikeSize;
            eruptionRect.anchoredPosition = bossGroundLocal;
            eruptionRect.localEulerAngles = Vector3.zero;
            eruptionRect.localScale = Vector3.one;

            Image eruptionImage = eruptionObj.GetComponent<Image>();
            eruptionImage.sprite = frames[0];
            eruptionImage.preserveAspect = true;
            eruptionImage.raycastTarget = false;
            eruptionImage.color = Color.white;

            int peakIndex = Mathf.Clamp(SkillConfig.RockSpikePeakFrameIndex, 0, frames.Length - 1);
            float totalDuration = frames.Length / FrameRate;
            float elapsed = 0f;
            int currentFrameIndex = 0;
            bool impactTriggered = false;
            bool impactDone = false;
            Coroutine impactRoutine = null;

            while (elapsed < totalDuration && eruptionObj != null)
            {
                int nextFrameIndex = Mathf.Clamp(
                    Mathf.FloorToInt(elapsed * FrameRate),
                    0,
                    frames.Length - 1);

                if (nextFrameIndex != currentFrameIndex)
                {
                    currentFrameIndex = nextFrameIndex;
                    eruptionImage.sprite = frames[currentFrameIndex];
                }

                if (!impactTriggered && currentFrameIndex >= peakIndex)
                {
                    impactTriggered = true;
                    if (onImpact != null)
                    {
                        impactRoutine = StartCoroutine(RunImpactRoutine(
                            onImpact,
                            () => impactDone = true));
                    }
                    else
                    {
                        impactDone = true;
                    }
                }

                if (impactTriggered && IsBossDefeated(boss))
                    break;

                yield return null;
                float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
                elapsed += dt;
            }

            if (!impactTriggered && onImpact != null && !IsBossDefeated(boss))
            {
                impactTriggered = true;
                impactRoutine = StartCoroutine(RunImpactRoutine(
                    onImpact,
                    () => impactDone = true));
            }

            if (eruptionObj != null)
            {
                Destroy(eruptionObj);
                if (activeEruption == eruptionObj)
                    activeEruption = null;
            }

            while (impactRoutine != null && !impactDone)
                yield return null;
        }

        static IEnumerator RunImpactRoutine(Func<IEnumerator> onImpact, Action onFinished)
        {
            try
            {
                if (onImpact != null)
                    yield return onImpact();
            }
            finally
            {
                onFinished?.Invoke();
            }
        }

        static bool IsBossDefeated(CharacterView boss)
        {
            return boss == null
                || boss.State == CharacterView.CharacterState.Death
                || !boss.gameObject.activeInHierarchy;
        }

        Vector2 ResolveBossGroundLocal(CharacterView boss, Vector2 fallback)
        {
            if (boss == null || effectLayer == null)
                return fallback;

            RectTransform visualRect = boss.VisualRect != null
                ? boss.VisualRect
                : boss.transform as RectTransform;
            if (visualRect == null)
                return fallback;

            // Đáy VisualRect (chân boss / mặt đất battle), KHÔNG dùng VFXAnchor (ngang thân người).
            Rect rect = visualRect.rect;
            Vector3 bottomCenterWorld = visualRect.TransformPoint(
                new Vector3(rect.center.x, rect.yMin, 0f));
            return effectLayer.InverseTransformPoint(bottomCenterWorld);
        }

        void EnsureEffectLayer(CharacterView hero, CharacterView boss)
        {
            if (effectLayer != null)
            {
                effectLayer.SetAsLastSibling();
                return;
            }

            Transform characterLayer = null;
            if (boss != null && boss.transform.parent != null)
                characterLayer = boss.transform.parent.parent;
            else if (hero != null && hero.transform.parent != null)
                characterLayer = hero.transform.parent.parent;

            RectTransform parentRect = characterLayer as RectTransform;
            if (parentRect == null)
                return;

            GameObject layerObj = new GameObject("RockSpikeLayer", typeof(RectTransform));
            effectLayer = layerObj.GetComponent<RectTransform>();
            effectLayer.SetParent(parentRect, false);
            effectLayer.anchorMin = Vector2.zero;
            effectLayer.anchorMax = Vector2.one;
            effectLayer.offsetMin = Vector2.zero;
            effectLayer.offsetMax = Vector2.zero;
            effectLayer.pivot = new Vector2(0.5f, 0.5f);
            effectLayer.localScale = Vector3.one;
            effectLayer.SetAsLastSibling();
        }
    }
}
