using System;
using System.Threading;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Board
{
    public sealed class LightningLineVFX : MaskableGraphic
    {
        const int PointCount = 13;
        public const float ActivationDelay = .03f;
        public const float FlickerIntervalMin = .025f;
        public const float FlickerIntervalMax = .05f;
        public const float TotalLifetime = .34f;
        public const float BoltSpacingInCells = .11f;
        static int nextSeed;

        readonly Vector2[] points = new Vector2[PointCount];
        System.Random random;
        float cellSize;
        float duration;
        float elapsed;
        float nextFlicker;
        float intensity;
        bool horizontal;
        bool playing;

        public int PathPointCount => PointCount;
        public bool IsPlaying => playing;
        public float Lifetime => duration;
        public float GlowWidth => Mathf.Clamp(cellSize * .26f, 20f, 30f);
        public float MainWidth => Mathf.Clamp(cellSize * .16f, 10f, 16f);
        public float CoreWidth => Mathf.Clamp(cellSize * .07f, 4f, 7f);
        public float MaximumDeviation => cellSize * .12f;

#if UNITY_EDITOR
        public Vector2 GetPointForDebug(int index) => points[index];
#endif

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            random = new System.Random(unchecked(Environment.TickCount + Interlocked.Increment(ref nextSeed)));
            enabled = false;
        }

        public void Play(Vector2 start, Vector2 end, bool isHorizontal, float gemCellSize, float lifetime)
        {
            horizontal = isHorizontal;
            cellSize = Mathf.Max(1f, gemCellSize);
            duration = Mathf.Max(.2f, lifetime);
            elapsed = 0f;
            nextFlicker = 0f;
            intensity = 0f;
            playing = true;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
            color = Color.white;
            raycastTarget = false;
            enabled = true;
            gameObject.SetActive(true);
            RegeneratePath(start, end);
            SetVerticesDirty();
        }

        public void SetUniqueSeed(int seed)
        {
            random = new System.Random(seed);
        }

        public void Stop()
        {
            playing = false;
            intensity = 0f;
            enabled = false;
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        public void RegeneratePath(Vector2 start, Vector2 end)
        {
            if (random == null)
                random = new System.Random(unchecked(Environment.TickCount + Interlocked.Increment(ref nextSeed)));

            for (int index = 0; index < PointCount; index++)
            {
                float progress = index / (float)(PointCount - 1);
                Vector2 point = Vector2.Lerp(start, end, progress);
                if (index > 0 && index < PointCount - 1)
                {
                    float offset = ((float)random.NextDouble() * 2f - 1f) * MaximumDeviation;
                    if (horizontal)
                        point.y += offset;
                    else
                        point.x += offset;
                }
                points[index] = point;
            }
            SetVerticesDirty();
        }

        public IEnumerator Animate(Vector2 start, Vector2 end, bool isHorizontal,
            float gemCellSize, float lifetime)
        {
            Play(start, end, isHorizontal, gemCellSize, lifetime);
            yield return new WaitForSecondsRealtime(ActivationDelay);
            elapsed = ActivationDelay;
            intensity = 1f;
            nextFlicker = elapsed;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= nextFlicker)
                {
                    RegeneratePath(start, end);
                    intensity = random.NextDouble() < .22
                        ? .35f
                        : .78f + (float)random.NextDouble() * .22f;
                    nextFlicker = elapsed + FlickerIntervalMin
                        + (float)random.NextDouble() * (FlickerIntervalMax - FlickerIntervalMin);
                }

                float fade = Mathf.InverseLerp(duration, .18f, elapsed);
                SetAlpha(Mathf.Min(intensity, fade));
                yield return null;
            }

            Stop();
        }

        void SetAlpha(float alpha)
        {
            intensity = alpha;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (!playing || points.Length < 2 || intensity <= 0f)
                return;

            AddRibbon(vertexHelper, GlowWidth, new Color32(40, 205, 255, 78));
            AddRibbon(vertexHelper, MainWidth, new Color32(125, 235, 255, 220));
            AddRibbon(vertexHelper, CoreWidth, new Color32(255, 255, 255, 255));
        }

        void AddRibbon(VertexHelper vertexHelper, float width, Color tint)
        {
            int vertexOffset = vertexHelper.currentVertCount;
            Color color = tint;
            color.a *= intensity;

            for (int index = 0; index < PointCount; index++)
            {
                Vector2 previous = points[Mathf.Max(0, index - 1)];
                Vector2 next = points[Mathf.Min(PointCount - 1, index + 1)];
                Vector2 tangent = (next - previous).normalized;
                Vector2 normal = new Vector2(-tangent.y, tangent.x) * (width * .5f);
                UIVertex first = UIVertex.simpleVert;
                first.color = color;
                first.position = points[index] - normal;
                vertexHelper.AddVert(first);

                UIVertex second = UIVertex.simpleVert;
                second.color = color;
                second.position = points[index] + normal;
                vertexHelper.AddVert(second);
            }

            for (int index = 0; index < PointCount - 1; index++)
            {
                int start = vertexOffset + index * 2;
                vertexHelper.AddTriangle(start, start + 1, start + 2);
                vertexHelper.AddTriangle(start + 1, start + 3, start + 2);
            }
        }
    }
}
