using UnityEngine;
using UnityEngine.EventSystems;

namespace Match3RPG.Board
{
    public sealed class BoardOutsideClickCatcher : MonoBehaviour, IPointerClickHandler
    {
        BoardManager board;

        public void Configure(BoardManager boardManager) => board = boardManager;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (board != null)
                board.ClearSelection();
        }
    }
}
