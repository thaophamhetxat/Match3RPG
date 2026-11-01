using System;

namespace Match3RPG.Core
{
    // Grid rows run from top to bottom: row 0 is the top row.
    [Serializable]
    public readonly struct BoardPos
    {
        public readonly int Row;
        public readonly int Col;
        public BoardPos(int row, int col) { Row = row; Col = col; }
        public bool IsAdjacent(BoardPos other) => Math.Abs(Row - other.Row) + Math.Abs(Col - other.Col) == 1;
        public override bool Equals(object obj) => obj is BoardPos p && p.Row == Row && p.Col == Col;
        public override int GetHashCode() => (Row * 397) ^ Col;
    }
}
