using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Board
{
    public sealed class AreaExplosionVFX : MaskableGraphic
    {
        const int ShockwaveSegments = 32;
        const int RayCount = 10;
        const int MaximumCellFlashes = 9;
        const float FlashSeconds = .12f;
        const float CellFlashSeconds = .14f;
        static int nextSeed;

        readonly Vector2[] cellCenters = new Vector2[MaximumCellFlashes];
        readonly float[] rayAngles = new float[RayCount];
        readonly float[] rayLengths = new float[RayCount];
        System.Random random;
        Vector2 center;
        Vector2 cellSize;
        int cellFlashCount;
        float lifetime;
        float elapsed;
        bool playing;

        public int ShockwaveCount => 2;
        public int RadialRayCount => RayCount;
        public int CellFlashCount => cellFlashCount;
        public bool IsPlaying => playing;

#if UNITY_EDITOR
        public Vector2 GetCellCenterForDebug(int index) => cellCenters[index];
#endif

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            random = new System.Random(unchecked(Environment.TickCount + Interlocked.Increment(ref nextSeed)));
            for (int index = 0; index < RayCount; index++)
            {
                rayAngles[index] = (index + (float)random.NextDouble() * .45f)
                    * Mathf.PI * 2f / RayCount;
                rayLengths[index] = .38f + (float)random.NextDouble() * .3f;
            }
            enabled = false;
        }

        public void Play(Vector2 explosionCenter, Vector2 gemCellSize,
            Vector2[] validCellCenters, float duration)
        {
            center = explosionCenter;
            cellSize = gemCellSize;
            lifetime = Mathf.Max(.25f, duration);
            elapsed = 0f;
            cellFlashCount = Mathf.Min(validCellCenters != null ? validCellCenters.Length : 0,
                MaximumCellFlashes);
            for (int index = 0; index < cellFlashCount; index++)
                cellCenters[index] = validCellCenters[index];
            playing = true;
            color = Color.white;
            raycastTarget = false;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
            enabled = true;
            gameObject.SetActive(true);
            SetVerticesDirty();
        }

        public void Stop()
        {
            playing = false;
            enabled = false;
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        public IEnumerator Animate(Vector2 explosionCenter, Vector2 gemCellSize,
            Vector2[] validCellCenters, float duration)
        {
            Play(explosionCenter, gemCellSize, validCellCenters, duration);
            while (elapsed < lifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                SetVerticesDirty();
                yield return null;
            }
            Stop();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (!playing)
                return;

            float cell = Mathf.Max(1f, Mathf.Min(cellSize.x, cellSize.y));
            float flash = Mathf.Clamp01(elapsed / .035f)
                * (1f - Mathf.Clamp01((elapsed - .035f) / (FlashSeconds - .035f)));
            float smokeProgress = Mathf.Clamp01((elapsed - .035f) / .12f);
            float smokeAlpha = smokeProgress * (1f - Mathf.Clamp01(elapsed / lifetime));
            for (int index = 0; index < 6; index++)
            {
                float angle = index * Mathf.PI * 2f / 6f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 puff = center + direction * cell * (.27f + smokeProgress * .28f);
                AddSoftDisc(vertexHelper, puff, cell * .20f,
                    new Color(.22f, .20f, .19f, .30f * smokeAlpha));
            }

            AddSoftDisc(vertexHelper, center,
                cell * Mathf.Lerp(.82f, 1.02f, Mathf.Clamp01(elapsed / .10f)),
                new Color(1f, .20f, .025f, .74f * flash));
            AddSoftDisc(vertexHelper, center, cell * .55f,
                new Color(1f, .50f, .045f, .88f * flash));
            AddDisc(vertexHelper, center, cell * .24f,
                new Color(1f, .84f, .20f, .94f * flash), 24);

            for (int index = 0; index < cellFlashCount; index++)
            {
                float cellFlash = Mathf.Clamp01(elapsed / .04f)
                    * (1f - Mathf.Clamp01((elapsed - .04f) / (CellFlashSeconds - .04f)));
                if (cellFlash <= 0f) continue;
                AddQuad(vertexHelper, cellCenters[index], cellSize * .84f,
                    new Color(1f, .64f, .20f, .56f * cellFlash));
            }

            float shockProgress = Mathf.Clamp01((elapsed - .075f) / Mathf.Max(.01f, lifetime - .075f));
            float shockAlpha = (1f - shockProgress) * (elapsed >= .075f ? 1f : 0f);
            float maxRadius = cell * 1.43f;
            float radius = maxRadius * Mathf.Lerp(.25f, 1f, shockProgress);
            AddRing(vertexHelper, center, radius, cell * .045f,
                new Color(1f, .55f, .10f, .90f * shockAlpha));
            AddRing(vertexHelper, center, radius * .78f, cell * .025f,
                new Color(1f, .88f, .30f, .92f * shockAlpha));

            float rayProgress = Mathf.Clamp01(elapsed / .21f);
            float rayAlpha = 1f - rayProgress;
            for (int index = 0; index < RayCount; index++)
            {
                float angle = rayAngles[index];
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float startRadius = cell * (.18f + rayProgress * .28f);
                float endRadius = startRadius + cell * rayLengths[index] * rayProgress;
                Vector2 start = center + direction * startRadius;
                Vector2 end = center + direction * endRadius;
                AddRay(vertexHelper, start, end, Mathf.Clamp(cell * .035f, 2f, 5f),
                    new Color(1f, .78f, .18f, .88f * rayAlpha));
            }
        }

        static void AddDisc(VertexHelper vertexHelper, Vector2 origin, float radius,
            Color tint, int segments)
        {
            int centerIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, origin, tint);
            for (int index = 0; index <= segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                AddVertex(vertexHelper, origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint);
            }
            for (int index = 0; index < segments; index++)
                vertexHelper.AddTriangle(centerIndex, centerIndex + index + 1, centerIndex + index + 2);
        }

        static void AddSoftDisc(VertexHelper vertexHelper, Vector2 origin, float radius, Color tint)
        {
            int centerIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, origin, tint);
            Color edge = tint;
            edge.a = 0f;
            const int segments = 20;
            for (int index = 0; index <= segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                Vector2 point = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                AddVertex(vertexHelper, point, edge);
            }
            for (int index = 0; index < segments; index++)
                vertexHelper.AddTriangle(centerIndex, centerIndex + index + 1, centerIndex + index + 2);
        }

        static void AddRing(VertexHelper vertexHelper, Vector2 origin, float radius,
            float thickness, Color tint)
        {
            int startIndex = vertexHelper.currentVertCount;
            float innerRadius = Mathf.Max(0f, radius - thickness * .5f);
            float outerRadius = radius + thickness * .5f;
            for (int index = 0; index <= ShockwaveSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / ShockwaveSegments;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddVertex(vertexHelper, origin + direction * innerRadius, tint);
                AddVertex(vertexHelper, origin + direction * outerRadius, tint);
            }
            for (int index = 0; index < ShockwaveSegments; index++)
            {
                int current = startIndex + index * 2;
                vertexHelper.AddTriangle(current, current + 1, current + 2);
                vertexHelper.AddTriangle(current + 1, current + 3, current + 2);
            }
        }

        static void AddRay(VertexHelper vertexHelper, Vector2 start, Vector2 end,
            float width, Color tint)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * (width * .5f);
            int startIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, start - normal, tint);
            AddVertex(vertexHelper, start + normal, tint);
            AddVertex(vertexHelper, end - normal, tint);
            AddVertex(vertexHelper, end + normal, tint);
            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex + 1, startIndex + 3, startIndex + 2);
        }

        static void AddQuad(VertexHelper vertexHelper, Vector2 center, Vector2 size, Color tint)
        {
            int startIndex = vertexHelper.currentVertCount;
            Vector2 halfSize = size * .5f;
            AddVertex(vertexHelper, center + new Vector2(-halfSize.x, -halfSize.y), tint);
            AddVertex(vertexHelper, center + new Vector2(-halfSize.x, halfSize.y), tint);
            AddVertex(vertexHelper, center + new Vector2(halfSize.x, halfSize.y), tint);
            AddVertex(vertexHelper, center + new Vector2(halfSize.x, -halfSize.y), tint);
            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }

        static void AddVertex(VertexHelper vertexHelper, Vector2 position, Color tint)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = tint;
            vertexHelper.AddVert(vertex);
        }
    }
}
