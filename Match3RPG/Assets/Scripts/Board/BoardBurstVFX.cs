using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Board
{
    public enum BoardBurstStyle
    {
        Fire,
        Water,
        Lotus,
        Sun,
        Leaf,
        ShieldShards,
        Soul,
        WhiteBlueFlash,
        Electric,
        RainbowBoard,
        BaguaClouds,
        RunicAura
    }

    public sealed class BoardBurstVFX : MaskableGraphic
    {
        const int PointCount = 13;

        public readonly struct Burst
        {
            public Vector2 Position { get; }
            public float Size { get; }
            public BoardBurstStyle Style { get; }
            public int Seed { get; }
            public Color Tint { get; }

            public Burst(Vector2 position, float size, BoardBurstStyle style,
                int seed = 0, Color? tint = null)
            {
                Position = position;
                Size = Mathf.Max(1f, size);
                Style = style;
                Seed = seed;
                Tint = tint ?? Color.white;
            }
        }

        public readonly struct Arc
        {
            public Vector2 Start { get; }
            public Vector2 End { get; }
            public float CellSize { get; }
            public int Seed { get; }

            public Arc(Vector2 start, Vector2 end, float cellSize, int seed = 0)
            {
                Start = start;
                End = end;
                CellSize = Mathf.Max(1f, cellSize);
                Seed = seed;
            }
        }

        readonly List<Burst> bursts = new List<Burst>();
        readonly List<Arc> arcs = new List<Arc>();
        readonly Vector2[] arcPoints = new Vector2[PointCount];
        float duration;
        float elapsed;

        public bool IsPlaying { get; private set; }
        public int BurstCount => bursts.Count;
        public int ArcCount => arcs.Count;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            enabled = false;
        }

        public void AddBurst(Vector2 position, float size, BoardBurstStyle style, int seed = 0)
        {
            bursts.Add(new Burst(position, size, style, seed));
        }

        public void AddBurst(Vector2 position, float size, BoardBurstStyle style,
            int seed, Color tint)
        {
            bursts.Add(new Burst(position, size, style, seed, tint));
        }

        public void AddArc(Vector2 start, Vector2 end, float cellSize, int seed = 0)
        {
            arcs.Add(new Arc(start, end, cellSize, seed));
        }

        public void Play(float lifetime)
        {
            duration = Mathf.Max(.12f, lifetime);
            elapsed = 0f;
            IsPlaying = true;
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
            IsPlaying = false;
            enabled = false;
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= duration)
            {
                Stop();
                return;
            }
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (!IsPlaying)
                return;

            float fade = 1f - Mathf.Clamp01(elapsed / duration);
            for (int index = 0; index < bursts.Count; index++)
                DrawBurst(vertexHelper, bursts[index], fade);
            for (int index = 0; index < arcs.Count; index++)
                DrawArc(vertexHelper, arcs[index], fade, index);
        }

        void DrawBurst(VertexHelper vh, Burst burst, float fade)
        {
            int firstVertex = vh.currentVertCount;
            float size = burst.Size;
            Vector2 center = burst.Position;
            float flashFade = fade * (1f - Mathf.Clamp01(elapsed / (duration * .58f)));
            float ringProgress = Mathf.Clamp01(elapsed / (duration * .72f));
            float rotation = (burst.Seed % 31) * .17f;

            switch (burst.Style)
            {
                case BoardBurstStyle.Fire:
                    DrawFire(vh, center, size, fade, flashFade, ringProgress, rotation);
                    break;
                case BoardBurstStyle.Water:
                    DrawWater(vh, center, size, fade, flashFade, ringProgress, rotation);
                    break;
                case BoardBurstStyle.Lotus:
                    DrawLotus(vh, center, size, fade, flashFade, rotation);
                    break;
                case BoardBurstStyle.Sun:
                    DrawSun(vh, center, size, fade, flashFade, ringProgress, rotation);
                    break;
                case BoardBurstStyle.Leaf:
                    DrawLeaf(vh, center, size, fade, flashFade, rotation);
                    break;
                case BoardBurstStyle.ShieldShards:
                    DrawShieldShards(vh, center, size, fade, flashFade, rotation);
                    break;
                case BoardBurstStyle.Soul:
                    DrawSoul(vh, center, size, fade, flashFade, ringProgress, rotation);
                    break;
                case BoardBurstStyle.WhiteBlueFlash:
                    DrawWhiteBlueFlash(vh, center, size, fade, flashFade, ringProgress, rotation);
                    break;
                case BoardBurstStyle.Electric:
                    DrawElectric(vh, center, size, fade, flashFade, rotation);
                    break;
                case BoardBurstStyle.RainbowBoard:
                    DrawRainbowBoard(vh, center, size, fade, flashFade, ringProgress, rotation);
                    break;
                case BoardBurstStyle.BaguaClouds:
                    DrawBaguaClouds(vh, center, size, fade, rotation);
                    break;
                case BoardBurstStyle.RunicAura:
                    DrawRunicAura(vh, center, size, fade, ringProgress, rotation);
                    break;
            }

            for (int vertexIndex = firstVertex; vertexIndex < vh.currentVertCount; vertexIndex++)
            {
                UIVertex vertex = UIVertex.simpleVert;
                vh.PopulateUIVertex(ref vertex, vertexIndex);
                vertex.color *= burst.Tint;
                vh.SetUIVertex(vertex, vertexIndex);
            }
        }

        static void DrawFire(VertexHelper vh, Vector2 center, float size,
            float fade, float flashFade, float ringProgress, float rotation)
        {
            Color smoke = new Color(.20f, .22f, .28f, .30f * fade);
            for (int index = 0; index < 5; index++)
            {
                float angle = rotation + index * Mathf.PI * 2f / 5f;
                Vector2 puff = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))
                    * size * (.30f + ringProgress * .12f);
                AddSoftDisc(vh, puff, size * .19f, smoke);
            }

            AddSoftDisc(vh, center, size * .43f, new Color(1f, .16f, .025f, .62f * flashFade));
            AddSoftDisc(vh, center, size * .29f, new Color(1f, .48f, .045f, .78f * flashFade));
            AddSoftDisc(vh, center, size * .13f, new Color(1f, .88f, .30f, .92f * flashFade));
            AddRing(vh, center, size * Mathf.Lerp(.24f, .52f, ringProgress),
                size * .045f, new Color(1f, .62f, .08f, .82f * fade), rotation, false);

            for (int index = 0; index < 10; index++)
            {
                float angle = rotation + index * Mathf.PI * 2f / 10f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float start = size * .22f;
                float end = size * (.35f + .18f * ringProgress);
                AddRay(vh, center + direction * start, center + direction * end,
                    size * .018f, new Color(1f, .82f, .19f, .86f * fade));
            }
        }

        static void DrawWater(VertexHelper vh, Vector2 center, float size,
            float fade, float flashFade, float ringProgress, float rotation)
        {
            AddSoftDisc(vh, center, size * .33f, new Color(.88f, .98f, 1f, .65f * flashFade));
            AddRing(vh, center, size * Mathf.Lerp(.30f, .47f, ringProgress),
                size * .045f, new Color(.97f, 1f, 1f, .92f * fade), rotation, false);
            AddStar(vh, center, size * .18f, size * .075f, 6, rotation,
                new Color(.56f, .90f, 1f, .96f * fade));
            AddRay(vh, center - Vector2.right * size * .43f,
                center + Vector2.right * size * .43f, size * .022f,
                new Color(.17f, .65f, 1f, .82f * fade));
            AddRay(vh, center - Vector2.right * size * .31f,
                center + Vector2.right * size * .31f, size * .008f,
                new Color(1f, 1f, 1f, .9f * fade));
        }

        static void DrawLotus(VertexHelper vh, Vector2 center, float size,
            float fade, float flashFade, float rotation)
        {
            AddSoftDisc(vh, center, size * .32f, new Color(.47f, .12f, .76f, .62f * flashFade));
            AddRing(vh, center, size * .39f, size * .035f,
                new Color(1f, .40f, .84f, .92f * fade), rotation, false);
            for (int index = 0; index < 8; index++)
            {
                float angle = rotation + index * Mathf.PI * 2f / 8f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 petal = center + direction * size * .20f;
                AddStar(vh, petal, size * .115f, size * .048f, 4, angle,
                    new Color(1f, .47f, .88f, .90f * fade));
                Vector2 particle = center + direction * size * (.45f + (index % 2) * .06f);
                AddSoftDisc(vh, particle, size * .035f,
                    new Color(1f, .70f, .94f, .94f * fade));
            }
        }

        static void DrawSun(VertexHelper vh, Vector2 center, float size,
            float fade, float flashFade, float ringProgress, float rotation)
        {
            AddSoftDisc(vh, center, size * .33f, new Color(1f, .66f, .06f, .64f * flashFade));
            AddStar(vh, center, size * .38f, size * .19f, 8, rotation,
                new Color(1f, .78f, .10f, .94f * fade));
            AddRing(vh, center, size * Mathf.Lerp(.32f, .47f, ringProgress),
                size * .035f, new Color(1f, .90f, .31f, .98f * fade), rotation, false);
            DrawRays(vh, center, size, 12, rotation,
                new Color(1f, .72f, .08f, .92f * fade), .29f, .51f, .018f);
        }

        static void DrawLeaf(VertexHelper vh, Vector2 center, float size,
            float fade, float flashFade, float rotation)
        {
            AddSoftDisc(vh, center, size * .31f, new Color(.23f, .75f, .27f, .56f * flashFade));
            AddStar(vh, center, size * .37f, size * .20f, 8, rotation,
                new Color(.68f, 1f, .42f, .92f * fade));
            for (int index = 0; index < 8; index++)
            {
                float angle = rotation + index * Mathf.PI * 2f / 8f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddRay(vh, center + direction * size * .30f,
                    center + direction * size * .49f, size * .015f,
                    new Color(.43f, .92f, .32f, .88f * fade));
                AddSoftDisc(vh, center + direction * size * .46f, size * .035f,
                    new Color(.68f, 1f, .46f, .94f * fade));
            }
        }

        static void DrawShieldShards(VertexHelper vh, Vector2 center, float size,
            float fade, float flashFade, float rotation)
        {
            AddSoftDisc(vh, center, size * .30f, new Color(1f, .12f, .20f, .45f * flashFade));
            for (int index = 0; index < 10; index++)
            {
                float angle = rotation + index * Mathf.PI * 2f / 10f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tangent = new Vector2(-direction.y, direction.x);
                float inner = size * (.12f + (index % 3) * .035f);
                float outer = size * (.36f + (index % 2) * .12f);
                Vector2 tip = center + direction * outer;
                Vector2 basePoint = center + direction * inner;
                AddTriangle(vh, tip, basePoint + tangent * size * .055f,
                    basePoint - tangent * size * .045f,
                    index % 2 == 0
                        ? new Color(1f, .14f, .20f, .96f * fade)
                        : new Color(.78f, .025f, .12f, .92f * fade));
                AddSoftDisc(vh, tip, size * .025f, new Color(1f, .42f, .45f, .94f * fade));
            }
        }

        static void DrawSoul(VertexHelper vh, Vector2 center, float size,
            float fade, float flashFade, float ringProgress, float rotation)
        {
            AddSoftDisc(vh, center, size * .25f, new Color(.035f, .045f, .075f, .82f * flashFade));
            AddRing(vh, center, size * Mathf.Lerp(.30f, .48f, ringProgress),
                size * .035f, new Color(1f, 1f, 1f, .98f * fade), rotation, false);
            DrawRays(vh, center, size, 12, rotation,
                new Color(1f, 1f, 1f, .92f * fade), .35f, .52f, .010f);
        }

        static void DrawWhiteBlueFlash(VertexHelper vh, Vector2 center, float size,
            float fade, float flashFade, float ringProgress, float rotation)
        {
            AddSoftDisc(vh, center, size * .35f, new Color(1f, 1f, 1f, .92f * flashFade));
            AddRing(vh, center, size * Mathf.Lerp(.22f, .48f, ringProgress),
                size * .025f, new Color(.35f, .82f, 1f, .95f * fade), rotation, false);
            DrawRays(vh, center, size, 8, rotation,
                new Color(.62f, .91f, 1f, .86f * fade), .32f, .55f, .009f);
        }

        static void DrawElectric(VertexHelper vh, Vector2 center, float size,
            float fade, float flashFade, float rotation)
        {
            AddSoftDisc(vh, center, size * .30f, new Color(.17f, .66f, 1f, .58f * flashFade));
            AddStar(vh, center, size * .20f, size * .08f, 6, rotation,
                new Color(1f, 1f, 1f, .98f * fade));
            DrawRays(vh, center, size, 10, rotation,
                new Color(.48f, .88f, 1f, .92f * fade), .20f, .48f, .018f);
        }

        static void DrawRainbowBoard(VertexHelper vh, Vector2 center, float size,
            float fade, float flashFade, float ringProgress, float rotation)
        {
            float radius = size * .64f;
            AddSoftDisc(vh, center, radius * .32f, new Color(.34f, .75f, 1f, .58f * flashFade));
            AddSoftDisc(vh, center, radius * .18f, new Color(1f, 1f, 1f, .98f * flashFade));
            AddRing(vh, center, radius * Mathf.Lerp(.36f, .90f, ringProgress),
                size * .018f, new Color(.16f, .63f, 1f, .92f * fade), rotation, true);
            AddRing(vh, center, radius * Mathf.Lerp(.29f, .75f, ringProgress),
                size * .008f, new Color(.73f, .95f, 1f, .92f * fade), rotation + .04f, false);
            DrawRays(vh, center, size, 40, rotation,
                new Color(.55f, .87f, 1f, .78f * fade), .29f, .98f, .0045f);
            DrawRays(vh, center, size, 16, rotation + .12f,
                new Color(1f, 1f, 1f, .88f * fade), .20f, .68f, .006f);
        }

        static void DrawBaguaClouds(VertexHelper vh, Vector2 center, float size,
            float fade, float rotation)
        {
            Color cloud = new Color(.93f, .98f, 1f, .34f * fade);
            for (int index = 0; index < 10; index++)
            {
                float angle = rotation + index * Mathf.PI * 2f / 10f;
                float radius = size * (.22f + (index % 3) * .035f);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddSoftDisc(vh, center + direction * radius, size * .15f, cloud);
            }

            Color[] stars =
            {
                new Color(.28f, .76f, 1f, .94f * fade),
                new Color(.72f, .42f, 1f, .94f * fade),
                new Color(1f, .82f, .22f, .94f * fade),
                new Color(1f, .43f, .72f, .94f * fade),
                new Color(.38f, .92f, .46f, .94f * fade)
            };
            for (int index = 0; index < 20; index++)
            {
                float angle = rotation + index * 2.39996323f;
                float radius = size * (.08f + (index % 5) * .075f);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 star = center + direction * radius;
                AddStar(vh, star, size * .025f, size * .010f, 4, angle, stars[index % stars.Length]);
            }
        }

        static void DrawRunicAura(VertexHelper vh, Vector2 center, float size,
            float fade, float ringProgress, float rotation)
        {
            for (int ring = 0; ring < 3; ring++)
            {
                float radius = size * (.25f + ring * .10f) * Mathf.Lerp(.75f, 1f, ringProgress);
                AddRing(vh, center, radius, size * (.012f - ring * .002f),
                    new Color(.54f, .75f, 1f, (.8f - ring * .12f) * fade), rotation + ring * .12f,
                    ring == 1);
            }

            for (int index = 0; index < 8; index++)
            {
                float angle = rotation + index * Mathf.PI * 2f / 8f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddStar(vh, center + direction * size * .38f, size * .035f, size * .012f,
                    4, angle, new Color(.79f, .89f, 1f, .9f * fade));
            }
        }

        void DrawArc(VertexHelper vh, Arc arc, float fade, int index)
        {
            Vector2 axis = arc.End - arc.Start;
            Vector2 normal = axis.sqrMagnitude > .001f
                ? new Vector2(-axis.y, axis.x).normalized
                : Vector2.up;
            float wobble = Mathf.Sin(elapsed * 55f + arc.Seed * .17f + index) * arc.CellSize * .10f;
            for (int pointIndex = 0; pointIndex < PointCount; pointIndex++)
            {
                float t = pointIndex / (float)(PointCount - 1);
                float jagged = pointIndex == 0 || pointIndex == PointCount - 1
                    ? 0f
                    : Mathf.Sin(pointIndex * 1.91f + arc.Seed) * wobble * Mathf.Sin(t * Mathf.PI);
                arcPoints[pointIndex] = Vector2.Lerp(arc.Start, arc.End, t) + normal * jagged;
            }
            Color glow = new Color(.12f, .66f, 1f, .40f * fade);
            Color core = new Color(.76f, .96f, 1f, .92f * fade);
            AddPolyline(vh, arcPoints, arc.CellSize * .13f, glow);
            AddPolyline(vh, arcPoints, arc.CellSize * .065f, core);
        }

        static void DrawRays(VertexHelper vh, Vector2 center, float size, int count,
            float rotation, Color tint, float startScale, float endScale, float widthScale)
        {
            for (int index = 0; index < count; index++)
            {
                float angle = rotation + index * Mathf.PI * 2f / count;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddRay(vh, center + direction * size * startScale,
                    center + direction * size * endScale, size * widthScale, tint);
            }
        }

        static void AddSoftDisc(VertexHelper vh, Vector2 center, float radius, Color tint)
        {
            if (tint.a <= 0f || radius <= 0f)
                return;
            int first = vh.currentVertCount;
            AddVertex(vh, center, tint);
            Color edge = tint;
            edge.a = 0f;
            const int segments = 20;
            for (int index = 0; index <= segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                AddVertex(vh, point, edge);
            }
            for (int index = 0; index < segments; index++)
                vh.AddTriangle(first, first + index + 1, first + index + 2);
        }

        static void AddRing(VertexHelper vh, Vector2 center, float radius,
            float thickness, Color tint, float rotation, bool jagged)
        {
            if (tint.a <= 0f || radius <= 0f)
                return;
            const int segments = 36;
            int first = vh.currentVertCount;
            for (int index = 0; index <= segments; index++)
            {
                float angle = rotation + index * Mathf.PI * 2f / segments;
                float jaggedOffset = jagged && index % 2 == 0 ? radius * .055f : 0f;
                float innerRadius = Mathf.Max(0f, radius + jaggedOffset - thickness * .5f);
                float outerRadius = radius + jaggedOffset + thickness * .5f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddVertex(vh, center + direction * innerRadius, tint);
                AddVertex(vh, center + direction * outerRadius, tint);
            }
            for (int index = 0; index < segments; index++)
            {
                int current = first + index * 2;
                vh.AddTriangle(current, current + 1, current + 2);
                vh.AddTriangle(current + 1, current + 3, current + 2);
            }
        }

        static void AddStar(VertexHelper vh, Vector2 center, float outerRadius,
            float innerRadius, int points, float rotation, Color tint)
        {
            if (tint.a <= 0f || outerRadius <= 0f)
                return;
            int first = vh.currentVertCount;
            AddVertex(vh, center, tint);
            for (int index = 0; index <= points * 2; index++)
            {
                float angle = rotation + index * Mathf.PI / points - Mathf.PI * .5f;
                float radius = index % 2 == 0 ? outerRadius : innerRadius;
                AddVertex(vh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint);
            }
            for (int index = 0; index < points * 2; index++)
                vh.AddTriangle(first, first + index + 1, first + index + 2);
        }

        static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color tint)
        {
            int first = vh.currentVertCount;
            AddVertex(vh, a, tint);
            AddVertex(vh, b, tint);
            AddVertex(vh, c, tint);
            vh.AddTriangle(first, first + 1, first + 2);
        }

        static void AddRay(VertexHelper vh, Vector2 start, Vector2 end, float width, Color tint)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * (width * .5f);
            int first = vh.currentVertCount;
            AddVertex(vh, start - normal, tint);
            AddVertex(vh, start + normal, tint);
            AddVertex(vh, end - normal, tint);
            AddVertex(vh, end + normal, tint);
            vh.AddTriangle(first, first + 1, first + 2);
            vh.AddTriangle(first + 1, first + 3, first + 2);
        }

        static void AddPolyline(VertexHelper vh, Vector2[] points, float width, Color tint)
        {
            int first = vh.currentVertCount;
            for (int index = 0; index < points.Length; index++)
            {
                Vector2 previous = points[Mathf.Max(0, index - 1)];
                Vector2 next = points[Mathf.Min(points.Length - 1, index + 1)];
                Vector2 tangent = (next - previous).normalized;
                Vector2 normal = new Vector2(-tangent.y, tangent.x) * (width * .5f);
                AddVertex(vh, points[index] - normal, tint);
                AddVertex(vh, points[index] + normal, tint);
            }
            for (int index = 0; index < points.Length - 1; index++)
            {
                int current = first + index * 2;
                vh.AddTriangle(current, current + 1, current + 2);
                vh.AddTriangle(current + 1, current + 3, current + 2);
            }
        }

        static void AddVertex(VertexHelper vh, Vector2 position, Color tint)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = tint;
            vh.AddVert(vertex);
        }
    }
}
