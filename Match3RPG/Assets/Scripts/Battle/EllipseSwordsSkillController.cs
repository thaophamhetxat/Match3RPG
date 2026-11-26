using System;
using System.Collections;
using System.Collections.Generic;
using Match3RPG.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Battle
{
    public sealed class EllipseSwordsSkillController : MonoBehaviour
    {
        readonly struct PortalSpec
        {
            public readonly float DemoX;
            public readonly float DemoY;
            public readonly float DemoWidth;
            public readonly float DemoHeight;
            public readonly float TiltDegrees;

            public PortalSpec(float demoX, float demoY, float demoWidth, float demoHeight, float tiltDegrees)
            {
                DemoX = demoX;
                DemoY = demoY;
                DemoWidth = demoWidth;
                DemoHeight = demoHeight;
                TiltDegrees = tiltDegrees;
            }
        }

        // Bảng vị trí 5 cổng elip theo demo v4 (canvas 800×450), quy đổi lên khung 1080×607.5
        // neo mép TRÊN CharacterLayer (1080×2400) để giữ đúng tỉ lệ 16:9, không bị kéo giãn dọc.
        public const float DemoCanvasWidth = 800f;
        public const float DemoCanvasHeight = 450f;
        public const float ReferenceFrameWidth = 1080f;
        public const float ReferenceFrameHeight = 607.5f;
        public const float PortalStaggerSeconds = 0.10f; // TẠM: các cổng hiện cách nhau ~0.1s
        public const float PortalFadeInSeconds = 0.15f; // TẠM: mỗi cổng mờ dần vào ~0.15s
        public const float PortalFadeOutSeconds = 0.30f; // TẠM: 5 cổng mờ dần sau khi xong trong 0.3s
        public const float SwordEmergeSeconds = 0.13f; // TẠM: kiếm chui ra lớn dần 30% -> 100% trong ~0.13s
        public const float SwordEmergeStartScale = 0.30f; // TẠM
        public const float SwordLengthLayerWidthRatio = 0.175f; // TẠM: ~140px trên layer rộng 800px (189px trên 1080px)
        const float DefaultPortalHeightOverWidth = 148f / 256f;
        const float DefaultSwordHeightOverWidth = 1280f / 1920f;

        static readonly PortalSpec[] PortalSpecs =
        {
            new PortalSpec(195f, 48f,  74f, 40f, 50f), // TẠM: Cổng #1
            new PortalSpec(120f, 98f,  64f, 34f, 35f), // TẠM: Cổng #2
            new PortalSpec(195f, 108f, 52f, 30f, 20f), // TẠM: Cổng #3
            new PortalSpec(165f, 152f, 48f, 28f, 45f), // TẠM: Cổng #4
            new PortalSpec(100f, 172f, 68f, 36f, 40f)  // TẠM: Cổng #5
        };

        [Header("Play-test Skill Level (TẠM: chỉ Lv1)")]
        [SerializeField, Range(1, 1)] int skillLevel = SkillConfig.DefaultLevel;

        [Header("Ellipse Swords Timing (TẠM)")]
        [SerializeField, Min(0.02f)] float launchIntervalSeconds = SkillConfig.EllipseSwordsDefaultIntervalSeconds; // TẠM: 0.15s
        [SerializeField, Min(0.05f)] float swordFlightSeconds = SkillConfig.EllipseSwordsDefaultFlightSeconds; // TẠM: 0.50s

        RectTransform effectLayer;
        readonly List<Image> activePortals = new List<Image>(SkillConfig.EllipseSwordsDefaultSwordCount);
        readonly List<GameObject> activeSwords = new List<GameObject>(SkillConfig.EllipseSwordsDefaultSwordCount);
        readonly List<Coroutine> activeSwordRoutines = new List<Coroutine>(SkillConfig.EllipseSwordsDefaultSwordCount);
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
            StopAndClearSwords();
            activePortals.Clear();
            if (effectLayer != null)
            {
                Destroy(effectLayer.gameObject);
                effectLayer = null;
            }
        }

        public IEnumerator ExecuteSkill(
            CharacterView hero,
            CharacterView boss,
            SkillConfig config,
            Func<int, IEnumerator> onImpact,
            Func<bool> shouldStop = null)
        {
            if (isRunning)
                yield break;

            isRunning = true;

            Sprite portalSprite = config != null && config.Icon != null
                ? config.Icon
                : SkillConfig.ResolveEllipseSwordsSprite();
            if (portalSprite == null)
                portalSprite = SkillConfig.ResolveEllipseSwordsSprite();

            Sprite swordSprite = SkillConfig.ResolveSwordSprite();
            if (portalSprite == null)
            {
                Debug.LogError($"[EllipseSwordsSkill] Missing portal sprite at {SkillConfig.EllipseSwordsAssetPath}.", this);
                isRunning = false;
                yield break;
            }
            if (swordSprite == null)
            {
                Debug.LogError($"[EllipseSwordsSkill] Missing sword sprite at {SkillConfig.FlyingSwordAssetPath}.", this);
                isRunning = false;
                yield break;
            }

            // Hero ĐỨNG YÊN suốt kỹ năng: KHÔNG gọi hero.PlaySkill(), PlayAttackInPlaceWithoutLunge,
            // hay bất kỳ animation/clip nào của hero.
            int activeImpactCount = 0;

            try
            {
                CreateEffectLayer(hero, boss);
                if (effectLayer == null)
                    yield break;

                float layerWidth = effectLayer.rect.width > 1f ? effectLayer.rect.width : ReferenceFrameWidth;
                float frameWidth = layerWidth;
                float frameHeight = frameWidth * (DemoCanvasHeight / DemoCanvasWidth); // 1080×607.5 (tỉ lệ 16:9)

                // Pha 1 — mở 5 cổng elip chụm nghiêng ở góc trên-trái, hiện dần từng cái cách ~0.1s.
                RectTransform[] portalRects = SpawnPortals(portalSprite, frameWidth, frameHeight);
                yield return FadeInPortalsStaggered();

                if (!ShouldStop(boss, shouldStop))
                {
                    // Pha 2 & Pha 3 — 5 kiếm chui ra từ 5 cổng, xoay theo hướng bay tới TorsoAnchor của boss.
                    float swordLength = layerWidth * SwordLengthLayerWidthRatio; // TẠM: 17.5% chiều rộng layer
                    float swordAspect = swordSprite.rect.width > 0f
                        ? swordSprite.rect.height / swordSprite.rect.width
                        : DefaultSwordHeightOverWidth;
                    Vector2 swordSize = new Vector2(swordLength, swordLength * swordAspect);
                    float interval = Mathf.Max(0.02f, launchIntervalSeconds);
                    float flightDuration = Mathf.Max(0.05f, swordFlightSeconds);
                    int swordCount = Mathf.Min(SkillConfig.EllipseSwordsDefaultSwordCount, portalRects.Length);

                    int inFlightSwords = 0;

                    for (int i = 0; i < swordCount; i++)
                    {
                        if (ShouldStop(boss, shouldStop))
                            break;

                        int swordIndex = i;
                        inFlightSwords++;
                        Coroutine swordRoutine = StartCoroutine(RunSingleSword(
                            swordIndex,
                            portalRects[swordIndex],
                            boss,
                            swordSprite,
                            swordSize,
                            flightDuration,
                            shouldStop,
                            onImpact,
                            () => inFlightSwords = Mathf.Max(0, inFlightSwords - 1),
                            () => activeImpactCount++,
                            () => activeImpactCount = Mathf.Max(0, activeImpactCount - 1)));
                        activeSwordRoutines.Add(swordRoutine);

                        if (i + 1 < swordCount)
                        {
                            float waitElapsed = 0f;
                            while (waitElapsed < interval && !ShouldStop(boss, shouldStop))
                            {
                                float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
                                waitElapsed += dt;
                                yield return null;
                            }
                        }
                    }

                    while (inFlightSwords > 0)
                    {
                        if (ShouldStop(boss, shouldStop))
                        {
                            StopAndClearSwords();
                            break;
                        }
                        yield return null;
                    }
                }

                // Xong (kiếm cuối chạm boss hoặc boss chết): 5 cổng mờ dần trong 0.3s.
                yield return FadeOutPortals(PortalFadeOutSeconds);

                // Chờ mọi coroutine impact (boss.Hit / boss.Death) kết thúc sạch trước khi trả lượt.
                while (activeImpactCount > 0)
                    yield return null;
            }
            finally
            {
                StopAndClear();
            }
        }

        RectTransform[] SpawnPortals(Sprite portalSprite, float frameWidth, float frameHeight)
        {
            activePortals.Clear();
            float spriteHeightOverWidth = portalSprite.rect.width > 0f
                ? portalSprite.rect.height / portalSprite.rect.width
                : DefaultPortalHeightOverWidth;

            RectTransform[] portalRects = new RectTransform[PortalSpecs.Length];
            for (int i = 0; i < PortalSpecs.Length; i++)
            {
                PortalSpec spec = PortalSpecs[i];
                float centerX = (spec.DemoX / DemoCanvasWidth) * frameWidth;
                float centerYFromTop = (spec.DemoY / DemoCanvasHeight) * frameHeight;
                float portalWidth = (spec.DemoWidth / DemoCanvasWidth) * frameWidth;
                float portalHeight = portalWidth * spriteHeightOverWidth; // Giữ đúng tỉ lệ sprite 256:148

                GameObject portalObj = new GameObject(
                    $"EllipsePortal_{i + 1}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                RectTransform portalRect = portalObj.GetComponent<RectTransform>();
                portalRect.SetParent(effectLayer, false);
                portalRect.anchorMin = new Vector2(0f, 1f);
                portalRect.anchorMax = new Vector2(0f, 1f);
                portalRect.pivot = new Vector2(0.5f, 0.5f);
                portalRect.sizeDelta = new Vector2(portalWidth, portalHeight);
                portalRect.anchoredPosition = new Vector2(centerX, -centerYFromTop);
                portalRect.localEulerAngles = new Vector3(0f, 0f, spec.TiltDegrees);
                portalRect.localScale = Vector3.one;

                Image portalImage = portalObj.GetComponent<Image>();
                portalImage.sprite = portalSprite;
                portalImage.preserveAspect = true;
                portalImage.raycastTarget = false;
                portalImage.color = new Color(1f, 1f, 1f, 0f);

                activePortals.Add(portalImage);
                portalRects[i] = portalRect;
            }

            return portalRects;
        }

        IEnumerator FadeInPortalsStaggered()
        {
            if (activePortals.Count == 0)
                yield break;

            float totalDuration = (activePortals.Count - 1) * PortalStaggerSeconds + PortalFadeInSeconds;
            float elapsed = 0f;

            while (elapsed < totalDuration && effectLayer != null)
            {
                for (int i = 0; i < activePortals.Count; i++)
                {
                    Image portal = activePortals[i];
                    if (portal == null)
                        continue;

                    float localTime = elapsed - i * PortalStaggerSeconds;
                    float alpha = Mathf.Clamp01(localTime / Mathf.Max(0.01f, PortalFadeInSeconds));
                    portal.color = new Color(1f, 1f, 1f, alpha);
                }

                yield return null;
                float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
                elapsed += dt;
            }

            for (int i = 0; i < activePortals.Count; i++)
            {
                if (activePortals[i] != null)
                    activePortals[i].color = Color.white;
            }
        }

        IEnumerator FadeOutPortals(float duration)
        {
            if (activePortals.Count == 0)
                yield break;

            float safeDuration = Mathf.Max(0.05f, duration);
            float[] initialAlphas = new float[activePortals.Count];
            for (int i = 0; i < activePortals.Count; i++)
                initialAlphas[i] = activePortals[i] != null ? activePortals[i].color.a : 0f;

            float elapsed = 0f;
            while (elapsed < safeDuration && effectLayer != null)
            {
                float t = Mathf.Clamp01(elapsed / safeDuration);
                for (int i = 0; i < activePortals.Count; i++)
                {
                    Image portal = activePortals[i];
                    if (portal == null)
                        continue;

                    portal.color = new Color(1f, 1f, 1f, Mathf.Lerp(initialAlphas[i], 0f, t));
                }

                yield return null;
                float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
                elapsed += dt;
            }

            for (int i = 0; i < activePortals.Count; i++)
            {
                if (activePortals[i] != null)
                    activePortals[i].color = new Color(1f, 1f, 1f, 0f);
            }
        }

        IEnumerator RunSingleSword(
            int swordIndex,
            RectTransform portalRect,
            CharacterView boss,
            Sprite swordSprite,
            Vector2 swordSize,
            float flightDuration,
            Func<bool> shouldStop,
            Func<int, IEnumerator> onImpact,
            Action onSwordFinished,
            Action onImpactStarted,
            Action onImpactFinished)
        {
            GameObject swordObj = null;
            try
            {
                if (effectLayer == null || portalRect == null || ShouldStop(boss, shouldStop))
                    yield break;

                Vector2 startLocal = effectLayer.InverseTransformPoint(portalRect.position);
                Vector2 targetLocal = ResolveBossTargetLocal(boss, new Vector2(270f, 540f));

                swordObj = new GameObject(
                    $"EllipseSword_{swordIndex + 1}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                activeSwords.Add(swordObj);

                RectTransform swordRect = swordObj.GetComponent<RectTransform>();
                swordRect.SetParent(effectLayer, false);
                swordRect.anchorMin = new Vector2(0.5f, 0.5f);
                swordRect.anchorMax = new Vector2(0.5f, 0.5f);
                swordRect.pivot = new Vector2(0.5f, 0.5f);
                swordRect.sizeDelta = swordSize;
                swordRect.anchoredPosition = startLocal;
                swordRect.localScale = Vector3.one * SwordEmergeStartScale;
                ApplyFlightRotation(swordRect, startLocal, targetLocal);

                Image swordImage = swordObj.GetComponent<Image>();
                swordImage.sprite = swordSprite;
                swordImage.preserveAspect = true;
                swordImage.raycastTarget = false;
                swordImage.color = Color.white;

                // Kiếm chui ra từ tâm cổng: lớn dần từ ~30% tới 100% trong ~0.13s.
                float emergeElapsed = 0f;
                while (emergeElapsed < SwordEmergeSeconds && swordObj != null)
                {
                    if (ShouldStop(boss, shouldStop))
                        yield break;

                    float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
                    emergeElapsed += dt;
                    float t = Mathf.Clamp01(emergeElapsed / SwordEmergeSeconds);
                    float scale = Mathf.Lerp(SwordEmergeStartScale, 1f, t);
                    swordRect.localScale = new Vector3(scale, scale, 1f);
                    targetLocal = ResolveBossTargetLocal(boss, targetLocal);
                    ApplyFlightRotation(swordRect, startLocal, targetLocal);
                    yield return null;
                }

                if (swordObj == null || ShouldStop(boss, shouldStop))
                    yield break;

                swordRect.localScale = Vector3.one;

                // Kiếm bay chéo tới TorsoAnchor của boss trong ~0.50s, xoay theo góc đường bay.
                float flightElapsed = 0f;
                while (flightElapsed < flightDuration && swordObj != null)
                {
                    if (ShouldStop(boss, shouldStop))
                        yield break;

                    float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
                    flightElapsed += dt;
                    float progress = Mathf.Clamp01(flightElapsed / flightDuration);
                    targetLocal = ResolveBossTargetLocal(boss, targetLocal);
                    swordRect.anchoredPosition = Vector2.Lerp(startLocal, targetLocal, progress);
                    ApplyFlightRotation(swordRect, startLocal, targetLocal);
                    yield return null;
                }

                if (swordObj == null || ShouldStop(boss, shouldStop))
                    yield break;

                DestroySwordObject(swordObj);
                swordObj = null;

                if (onImpact != null)
                {
                    onImpactStarted?.Invoke();
                    StartCoroutine(RunImpactRoutine(onImpact, swordIndex, onImpactFinished));
                }
            }
            finally
            {
                if (swordObj != null)
                    DestroySwordObject(swordObj);
                onSwordFinished?.Invoke();
            }
        }

        static IEnumerator RunImpactRoutine(
            Func<int, IEnumerator> onImpact,
            int swordIndex,
            Action onFinished)
        {
            try
            {
                if (onImpact != null)
                    yield return onImpact(swordIndex);
            }
            finally
            {
                onFinished?.Invoke();
            }
        }

        static void ApplyFlightRotation(RectTransform swordRect, Vector2 startLocal, Vector2 targetLocal)
        {
            Vector2 delta = targetLocal - startLocal;
            if (delta.sqrMagnitude > 0.0001f)
            {
                float angleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                swordRect.localEulerAngles = new Vector3(0f, 0f, angleDeg);
            }
        }

        static bool ShouldStop(CharacterView boss, Func<bool> shouldStop)
        {
            if (shouldStop != null && shouldStop())
                return true;
            return boss == null
                || boss.State == CharacterView.CharacterState.Death
                || !boss.gameObject.activeInHierarchy;
        }

        void DestroySwordObject(GameObject swordObj)
        {
            if (swordObj == null)
                return;
            activeSwords.Remove(swordObj);
            Destroy(swordObj);
        }

        void StopAndClearSwords()
        {
            for (int i = 0; i < activeSwordRoutines.Count; i++)
            {
                if (activeSwordRoutines[i] != null)
                    StopCoroutine(activeSwordRoutines[i]);
            }
            activeSwordRoutines.Clear();

            for (int i = activeSwords.Count - 1; i >= 0; i--)
            {
                if (activeSwords[i] != null)
                    Destroy(activeSwords[i]);
            }
            activeSwords.Clear();
        }

        Vector2 ResolveBossTargetLocal(CharacterView boss, Vector2 fallback)
        {
            if (boss == null || effectLayer == null)
                return fallback;

            RectTransform targetAnchor = boss.TorsoAnchor != null
                ? boss.TorsoAnchor
                : (boss.VFXAnchor != null
                    ? boss.VFXAnchor
                    : (boss.VisualRect != null ? boss.VisualRect : boss.transform as RectTransform));
            if (targetAnchor == null)
                return fallback;

            return effectLayer.InverseTransformPoint(targetAnchor.position);
        }

        void CreateEffectLayer(CharacterView hero, CharacterView boss)
        {
            if (effectLayer != null)
            {
                Destroy(effectLayer.gameObject);
                effectLayer = null;
            }

            Transform characterLayer = null;
            if (hero != null && hero.transform.parent != null)
                characterLayer = hero.transform.parent.parent;
            else if (boss != null && boss.transform.parent != null)
                characterLayer = boss.transform.parent.parent;

            RectTransform parentRect = characterLayer as RectTransform;
            if (parentRect == null)
                return;

            GameObject layerObj = new GameObject("EllipseSwordsLayer", typeof(RectTransform));
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
