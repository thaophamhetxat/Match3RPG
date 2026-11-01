using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Board
{
    public sealed class BoardBombRadiusGlow : MaskableGraphic
    {
        const int Segments = 24;
        int overlapCount;
        Color glowColor;
        float phase;
        float boardScale = 3f;

        public int OverlapCount => overlapCount;
        public Color GlowColor => glowColor;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            enabled = false;
        }

        public void SetGlow(Color color, int count, float scale = 3f)
        {
            overlapCount = Mathf.Max(0, count);
            glowColor = color;
            boardScale = Mathf.Max(1f, scale);
            bool active = overlapCount > 0;
            if (gameObject.activeSelf != active)
                gameObject.SetActive(active);
            enabled = active;
            if (active)
                SetVerticesDirty();
        }

        void Update()
        {
            phase += Time.unscaledDeltaTime;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (overlapCount <= 0)
                return;

            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * .68f * (boardScale / 3f);
            float pulse = .5f + .5f * Mathf.Sin(phase * 1.35f);
            float overlapBoost = Mathf.Min(.035f, (overlapCount - 1) * .018f);
            Color inner = glowColor;
            inner.a = .075f + pulse * .045f + overlapBoost;
            Color outer = inner;
            outer.a = 0f;

            int centerIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, center, inner);
            for (int index = 0; index <= Segments; index++)
            {
                float angle = index * Mathf.PI * 2f / Segments;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                AddVertex(vertexHelper, point, outer);
            }
            for (int index = 0; index < Segments; index++)
                vertexHelper.AddTriangle(centerIndex, centerIndex + index + 1,
                    centerIndex + index + 2);
        }

        static void AddVertex(VertexHelper vertexHelper, Vector2 position, Color color)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertexHelper.AddVert(vertex);
        }
    }
}
