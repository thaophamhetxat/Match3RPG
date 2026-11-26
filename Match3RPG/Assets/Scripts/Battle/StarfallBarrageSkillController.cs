using System;
using System.Collections;
using Match3RPG.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Battle
{
    public sealed class StarfallBarrageSkillController : MonoBehaviour
    {
        public const float DefaultHeightBossRatio = 1.6f; // TẠM: cao = 1.6 × chiều cao battle của boss
        public const float FrameRate = 30f;
        public const float CastReleaseNormalizedTime = 0.48f; // F12 -> F13 (12/25 = 0.48, ấn thành)
        public const float DefaultSpriteWidthOverHeight = 768f / 640f; // Tỉ lệ ô 768×640 của sheet 7680×5760

        [Header("Play-test Skill Level (TẠM: chỉ Lv1)")]
        [SerializeField, Range(1, 1)] int skillLevel = SkillConfig.DefaultLevel;

        [Header("Starfall Barrage (Mưa Sao Băng)")]
        [SerializeField, Min(0.2f)] float heightBossRatio = DefaultHeightBossRatio; // TẠM

        RectTransform effectLayer;
        GameObject activeBarrage;
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
            if (activeBarrage != null)
            {
                Destroy(activeBarrage);
                activeBarrage = null;
            }
        }

        public static float MeasureBossBattleHeight(CharacterView boss)
        {
            if (boss == null || boss.VisualRect == null)
                return 394.74f;

            RectTransform visualRect = boss.VisualRect;
            float unscaled = visualRect.rect.height;
            if (unscaled <= 1f)
                unscaled = 464.4f;
            float scaleY = Mathf.Abs(visualRect.localScale.y);
            if (scaleY <= 0.01f)
                scaleY = 0.85f;
            return unscaled * scaleY;
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
                Debug.LogError($"[StarfallBarrageSkill] Missing sprite frames at {SkillConfig.StarfallBarrageAssetPath}.", this);
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
                    yield return PlayStarfallBarrage(boss, frames, onImpact);
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
                if (activeBarrage != null)
                {
                    Destroy(activeBarrage);
                    activeBarrage = null;
                }
                isRunning = false;
            }
        }

        static Sprite[] ResolveFrames(SkillConfig config)
        {
            if (config != null && config.EffectFrames != null
                && config.EffectFrames.Length == SkillConfig.StarfallBarrageFrameCount
                && config.EffectFrames[0] != null)
                return config.EffectFrames;

            return SkillConfig.ResolveStarfallBarrageFrames();
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

        IEnumerator PlayStarfallBarrage(
            CharacterView boss,
            Sprite[] frames,
            Func<IEnumerator> onImpact)
        {
            Vector2 bossGroundLocal = ResolveBossGroundLocal(boss, new Vector2(270f, -150f));
            Sprite sampleSprite = frames.Length > SkillConfig.StarfallBarragePeakFrameIndex
                ? frames[SkillConfig.StarfallBarragePeakFrameIndex]
                : frames[0];

            float bossHeight = MeasureBossBattleHeight(boss);
            float displayHeight = bossHeight * Mathf.Max(0.2f, heightBossRatio); // TẠM: 1.6 × chiều cao battle của boss
            float spriteWidthOverHeight = sampleSprite != null && sampleSprite.rect.height > 0f
                ? sampleSprite.rect.width / sampleSprite.rect.height
                : DefaultSpriteWidthOverHeight;
            Vector2 effectSize = new Vector2(displayHeight * spriteWidthOverHeight, displayHeight);

            Vector2 normalizedPivot = sampleSprite != null && sampleSprite.rect.width > 0f
                && sampleSprite.rect.height > 0f
                ? new Vector2(
                    sampleSprite.pivot.x / sampleSprite.rect.width,
                    sampleSprite.pivot.y / sampleSprite.rect.height)
                : new Vector2(0.5f, SkillConfig.StarfallBarragePivotY);

            GameObject barrageObj = new GameObject(
                "StarfallBarrageEffect",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            activeBarrage = barrageObj;

            RectTransform barrageRect = barrageObj.GetComponent<RectTransform>();
            barrageRect.SetParent(effectLayer, false);
            barrageRect.anchorMin = new Vector2(0.5f, 0.5f);
            barrageRect.anchorMax = new Vector2(0.5f, 0.5f);
            barrageRect.pivot = normalizedPivot;
            barrageRect.sizeDelta = effectSize;
            barrageRect.anchoredPosition = bossGroundLocal;
            barrageRect.localEulerAngles = Vector3.zero;
            barrageRect.localScale = Vector3.one;

            Image barrageImage = barrageObj.GetComponent<Image>();
            barrageImage.sprite = frames[0];
            barrageImage.preserveAspect = true;
            barrageImage.raycastTarget = false;
            barrageImage.color = Color.white;

            int peakIndex = Mathf.Clamp(SkillConfig.StarfallBarragePeakFrameIndex, 0, frames.Length - 1);
            float totalDuration = frames.Length / FrameRate;
            float elapsed = 0f;
            int currentFrameIndex = 0;
            bool impactTriggered = false;
            bool impactDone = false;
            Coroutine impactRoutine = null;

            while (elapsed < totalDuration && barrageObj != null)
            {
                int nextFrameIndex = Mathf.Clamp(
                    Mathf.FloorToInt(elapsed * FrameRate),
                    0,
                    frames.Length - 1);

                if (nextFrameIndex != currentFrameIndex)
                {
                    currentFrameIndex = nextFrameIndex;
                    barrageImage.sprite = frames[currentFrameIndex];
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

            if (barrageObj != null)
            {
                Destroy(barrageObj);
                if (activeBarrage == barrageObj)
                    activeBarrage = null;
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

            GameObject layerObj = new GameObject("StarfallBarrageLayer", typeof(RectTransform));
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
