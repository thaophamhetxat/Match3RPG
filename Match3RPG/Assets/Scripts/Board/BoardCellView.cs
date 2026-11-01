using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Board
{
    public sealed class BoardCellView : MonoBehaviour
    {
        BoardBombRadiusGlow bombRadiusGlow;
        public BoardPos Position { get; private set; }
        public RectTransform RectTransform => (RectTransform)transform;

        public void Initialize(BoardPos position)
        {
            Position = position;
        }

        public BoardBombRadiusGlow GetOrCreateBombRadiusGlow()
        {
            if (bombRadiusGlow != null)
                return bombRadiusGlow;

            GameObject glowObject = new GameObject("BombRadiusGlow",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(BoardBombRadiusGlow));
            glowObject.transform.SetParent(transform, false);
            RectTransform glowRect = (RectTransform)glowObject.transform;
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;
            glowRect.SetAsLastSibling();
            bombRadiusGlow = glowObject.GetComponent<BoardBombRadiusGlow>();
            bombRadiusGlow.SetGlow(Color.clear, 0);
            return bombRadiusGlow;
        }
    }
}
