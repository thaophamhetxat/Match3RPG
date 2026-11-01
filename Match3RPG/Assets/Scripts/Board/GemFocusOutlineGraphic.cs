using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Board
{
    public sealed class GemFocusOutlineGraphic : MaskableGraphic
    {
        const int SegmentsPerCorner = 8;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            float thickness = Mathf.Min(2.5f, Mathf.Min(rect.width, rect.height) * .1f);
            float outerRadius = Mathf.Min(12f, Mathf.Min(rect.width, rect.height) * .15f);
            float innerRadius = Mathf.Max(0f, outerRadius - thickness);
            Vector2[] outer = CreateRoundedRectPoints(rect, outerRadius);
            Rect innerRect = new Rect(
                rect.xMin + thickness, rect.yMin + thickness,
                rect.width - thickness * 2f, rect.height - thickness * 2f);
            Vector2[] inner = CreateRoundedRectPoints(innerRect, innerRadius);

            for (int index = 0; index < outer.Length; index++)
            {
                vertexHelper.AddVert(outer[index], color, Vector2.zero);
                vertexHelper.AddVert(inner[index], color, Vector2.zero);
            }

            for (int index = 0; index < outer.Length; index++)
            {
                int next = (index + 1) % outer.Length;
                int outerIndex = index * 2;
                int innerIndex = outerIndex + 1;
                int nextOuterIndex = next * 2;
                int nextInnerIndex = nextOuterIndex + 1;
                vertexHelper.AddTriangle(outerIndex, nextOuterIndex, innerIndex);
                vertexHelper.AddTriangle(nextOuterIndex, nextInnerIndex, innerIndex);
            }
        }

        static Vector2[] CreateRoundedRectPoints(Rect rect, float radius)
        {
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * .5f);
            Vector2[] points = new Vector2[SegmentsPerCorner * 4];
            Vector2[] centers =
            {
                new Vector2(rect.xMax - radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMin + radius),
                new Vector2(rect.xMax - radius, rect.yMin + radius)
            };

            for (int corner = 0; corner < 4; corner++)
            for (int segment = 0; segment < SegmentsPerCorner; segment++)
            {
                float angle = (corner * 90f + segment * (90f / SegmentsPerCorner))
                    * Mathf.Deg2Rad;
                points[corner * SegmentsPerCorner + segment] = centers[corner]
                    + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return points;
        }
    }
}
