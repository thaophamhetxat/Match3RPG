using System.Collections.Generic;
using System.Linq;
using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Board
{
    public sealed class BoardSnapshot
    {
        readonly GemType[,] types;
        readonly GemSpecialType[,] specials;

        public int Rows => types.GetLength(0);
        public int Columns => types.GetLength(1);

        public BoardSnapshot(GemType[,] gemTypes, GemSpecialType[,] gemSpecials)
        {
            types = (GemType[,])gemTypes.Clone();
            specials = (GemSpecialType[,])gemSpecials.Clone();
        }

        public GemType TypeAt(BoardPos position) => types[position.Row, position.Col];
        public GemSpecialType SpecialAt(BoardPos position) => specials[position.Row, position.Col];

        public BoardSnapshot Clone() => new BoardSnapshot(types, specials);

        public void Swap(BoardPos first, BoardPos second)
        {
            (types[first.Row, first.Col], types[second.Row, second.Col]) =
                (types[second.Row, second.Col], types[first.Row, first.Col]);
            (specials[first.Row, first.Col], specials[second.Row, second.Col]) =
                (specials[second.Row, second.Col], specials[first.Row, first.Col]);
        }
    }

    public sealed class SpecialGemResolver
    {
        public List<MoveResult> FindLegalMoves(BoardSnapshot snapshot)
        {
            List<MoveResult> moves = new List<MoveResult>();
            for (int row = 0; row < snapshot.Rows; row++)
            for (int column = 0; column < snapshot.Columns; column++)
            {
                BoardPos from = new BoardPos(row, column);
                if (column + 1 < snapshot.Columns)
                    AddLegalMove(snapshot, moves, from, new BoardPos(row, column + 1));
                if (row + 1 < snapshot.Rows)
                    AddLegalMove(snapshot, moves, from, new BoardPos(row + 1, column));
            }
            return moves;
        }

        public MoveResult EvaluateSwap(BoardSnapshot source, BoardPos from, BoardPos to)
        {
            MoveResult move = new MoveResult(from, to);
            if (!from.IsAdjacent(to))
                return move;

            BoardSnapshot swapped = source.Clone();
            swapped.Swap(from, to);
            move.WasSpecialActivation = source.SpecialAt(from) != GemSpecialType.None
                || source.SpecialAt(to) != GemSpecialType.None;

            GemSpecialType firstSpecial = swapped.SpecialAt(from);
            GemSpecialType secondSpecial = swapped.SpecialAt(to);
            if (firstSpecial == GemSpecialType.AreaBomb && secondSpecial == GemSpecialType.AreaBomb)
                return move;
            if (ContainsRainbow(firstSpecial, secondSpecial)
                && (firstSpecial != GemSpecialType.Rainbow || secondSpecial != GemSpecialType.Rainbow)
                && (firstSpecial != GemSpecialType.None && secondSpecial != GemSpecialType.None))
                return move;

            MatchResult match = ResolveCurrent(swapped, move, true, 1, false);
            move.InitialMatch = match;
            move.IsValid = match.HasMatch;
            move.GrantsExtraTurn = match.GrantsExtraTurn;
            move.ExtraTurnsGranted = match.ExtraTurnsGranted;
            return move;
        }

        public MoveResult EvaluateDirectActivation(BoardSnapshot source, BoardPos position)
        {
            MoveResult move = new MoveResult(position, position) { IsDirectActivation = true };
            GemSpecialType special = source.SpecialAt(position);
            if (special == GemSpecialType.Bagua)
            {
                move.IsValid = true;
                move.IsBaguaShuffle = true;
                move.WasSpecialActivation = true;
                move.GrantsExtraTurn = true;
                move.ExtraTurnsGranted = 1;
                move.InitialMatch = new MatchResult
                {
                    GrantsExtraTurn = true,
                    ExtraTurnsGranted = 1
                };
                move.InitialMatch.SpecialActivations.Add(new SpecialActivation(
                    SpecialActivationKind.BaguaShuffle, position));
                return move;
            }
            if (special == GemSpecialType.Lightning)
            {
                move.IsValid = CountOrdinaryGems(source) >= 5;
                move.WasSpecialActivation = true;
                move.InitialMatch = new MatchResult();
                move.InitialMatch.SpecialActivations.Add(new SpecialActivation(
                    SpecialActivationKind.Lightning, position));
                return move;
            }
            if (special != GemSpecialType.AreaBomb)
                return move;

            move.WasSpecialActivation = true;
            MatchResult match = ResolveCurrent(source, move, false, 1, true);
            move.InitialMatch = match;
            move.IsValid = match.HasMatch;
            return move;
        }

        public MatchResult ResolveCurrent(BoardSnapshot board, MoveResult move,
            bool initialSwap, int comboIndex, bool directActivation, bool rollBonusSpecials = false)
        {
            MatchResult result = new MatchResult { ComboIndex = comboIndex };
            HashSet<BoardPos> clear = new HashSet<BoardPos>();
            HashSet<BoardPos> reserved = new HashSet<BoardPos>();
            HashSet<BoardPos> handledSpecials = new HashSet<BoardPos>();

            if (!directActivation)
            {
                FindLines(board, result);
                foreach (MatchSegment segment in result.Segments)
                foreach (BoardPos position in segment.Positions)
                    clear.Add(position);
            }

            List<SpecialGemCreation> createdSpecials = SelectSpecials(result,
                initialSwap && !directActivation ? move.To : (BoardPos?)null,
                board, rollBonusSpecials);
            foreach (SpecialGemCreation creation in createdSpecials)
            {
                result.CreatedSpecials.Add(creation);
                reserved.Add(creation.Position);
                clear.Remove(creation.Position);
            }
            if (result.CreatedSpecials.Count > 0)
            {
                SpecialGemCreation primary = result.CreatedSpecials[0];
                result.CreatedSpecial = primary.SpecialType;
                result.CreatedGemType = primary.GemType;
                result.SpecialPosition = primary.Position;
            }
            foreach (SpecialGemCreation creation in result.CreatedSpecials)
            {
                switch (creation.SpecialType)
                {
                    case GemSpecialType.Line:
                        result.CreatedLineCount++;
                        break;
                    case GemSpecialType.AreaBomb:
                        result.CreatedAreaBombCount++;
                        if (creation.IsTLBomb) result.CreatedTLBombCount++;
                        break;
                    case GemSpecialType.Rainbow:
                        result.CreatedRainbowCount++;
                        break;
                }
            }
            result.ExtraTurnsGranted = result.CreatedAreaBombCount + result.CreatedRainbowCount;
            result.GrantsExtraTurn = result.ExtraTurnsGranted > 0;

            if (directActivation)
            {
                if (board.SpecialAt(move.From) == GemSpecialType.Lightning)
                {
                    AddLightningTargets(board, move.From, clear);
                    result.SpecialActivations.Add(new SpecialActivation(
                        SpecialActivationKind.Lightning, move.From));
                }
                else
                {
                    AddAreaBomb(board, move.From, clear, reserved);
                    result.SpecialActivations.Add(new SpecialActivation(
                        SpecialActivationKind.AreaBomb, move.From));
                }
                handledSpecials.Add(move.From);
            }
            else if (initialSwap && TryResolveSwapCombination(board, move, clear, reserved,
                handledSpecials, result))
            {
                // The supported combination has already added its unique affected cells.
            }

            ExpandSpecials(board, clear, reserved, handledSpecials, result);

            result.LongestMatch = result.Segments.Count == 0
                ? 0
                : result.Segments.Max(segment => segment.Length);
            result.ClearPositions.AddRange(clear.OrderBy(position => position.Row)
                .ThenBy(position => position.Col));
            foreach (BoardPos position in result.ClearPositions)
            {
                if (directActivation && board.SpecialAt(move.From) == GemSpecialType.Lightning
                    && (position.Equals(move.From) || board.SpecialAt(position) != GemSpecialType.None))
                    continue;
                GemType type = board.TypeAt(position);
                result.GemCounts.TryGetValue(type, out int count);
                result.GemCounts[type] = count + 1;
            }
            return result;
        }

        bool TryResolveSwapCombination(BoardSnapshot board, MoveResult move,
            HashSet<BoardPos> clear, HashSet<BoardPos> reserved,
            HashSet<BoardPos> handledSpecials, MatchResult result)
        {
            GemSpecialType first = board.SpecialAt(move.From);
            GemSpecialType second = board.SpecialAt(move.To);
            if (first == GemSpecialType.Rainbow && second == GemSpecialType.Rainbow)
            {
                AddAll(board, clear, reserved);
                result.SpecialActivations.Add(new SpecialActivation(
                    SpecialActivationKind.RainbowFullBoard, move.From));
                handledSpecials.Add(move.From);
                handledSpecials.Add(move.To);
                return true;
            }

            if (first == GemSpecialType.Rainbow && second == GemSpecialType.None)
                return AddRainbowColor(board, move.From, board.TypeAt(move.To), clear, reserved, result);
            if (second == GemSpecialType.Rainbow && first == GemSpecialType.None)
                return AddRainbowColor(board, move.To, board.TypeAt(move.From), clear, reserved, result);

            BoardPos bombPosition;
            if (first == GemSpecialType.AreaBomb && IsLineSpecial(second))
            {
                bombPosition = move.From;
            }
            else if (second == GemSpecialType.AreaBomb && IsLineSpecial(first))
            {
                bombPosition = move.To;
            }
            else
            {
                return false;
            }

            BoardPos linePosition = bombPosition.Equals(move.From) ? move.To : move.From;
            handledSpecials.Add(bombPosition);
            handledSpecials.Add(linePosition);
            clear.Add(bombPosition);
            clear.Add(linePosition);
            result.SpecialActivations.Add(new SpecialActivation(
                SpecialActivationKind.BombLineCombo, linePosition));
            for (int offset = -1; offset <= 1; offset++)
            {
                int row = linePosition.Row + offset;
                int column = linePosition.Col + offset;
                if (row >= 0 && row < board.Rows)
                    for (int clearColumn = 0; clearColumn < board.Columns; clearColumn++)
                        AddPosition(clear, reserved, new BoardPos(row, clearColumn));
                if (column >= 0 && column < board.Columns)
                    for (int clearRow = 0; clearRow < board.Rows; clearRow++)
                        AddPosition(clear, reserved, new BoardPos(clearRow, column));
            }
            return true;
        }

        static bool AddRainbowColor(BoardSnapshot board, BoardPos rainbowPosition, GemType targetType,
            HashSet<BoardPos> clear, HashSet<BoardPos> reserved, MatchResult result)
        {
            if (targetType == GemType.Rainbow)
                return false;
            result.SpecialActivations.Add(new SpecialActivation(
                SpecialActivationKind.RainbowColor, rainbowPosition, targetType));
            AddPosition(clear, reserved, rainbowPosition);
            for (int row = 0; row < board.Rows; row++)
            for (int column = 0; column < board.Columns; column++)
            {
                BoardPos position = new BoardPos(row, column);
                if (board.TypeAt(position) == targetType)
                    AddPosition(clear, reserved, position);
            }
            return true;
        }

        void AddLegalMove(BoardSnapshot snapshot, List<MoveResult> moves, BoardPos from, BoardPos to)
        {
            MoveResult move = EvaluateSwap(snapshot, from, to);
            if (move.IsValid) moves.Add(move);
        }

        static bool ContainsRainbow(GemSpecialType first, GemSpecialType second) =>
            first == GemSpecialType.Rainbow || second == GemSpecialType.Rainbow;

        static bool IsLineSpecial(GemSpecialType special) => special == GemSpecialType.Line;

        static void FindLines(BoardSnapshot board, MatchResult result)
        {
            for (int row = 0; row < board.Rows; row++)
            {
                int start = 0;
                while (start < board.Columns)
                {
                    BoardPos position = new BoardPos(row, start);
                    GemType type = board.TypeAt(position);
                    if (board.SpecialAt(position) == GemSpecialType.Rainbow || type == GemType.Rainbow)
                    {
                        start++;
                        continue;
                    }

                    int end = start + 1;
                    while (end < board.Columns)
                    {
                        BoardPos next = new BoardPos(row, end);
                        if (board.TypeAt(next) != type || board.SpecialAt(next) == GemSpecialType.Rainbow)
                            break;
                        end++;
                    }
                    AddSegment(result, type, true, row, start, end);
                    start = end;
                }
            }

            for (int column = 0; column < board.Columns; column++)
            {
                int start = 0;
                while (start < board.Rows)
                {
                    BoardPos position = new BoardPos(start, column);
                    GemType type = board.TypeAt(position);
                    if (board.SpecialAt(position) == GemSpecialType.Rainbow || type == GemType.Rainbow)
                    {
                        start++;
                        continue;
                    }

                    int end = start + 1;
                    while (end < board.Rows)
                    {
                        BoardPos next = new BoardPos(end, column);
                        if (board.TypeAt(next) != type || board.SpecialAt(next) == GemSpecialType.Rainbow)
                            break;
                        end++;
                    }
                    AddSegment(result, type, false, column, start, end);
                    start = end;
                }
            }
        }

        static void AddSegment(MatchResult result, GemType type, bool horizontal, int fixedPosition,
            int start, int end)
        {
            if (end - start < 3) return;
            List<BoardPos> positions = new List<BoardPos>(end - start);
            for (int index = start; index < end; index++)
                positions.Add(horizontal
                    ? new BoardPos(fixedPosition, index)
                    : new BoardPos(index, fixedPosition));
            result.Segments.Add(new MatchSegment(type, horizontal, positions));
        }

        static List<SpecialGemCreation> SelectSpecials(MatchResult result,
            BoardPos? destination, BoardSnapshot board, bool rollBonusSpecials)
        {
            List<SpecialGemCreation> creations = new List<SpecialGemCreation>();
            HashSet<BoardPos> claimedPositions = new HashSet<BoardPos>();
            IOrderedEnumerable<MatchSegment> candidates = result.Segments
                .Where(segment => segment.Length >= 6)
                .OrderByDescending(segment => segment.Length)
                .ThenByDescending(segment => destination.HasValue
                    && segment.Positions.Contains(destination.Value))
                .ThenByDescending(segment => segment.IsHorizontal)
                .ThenBy(segment => segment.Positions.Min(position => position.Row))
                .ThenBy(segment => segment.Positions.Min(position => position.Col));

            foreach (MatchSegment segment in candidates)
            {
                AddStraightCreation(result, creations, claimedPositions, segment, destination,
                    board, rollBonusSpecials);
            }

            foreach (IntersectionCandidate candidate in FindIntersectionCandidates(result.Segments))
            {
                if (claimedPositions.Contains(candidate.Intersection))
                    continue;
                List<BoardPos> availablePositions = candidate.Positions
                    .OrderBy(position => position.Row)
                    .ThenBy(position => position.Col)
                    .Where(position => !claimedPositions.Contains(position)).ToList();
                if (availablePositions.Count < 5)
                    continue;

                MatchSegment shape = new MatchSegment(candidate.Type, false, availablePositions);
                BoardPos creationPosition = availablePositions.Contains(candidate.Intersection)
                    && board.SpecialAt(candidate.Intersection) == GemSpecialType.None
                    ? candidate.Intersection
                    : SelectCreationPosition(shape, destination, board);
                if (board.SpecialAt(creationPosition) != GemSpecialType.None)
                    continue;

                creations.Add(new SpecialGemCreation(GemSpecialType.AreaBomb, candidate.Type,
                    creationPosition, shape, true));
                if (rollBonusSpecials)
                    AddBonusSpecial(result, isTLBomb: true);
                foreach (BoardPos position in availablePositions)
                    claimedPositions.Add(position);
            }

            foreach (MatchSegment segment in result.Segments
                .Where(segment => segment.Length == 5)
                .OrderByDescending(segment => destination.HasValue
                    && segment.Positions.Contains(destination.Value))
                .ThenByDescending(segment => segment.IsHorizontal)
                .ThenBy(segment => segment.Positions.Min(position => position.Row))
                .ThenBy(segment => segment.Positions.Min(position => position.Col)))
            {
                AddStraightCreation(result, creations, claimedPositions, segment, destination,
                    board, rollBonusSpecials);
            }

            foreach (MatchSegment segment in result.Segments
                .Where(segment => segment.Length == 4)
                .OrderByDescending(segment => destination.HasValue
                    && segment.Positions.Contains(destination.Value))
                .ThenByDescending(segment => segment.IsHorizontal)
                .ThenBy(segment => segment.Positions.Min(position => position.Row))
                .ThenBy(segment => segment.Positions.Min(position => position.Col)))
            {
                AddStraightCreation(result, creations, claimedPositions, segment, destination,
                    board, rollBonusSpecials);
            }
            return creations;
        }

        static void AddStraightCreation(MatchResult result, List<SpecialGemCreation> creations,
            HashSet<BoardPos> claimedPositions, MatchSegment segment, BoardPos? destination,
            BoardSnapshot board, bool rollBonusSpecials)
        {
            List<BoardPos> availablePositions = segment.Positions
                .Where(position => !claimedPositions.Contains(position)).ToList();
            if (availablePositions.Count < (segment.Length >= 5 ? 5 : 4))
                return;

            MatchSegment availableSegment = availablePositions.Count == segment.Length
                ? segment
                : new MatchSegment(segment.Type, segment.IsHorizontal, availablePositions);
            if (availableSegment.Length < 4)
                return;

            GemSpecialType special = availableSegment.Length >= 5
                ? GemSpecialType.Rainbow
                : GemSpecialType.Line;
            BoardPos position = SelectCreationPosition(availableSegment, destination, board);
            if (board.SpecialAt(position) != GemSpecialType.None)
                return;

            GemType type = special == GemSpecialType.Rainbow ? GemType.Rainbow : availableSegment.Type;
            creations.Add(new SpecialGemCreation(special, type, position, availableSegment));
            if (rollBonusSpecials && special == GemSpecialType.Rainbow)
                AddBonusSpecial(result, isTLBomb: false);
            foreach (BoardPos matchedPosition in availablePositions)
                claimedPositions.Add(matchedPosition);
        }

        static void AddBonusSpecial(MatchResult result, bool isTLBomb)
        {
            GemSpecialType bonus = RollBonusSpecial(isTLBomb);
            if (bonus != GemSpecialType.None)
                result.RefillSpecials.Add(bonus);
        }

        static GemSpecialType RollBonusSpecial(bool isTLBomb)
        {
            return ResolveBonusRoll(Random.Range(0, 100), isTLBomb);
        }

        internal static GemSpecialType ResolveBonusRoll(int roll, bool isTLBomb)
        {
            roll = Mathf.Clamp(roll, 0, 99);
            if (isTLBomb)
            {
                if (roll < 50) return GemSpecialType.None;
                return roll < 75 ? GemSpecialType.Lightning : GemSpecialType.Bagua;
            }

            if (roll < 70) return GemSpecialType.None;
            return roll < 85 ? GemSpecialType.Lightning : GemSpecialType.Bagua;
        }

        static int CountOrdinaryGems(BoardSnapshot board)
        {
            int count = 0;
            for (int row = 0; row < board.Rows; row++)
            for (int column = 0; column < board.Columns; column++)
            {
                BoardPos position = new BoardPos(row, column);
                if (IsOrdinaryGem(board, position)) count++;
            }
            return count;
        }

        static void AddLightningTargets(BoardSnapshot board, BoardPos source,
            HashSet<BoardPos> clear)
        {
            List<BoardPos> candidates = new List<BoardPos>();
            for (int row = 0; row < board.Rows; row++)
            for (int column = 0; column < board.Columns; column++)
            {
                BoardPos position = new BoardPos(row, column);
                if (!position.Equals(source) && IsOrdinaryGem(board, position))
                    candidates.Add(position);
            }

            for (int index = candidates.Count - 1; index > 0; index--)
            {
                int other = Random.Range(0, index + 1);
                (candidates[index], candidates[other]) = (candidates[other], candidates[index]);
            }
            int targetCount = Mathf.Min(Random.Range(5, 11), candidates.Count);
            clear.Add(source);
            for (int index = 0; index < targetCount; index++)
                clear.Add(candidates[index]);
        }

        static bool IsOrdinaryGem(BoardSnapshot board, BoardPos position)
        {
            GemType type = board.TypeAt(position);
            return board.SpecialAt(position) == GemSpecialType.None
                && type != GemType.Rainbow
                && type != GemType.Lightning
                && type != GemType.Bagua;
        }

        static List<IntersectionCandidate> FindIntersectionCandidates(
            IReadOnlyList<MatchSegment> segments)
        {
            List<MatchSegment> horizontal = segments
                .Where(segment => segment.IsHorizontal)
                .OrderBy(segment => segment.Positions[0].Row)
                .ThenBy(segment => segment.Positions[0].Col)
                .ToList();
            List<MatchSegment> vertical = segments
                .Where(segment => !segment.IsHorizontal)
                .OrderBy(segment => segment.Positions[0].Col)
                .ThenBy(segment => segment.Positions[0].Row)
                .ToList();
            List<IntersectionCandidate> candidates = new List<IntersectionCandidate>();
            foreach (MatchSegment rowRun in horizontal)
            foreach (MatchSegment columnRun in vertical)
            {
                if (rowRun.Length >= 5 || columnRun.Length >= 5)
                    continue;
                if (rowRun.Type != columnRun.Type) continue;
                bool intersects = false;
                BoardPos intersection = default;
                foreach (BoardPos position in rowRun.Positions)
                {
                    if (!columnRun.Positions.Contains(position)) continue;
                    intersection = position;
                    intersects = true;
                    break;
                }
                if (!intersects) continue;

                HashSet<BoardPos> positions = new HashSet<BoardPos>(rowRun.Positions);
                positions.UnionWith(columnRun.Positions);
                candidates.Add(new IntersectionCandidate(rowRun.Type, intersection, positions));
            }

            return candidates
                .OrderByDescending(candidate => candidate.Positions.Count)
                .ThenBy(candidate => candidate.Intersection.Row)
                .ThenBy(candidate => candidate.Intersection.Col)
                .ToList();
        }

        sealed class IntersectionCandidate
        {
            public readonly GemType Type;
            public readonly BoardPos Intersection;
            public readonly HashSet<BoardPos> Positions;

            public IntersectionCandidate(GemType type, BoardPos intersection,
                HashSet<BoardPos> positions)
            {
                Type = type;
                Intersection = intersection;
                Positions = positions;
            }
        }

        static BoardPos SelectCreationPosition(MatchSegment segment, BoardPos? destination,
            BoardSnapshot board)
        {
            if (destination.HasValue && segment.Positions.Contains(destination.Value)
                && board.SpecialAt(destination.Value) == GemSpecialType.None)
                return destination.Value;

            BoardPos center = segment.Positions[segment.Length / 2];
            if (board.SpecialAt(center) == GemSpecialType.None)
                return center;

            return segment.Positions
                .Where(position => board.SpecialAt(position) == GemSpecialType.None)
                .DefaultIfEmpty(segment.Positions[0])
                .First();
        }

        static void ExpandSpecials(BoardSnapshot board, HashSet<BoardPos> clear,
            HashSet<BoardPos> reserved, HashSet<BoardPos> handledSpecials, MatchResult result)
        {
            Queue<BoardPos> pending = new Queue<BoardPos>(clear);
            while (pending.Count > 0)
            {
                BoardPos position = pending.Dequeue();
                GemSpecialType special = board.SpecialAt(position);
                if (special == GemSpecialType.None || special == GemSpecialType.Rainbow
                    || !handledSpecials.Add(position))
                    continue;

                if (special == GemSpecialType.Line)
                {
                    result.SpecialActivations.Add(new SpecialActivation(
                        SpecialActivationKind.Line, position));
                    for (int offset = -1; offset <= 1; offset++)
                    {
                        int row = position.Row + offset;
                        int column = position.Col + offset;
                        if (row >= 0 && row < board.Rows)
                            AddAndQueue(clear, reserved, pending,
                                new BoardPos(row, position.Col));
                        if (column >= 0 && column < board.Columns)
                            AddAndQueue(clear, reserved, pending,
                                new BoardPos(position.Row, column));
                    }
                }
                else if (special == GemSpecialType.AreaBomb)
                {
                    result.SpecialActivations.Add(new SpecialActivation(
                        SpecialActivationKind.AreaBomb, position));
                    for (int row = position.Row - 1; row <= position.Row + 1; row++)
                    for (int column = position.Col - 1; column <= position.Col + 1; column++)
                    {
                        if (row < 0 || row >= board.Rows || column < 0 || column >= board.Columns)
                            continue;
                        AddAndQueue(clear, reserved, pending, new BoardPos(row, column));
                    }
                }
            }
        }

        static void AddAreaBomb(BoardSnapshot board, BoardPos center, HashSet<BoardPos> clear,
            HashSet<BoardPos> reserved)
        {
            for (int row = center.Row - 1; row <= center.Row + 1; row++)
            for (int column = center.Col - 1; column <= center.Col + 1; column++)
            {
                if (row < 0 || row >= board.Rows || column < 0 || column >= board.Columns)
                    continue;
                AddPosition(clear, reserved, new BoardPos(row, column));
            }
        }

        static void AddAndQueue(HashSet<BoardPos> clear, HashSet<BoardPos> reserved,
            Queue<BoardPos> pending, BoardPos position)
        {
            if (reserved.Contains(position) || !clear.Add(position)) return;
            pending.Enqueue(position);
        }

        static void AddPosition(HashSet<BoardPos> clear, HashSet<BoardPos> reserved, BoardPos position)
        {
            if (!reserved.Contains(position))
                clear.Add(position);
        }

        static void AddAll(BoardSnapshot board, HashSet<BoardPos> clear, HashSet<BoardPos> reserved)
        {
            for (int row = 0; row < board.Rows; row++)
            for (int column = 0; column < board.Columns; column++)
                AddPosition(clear, reserved, new BoardPos(row, column));
        }
    }
}
