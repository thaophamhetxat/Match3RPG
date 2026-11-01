using System;
using System.Collections;
using System.Threading;
using Match3RPG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Board
{
    public sealed class SpecialGemIdleVFX : MaskableGraphic
    {
        const int RingSegments = 40;
        const int EdgeFlameSegments = 20;
        const int MaximumSparkles = 4;
        const float CreationFlashDuration = .24f;
        const float SparkleLifetimeMin = .25f;
        const float SparkleLifetimeMax = .45f;
        const float SparkleDelayMin = .15f;
        const float SparkleDelayMax = .40f;
        const float FlickerIntervalMin = .08f;
        const float FlickerIntervalMax = .16f;
        static int nextSeed;

        struct Sparkle
        {
            public Vector2 Position;
            public float Age;
            public float Lifetime;
            public float Size;
            public bool Active;
        }

        readonly Sparkle[] sparkles = new Sparkle[MaximumSparkles];
        System.Random random;
        Coroutine effectRoutine;
        GemSpecialType specialType;
        float auraAlpha;
        float baseAuraAlpha;
        float auraScale = 1f;
        float auraScaleX = 1f;
        float auraScaleY = 1f;
        float auraRotation;
        float creationFlashAlpha;
        int sparkleTarget;
        float randomPhase;
        float nextFlickerTime;
        float targetAuraAlpha;
        float targetScaleX = 1f;
        float targetScaleY = 1f;
        float targetRotation;

        public GemSpecialType SpecialType => specialType;
        public int ActiveSparkleCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < sparkles.Length; index++)
                    if (sparkles[index].Active) count++;
                return count;
            }
        }
        public bool IsIdleEffectRunning => effectRoutine != null;
        public bool IsCreationFlashPlaying => creationFlashAlpha > 0f;
        public float AuraAlpha => auraAlpha;
        public float AuraScale => auraScale;
        public int IntensityLevel => GetIntensityLevel(specialType);
        public int CreationFlashCount { get; private set; }
        public int IdleStartRequestCount { get; private set; }

        public static Vector2 GetRoundedSquarePointForDebug(float angle)
        {
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return GetRoundedSquarePoint(direction, 1f, 1f);
        }

        public static float GetAuraAlphaForDebug(GemSpecialType type)
        {
            GetStyle(type, out float alpha, out _, out _);
            return alpha;
        }

        public static int GetSparkleTargetForDebug(GemSpecialType type)
        {
            GetStyle(type, out _, out int count, out _);
            return count;
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            random = new System.Random(unchecked(Environment.TickCount * 397
                ^ Interlocked.Increment(ref nextSeed)));
            randomPhase = (float)random.NextDouble() * 100f;
            enabled = false;
        }

        public void SetSpecialType(GemSpecialType type)
        {
            if (specialType == type)
                return;

            StopIdleEffect();
            specialType = type;
            if (type != GemSpecialType.None)
                StartIdleEffect();
        }

        public void PlayCreationFlash()
        {
            if (specialType == GemSpecialType.None)
                return;

            CreationFlashCount++;
            if (!Application.isPlaying)
                return;

            if (effectRoutine != null)
                StopCoroutine(effectRoutine);
            enabled = true;
            effectRoutine = StartCoroutine(RunEffects(true));
        }

        public void StartIdleEffect()
        {
            if (specialType == GemSpecialType.None || effectRoutine != null)
                return;
            IdleStartRequestCount++;
            if (!Application.isPlaying)
                return;
            enabled = true;
            effectRoutine = StartCoroutine(RunEffects(false));
        }

        public void StopIdleEffect()
        {
            if (effectRoutine != null)
            {
                StopCoroutine(effectRoutine);
                effectRoutine = null;
            }
            enabled = false;
            auraAlpha = 0f;
            creationFlashAlpha = 0f;
            auraScale = 1f;
            auraScaleX = auraScaleY = 1f;
            auraRotation = 0f;
            targetRotation = 0f;
            targetScaleX = targetScaleY = 1f;
            targetAuraAlpha = 0f;
            sparkleTarget = 0;
            baseAuraAlpha = 0f;
            for (int index = 0; index < sparkles.Length; index++)
                sparkles[index].Active = false;
            SetVerticesDirty();
        }

        IEnumerator RunEffects(bool playCreationFlash)
        {
            enabled = true;
            raycastTarget = false;
            gameObject.SetActive(true);
            GetStyle(specialType, out baseAuraAlpha, out sparkleTarget, out _);
            auraAlpha = baseAuraAlpha;
            targetAuraAlpha = baseAuraAlpha;
            nextFlickerTime = 0f;
            if (playCreationFlash)
                yield return PlayFlash();

            float sparkleDelay = RandomRange(SparkleDelayMin, SparkleDelayMax);
            while (specialType != GemSpecialType.None)
            {
                float deltaTime = Time.unscaledDeltaTime;
                UpdateSparkles(deltaTime);
                sparkleDelay -= deltaTime;
                if (sparkleDelay <= 0f)
                {
                    SpawnSparkle();
                    sparkleDelay = RandomRange(SparkleDelayMin, SparkleDelayMax);
                }

                if (Time.unscaledTime >= nextFlickerTime)
                {
                    float flicker = (float)random.NextDouble();
                    targetAuraAlpha = Mathf.Min(specialType == GemSpecialType.AreaBomb ? .38f : 1f,
                        baseAuraAlpha * Mathf.Lerp(.85f, 1.12f, flicker));
                    targetScaleX = Mathf.Lerp(.99f, 1.04f, flicker);
                    targetScaleY = Mathf.Lerp(1f, 1.08f, (float)random.NextDouble());
                    targetRotation = Mathf.Lerp(-2f, 2f, (float)random.NextDouble());
                    nextFlickerTime = Time.unscaledTime
                        + RandomRange(FlickerIntervalMin, FlickerIntervalMax);
                }
                float smoothing = 1f - Mathf.Exp(-deltaTime * 9f);
                auraAlpha = Mathf.Lerp(auraAlpha, targetAuraAlpha, smoothing);
                auraScaleX = Mathf.Lerp(auraScaleX, targetScaleX, smoothing);
                auraScaleY = Mathf.Lerp(auraScaleY, targetScaleY, smoothing);
                auraRotation = Mathf.Lerp(auraRotation, targetRotation, smoothing);
                float pulse = specialType == GemSpecialType.AreaBomb
                    ? 1f + .04f * (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / .85f) + 1f) * .5f
                    : specialType == GemSpecialType.Rainbow
                        ? 1f + .035f * (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / .92f) + 1f) * .5f
                        : 1f;
                auraScale = pulse;
                SetVerticesDirty();
                yield return null;
            }

            effectRoutine = null;
            StopIdleEffect();
        }

        IEnumerator PlayFlash()
        {
            for (int index = 0; index < 4; index++)
                SpawnSparkle(true);
            float elapsed = 0f;
            while (elapsed < CreationFlashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                UpdateSparkles(Time.unscaledDeltaTime);
                float progress = Mathf.Clamp01(elapsed / CreationFlashDuration);
                float pulse = progress < .45f
                    ? Mathf.Lerp(.4f, 1.15f, progress / .45f)
                    : Mathf.Lerp(1.15f, 1f, (progress - .45f) / .55f);
                auraScale = pulse;
                creationFlashAlpha = progress < .45f
                    ? Mathf.Lerp(0f, .9f, progress / .45f)
                    : Mathf.Lerp(.9f, auraAlpha, (progress - .45f) / .55f);
                auraScaleX = auraScaleY = 1f;
                SetVerticesDirty();
                yield return null;
            }
            creationFlashAlpha = 0f;
        }

        void UpdateSparkles(float deltaTime)
        {
            for (int index = 0; index < sparkles.Length; index++)
            {
                if (!sparkles[index].Active)
                    continue;
                Sparkle sparkle = sparkles[index];
                sparkle.Age += deltaTime;
                if (sparkle.Age >= sparkle.Lifetime)
                    sparkle.Active = false;
                sparkles[index] = sparkle;
            }
        }

        void SpawnSparkle(bool force = false)
        {
            int currentCount = ActiveSparkleCount;
            if (currentCount >= sparkles.Length || (!force && currentCount >= sparkleTarget))
                return;

            for (int index = 0; index < sparkles.Length; index++)
            {
                if (sparkles[index].Active)
                    continue;
                float halfSize = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
                float angle = RandomRange(0f, Mathf.PI * 2f);
                Vector2 edgePoint = GetRoundedSquarePoint(
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), auraScaleX, auraScaleY);
                sparkles[index] = new Sparkle
                {
                    Position = edgePoint * halfSize * RandomRange(.82f, .90f),
                    Age = 0f,
                    Lifetime = RandomRange(SparkleLifetimeMin, SparkleLifetimeMax),
                    Size = halfSize * RandomRange(.035f, .065f),
                    Active = true
                };
                return;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (specialType == GemSpecialType.None || rectTransform.rect.width <= 0f)
                return;

            GetStyle(specialType, out _, out _, out Color tint);
            float alpha = Mathf.Max(auraAlpha, creationFlashAlpha);
            if (alpha <= 0f)
                return;

            float halfSize = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
            float scaledHalfSize = halfSize * auraScale;
            AddGlowBand(vertexHelper, scaledHalfSize, alpha, tint);
            AddFlameEdge(vertexHelper, scaledHalfSize, alpha, tint);
            for (int index = 0; index < sparkles.Length; index++)
                if (sparkles[index].Active)
                    AddSparkle(vertexHelper, sparkles[index], alpha, tint);
        }

        void AddGlowBand(VertexHelper vertexHelper, float halfSize, float alpha, Color tint)
        {
            int start = vertexHelper.currentVertCount;
            Color outer = WithAlpha(tint, alpha * .08f);
            Color inner = WithAlpha(tint, alpha * .42f);
            float rotation = auraRotation * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rotation);
            float cos = Mathf.Cos(rotation);
            for (int index = 0; index <= RingSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / RingSegments;
                float noise = Mathf.PerlinNoise(index * .31f + randomPhase,
                    Time.unscaledTime * 2.2f);
                float flicker = Mathf.Clamp01((noise - .18f) * 1.55f);
                float wobble = (noise - .5f) * .025f;
                float maximumScale = Mathf.Max(auraScaleX, auraScaleY);
                float scale = Mathf.Min(1.035f + wobble,
                    1.07f / (maximumScale * auraScale));
                Vector2 direction = GetRoundedSquarePoint(
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), auraScaleX, auraScaleY);
                AddVertex(vertexHelper, RotatePoint(direction * halfSize * (scale - .045f), sin, cos),
                    WithAlpha(inner, inner.a * flicker));
                AddVertex(vertexHelper, RotatePoint(direction * halfSize * scale, sin, cos),
                    WithAlpha(outer, outer.a * flicker));
            }

            for (int index = 0; index < RingSegments; index++)
            {
                int current = start + index * 2;
                vertexHelper.AddTriangle(current, current + 1, current + 2);
                vertexHelper.AddTriangle(current + 1, current + 3, current + 2);
            }
        }

        void AddFlameEdge(VertexHelper vertexHelper, float halfSize, float alpha, Color tint)
        {
            float rotation = auraRotation * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rotation);
            float cos = Mathf.Cos(rotation);
            float maximumScale = Mathf.Max(auraScaleX, auraScaleY);
            for (int index = 0; index < EdgeFlameSegments; index++)
            {
                float angle = (index + .5f) * Mathf.PI * 2f / EdgeFlameSegments;
                float noise = Mathf.PerlinNoise(index * .47f + randomPhase,
                    Time.unscaledTime * 2.6f);
                float length = halfSize * Mathf.Lerp(.055f, .115f, noise);
                float width = halfSize * Mathf.Lerp(.035f, .065f,
                    Mathf.PerlinNoise(index * .63f + randomPhase, 1.7f));
                float radius = Mathf.Min(.88f + noise * .035f,
                    1.07f / (maximumScale * auraScale) - .12f);
                Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tangent = new Vector2(-radial.y, radial.x);
                Vector2 shape = GetRoundedSquarePoint(radial, auraScaleX, auraScaleY);
                Vector2 root = shape * halfSize * radius;
                Vector2 tip = shape * halfSize * (radius + length / halfSize);
                Vector2 shoulderLeft = root - tangent * width * .42f
                    + radial * length * .52f;
                Vector2 shoulderRight = root + tangent * width * .42f
                    + radial * length * .52f;
                Vector2 baseLeft = root - tangent * width * .5f;
                Vector2 baseRight = root + tangent * width * .5f;
                Vector2 middleLeft = root - tangent * width * .28f
                    + radial * length * .78f;
                Vector2 middleRight = root + tangent * width * .28f
                    + radial * length * .78f;
                Vector2 center = root + radial * length * .48f;
                int start = vertexHelper.currentVertCount;
                AddVertex(vertexHelper, RotatePoint(center, sin, cos),
                    WithAlpha(tint, alpha * (.16f + noise * .10f)));
                AddVertex(vertexHelper, RotatePoint(baseLeft, sin, cos), WithAlpha(tint, alpha * .08f));
                AddVertex(vertexHelper, RotatePoint(shoulderLeft, sin, cos), WithAlpha(tint, alpha * .22f));
                AddVertex(vertexHelper, RotatePoint(middleLeft, sin, cos), WithAlpha(tint, alpha * .30f));
                AddVertex(vertexHelper, RotatePoint(tip, sin, cos), WithAlpha(Color.white, alpha * .48f));
                AddVertex(vertexHelper, RotatePoint(middleRight, sin, cos), WithAlpha(tint, alpha * .30f));
                AddVertex(vertexHelper, RotatePoint(shoulderRight, sin, cos), WithAlpha(tint, alpha * .22f));
                AddVertex(vertexHelper, RotatePoint(baseRight, sin, cos), WithAlpha(tint, alpha * .08f));
                for (int point = 0; point < 7; point++)
                    vertexHelper.AddTriangle(start, start + point + 1,
                        start + (point + 1) % 7 + 1);
            }
        }

        void AddSparkle(VertexHelper vertexHelper, Sparkle sparkle, float alpha, Color tint)
        {
            float progress = Mathf.Clamp01(sparkle.Age / sparkle.Lifetime);
            float flash = Mathf.Sin(progress * Mathf.PI);
            float size = sparkle.Size * flash;
            if (size <= 0f)
                return;

            Color white = Color.Lerp(Color.white, tint, .22f);
            white.a = alpha * (.35f + flash * .65f);
            float diagonal = size * .28f;
            int center = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, sparkle.Position, white);
            for (int index = 0; index < 8; index++)
            {
                float angle = index * Mathf.PI / 4f - Mathf.PI * .5f;
                float pointRadius = (index & 1) == 0 ? size : diagonal;
                AddVertex(vertexHelper, sparkle.Position
                    + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * pointRadius, white);
            }
            for (int index = 0; index < 8; index++)
                vertexHelper.AddTriangle(center, center + index + 1, center + (index + 1) % 8 + 1);
        }

        static int GetIntensityLevel(GemSpecialType type)
        {
            switch (type)
            {
                case GemSpecialType.Line: return 1;
                case GemSpecialType.AreaBomb: return 2;
                case GemSpecialType.Rainbow: return 3;
                default: return 0;
            }
        }

        static void GetStyle(GemSpecialType type, out float alpha, out int sparklesCount, out Color tint)
        {
            switch (type)
            {
                case GemSpecialType.Line:
                    alpha = .24f;
                    sparklesCount = 2;
                    tint = new Color32(135, 240, 255, 255);
                    break;
                case GemSpecialType.AreaBomb:
                    alpha = .34f;
                    sparklesCount = 3;
                    tint = new Color32(192, 112, 255, 255);
                    break;
                case GemSpecialType.Rainbow:
                    alpha = .46f;
                    sparklesCount = 4;
                    tint = Color.Lerp(new Color32(95, 224, 255, 255),
                        new Color32(255, 175, 250, 255),
                        (Mathf.Sin(Time.unscaledTime * 1.2f) + 1f) * .5f);
                    break;
                default:
                    alpha = 0f;
                    sparklesCount = 0;
                    tint = Color.white;
                    break;
            }
        }

        static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        static Vector2 RotatePoint(Vector2 point, float sin, float cos) =>
            new Vector2(point.x * cos - point.y * sin,
                point.x * sin + point.y * cos);

        static Vector2 GetRoundedSquarePoint(Vector2 direction, float scaleX, float scaleY)
        {
            const float exponent = .38f;
            return new Vector2(
                Mathf.Sign(direction.x) * Mathf.Pow(Mathf.Abs(direction.x), exponent) * scaleX,
                Mathf.Sign(direction.y) * Mathf.Pow(Mathf.Abs(direction.y), exponent) * scaleY);
        }

        static void AddVertex(VertexHelper vertexHelper, Vector2 position, Color color)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertexHelper.AddVert(vertex);
        }

        float RandomRange(float minimum, float maximum) =>
            minimum + (float)random.NextDouble() * (maximum - minimum);
    }
}
