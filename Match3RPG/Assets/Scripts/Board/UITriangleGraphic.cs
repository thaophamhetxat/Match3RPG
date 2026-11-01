using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Board
{
    public sealed class UITriangleGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            vertexHelper.AddVert(
                new Vector3(rect.center.x, rect.yMax), color, Vector2.up);
            vertexHelper.AddVert(
                new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            vertexHelper.AddVert(
                new Vector3(rect.xMax, rect.yMin), color, Vector2.right);
            vertexHelper.AddTriangle(0, 1, 2);
        }
    }
}
