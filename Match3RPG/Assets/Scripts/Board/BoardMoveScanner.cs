using System.Collections.Generic;
using Match3RPG.Core;

namespace Match3RPG.Board
{
    public sealed class BoardMoveScanner
    {
        readonly SpecialGemResolver resolver;

        public BoardMoveScanner(SpecialGemResolver specialGemResolver)
        {
            resolver = specialGemResolver;
        }

        public List<MoveResult> GetLegalMoves(BoardSnapshot snapshot)
        {
            List<MoveResult> moves = resolver.FindLegalMoves(snapshot);
            for (int row = 0; row < snapshot.Rows; row++)
            for (int column = 0; column < snapshot.Columns; column++)
            {
                BoardPos position = new BoardPos(row, column);
                GemSpecialType special = snapshot.SpecialAt(position);
                if (special == GemSpecialType.None) continue;
                MoveResult activation = resolver.EvaluateDirectActivation(snapshot, position);
                if (activation.IsValid) moves.Add(activation);
            }
            return moves;
        }

        public bool HasAnyLegalMove(BoardSnapshot snapshot)
        {
            return GetLegalMoves(snapshot).Count > 0;
        }
    }
}