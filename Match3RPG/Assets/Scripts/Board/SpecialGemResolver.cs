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
            if (ContainsRainbow(firstSpecial, secondSpecial)
                && (firstSpecial != GemSpecialType.Rainbow || secondSpecial != GemSpecialType.Rainbow)
                && firstSpecial != GemSpecialType.Bagua && secondSpecial != GemSpecialType.Bagua
                && firstSpecial != GemSpecialType.AreaBomb && secondSpecial != GemSpecialType.AreaBomb
                && firstSpecial != GemSpecialType.Line && secondSpecial != GemSpecialType.Line
                && firstSpecial != GemSpecialType.Lightning && secondSpecial != GemSpecialType.Lightning
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
            bool fullLineSwapCombination = !directActivation && initialSwap
                && IsFullLineSwapCombination(board, move);
            bool rainbowBaguaSwap = !directActivation && initialSwap
                && TryGetRainbowSpecialPair(board, move, GemSpecialType.Bagua,
                    out _, out _);
            bool rainbowBombSwap = !directActivation && initialSwap
                && TryGetRainbowSpecialPair(board, move, GemSpecialType.AreaBomb,
                    out _, out _);
            bool rainbowLineSwap = !directActivation && initialSwap
                && TryGetRainbowSpecialPair(board, move, GemSpecialType.Line,
                    out _, out _);
            bool rainbowLightningSwap = !directActivation && initialSwap
                && TryGetRainbowSpecialPair(board, move, GemSpecialType.Lightning,
                    out _, out _);
            bool bombBombSwap = !directActivation && initialSwap
                && IsBombBombSwap(board, move);
            bool fallbackSpecialSwap = !directActivation && initialSwap
                && IsFallbackSpecialSwap(board, move);
            bool specialSwapCombination = rainbowBaguaSwap || rainbowBombSwap
                || rainbowLineSwap || rainbowLightningSwap || bombBombSwap
                || fallbackSpecialSwap;

            if (!directActivation && !fullLineSwapCombination && !specialSwapCombination)
            {
                FindLines(board, result);
                foreach (MatchSegment segment in result.Segments)
                foreach (BoardPos position in segment.Positions)
                    clear.Add(position);
            }

            List<SpecialGemCreation> createdSpecials = fullLineSwapCombination || specialSwapCombination
                ? new List<SpecialGemCreation>()
                : SelectSpecials(result,
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
                handledSpecials, result, rollBonusSpecials))
            {
                // The supported combination has already added its unique affected cells.
            }

            ExpandSpecials(board, clear, reserved, handledSpecials, result);
            if (fallbackSpecialSwap)
            {
                AddFallbackBaguaActivations(board, move, result);
            }
            HashSet<BoardPos> consumedRainbows = new HashSet<BoardPos>();
            if (initialSwap && !directActivation)
            {
                if (board.SpecialAt(move.From) == GemSpecialType.Rainbow)
                    consumedRainbows.Add(move.From);
                if (board.SpecialAt(move.To) == GemSpecialType.Rainbow)
                    consumedRainbows.Add(move.To);
            }
            PreserveUnconsumedRainbows(board, clear, consumedRainbows);

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
                if (rainbowLightningSwap && !IsOrdinaryGem(board, position))
                    continue;
                GemType type = board.TypeAt(position);
                result.GemCounts.TryGetValue(type, out int count);
                result.GemCounts[type] = count + 1;
            }
            return result;
        }

        public MatchResult ResolveRainbowBaguaCombo(BoardSnapshot board,
            BoardPos rainbowPosition, BoardPos baguaPosition)
        {
            MatchResult result = new MatchResult { ComboIndex = 1 };
            int colorCount = (int)GemType.Soul - (int)GemType.Attack + 1;
            int[] counts = new int[colorCount];
            for (int row = 0; row < board.Rows; row++)
            for (int column = 0; column < board.Columns; column++)
            {
                GemType type = board.TypeAt(new BoardPos(row, column));
                if (IsColoredGemType(type))
                    counts[(int)type - (int)GemType.Attack]++;
            }

            int maximum = counts.Max();
            List<GemType> mostCommonTypes = new List<GemType>();
            for (int index = 0; index < counts.Length; index++)
                if (counts[index] == maximum && maximum > 0)
                    mostCommonTypes.Add((GemType)((int)GemType.Attack + index));

            HashSet<BoardPos> clear = new HashSet<BoardPos>
            {
                rainbowPosition,
                baguaPosition
            };
            if (mostCommonTypes.Count > 0)
            {
                GemType selectedType = mostCommonTypes[
                    UnityEngine.Random.Range(0, mostCommonTypes.Count)];
                result.SpecialActivations.Add(new SpecialActivation(
                    SpecialActivationKind.RainbowColor, rainbowPosition, selectedType));
                for (int row = 0; row < board.Rows; row++)
                for (int column = 0; column < board.Columns; column++)
                {
                    BoardPos position = new BoardPos(row, column);
                    if (board.TypeAt(position) == selectedType)
                        clear.Add(position);
                }
            }
            result.SpecialActivations.Add(new SpecialActivation(
                SpecialActivationKind.BaguaShuffle, baguaPosition));

            HashSet<BoardPos> handled = new HashSet<BoardPos>
            {
                rainbowPosition,
                baguaPosition
            };
            ExpandSpecials(board, clear, new HashSet<BoardPos>(), handled, result);
            PreserveUnconsumedRainbows(board, clear,
                new HashSet<BoardPos> { rainbowPosition });
            AddClearedPositions(board, clear, result);
            return result;
        }

        bool TryResolveSwapCombination(BoardSnapshot board, MoveResult move,
            HashSet<BoardPos> clear, HashSet<BoardPos> reserved,
            HashSet<BoardPos> handledSpecials, MatchResult result,
            bool rollBonusSpecials)
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

            BoardPos rainbowPosition;
            BoardPos partnerPosition;
            if (TryGetRainbowSpecialPair(board, move, GemSpecialType.Bagua,
                out rainbowPosition, out partnerPosition))
            {
                handledSpecials.Add(rainbowPosition);
                handledSpecials.Add(partnerPosition);
                clear.Add(rainbowPosition);
                clear.Add(partnerPosition);
                result.SpecialActivations.Add(new SpecialActivation(
                    SpecialActivationKind.RainbowBaguaCombo, rainbowPosition));
                return true;
            }

            if (TryGetRainbowSpecialPair(board, move, GemSpecialType.AreaBomb,
                out rainbowPosition, out partnerPosition))
            {
                return ResolveRainbowBombCombo(board, rainbowPosition,
                    partnerPosition, clear, reserved, handledSpecials, result,
                    rollBonusSpecials);
            }

            if (TryGetRainbowSpecialPair(board, move, GemSpecialType.Line,
                out rainbowPosition, out partnerPosition))
            {
                ResolveRainbowLineCombo(board, rainbowPosition, partnerPosition,
                    clear, handledSpecials, result);
                return true;
            }

            if (TryGetRainbowSpecialPair(board, move, GemSpecialType.Lightning,
                out rainbowPosition, out partnerPosition))
            {
                ResolveRainbowLightningCombo(board, rainbowPosition, partnerPosition,
                    clear, handledSpecials, result);
                return true;
            }

            if (IsBombBombSwap(board, move))
            {
                BoardPos center = move.To;
                clear.Add(move.From);
                clear.Add(move.To);
                result.SpecialActivations.Add(new SpecialActivation(
                    SpecialActivationKind.AreaBombFiveByFive, center,
                    board.TypeAt(center)));
                handledSpecials.Add(move.From);
                handledSpecials.Add(move.To);
                for (int row = center.Row - 2; row <= center.Row + 2; row++)
                for (int column = center.Col - 2; column <= center.Col + 2; column++)
                {
                    if (row >= 0 && row < board.Rows
                        && column >= 0 && column < board.Columns)
                    {
                        BoardPos position = new BoardPos(row, column);
                        clear.Add(position);
                    }
                }
                return true;
            }

            if (first == GemSpecialType.Line && second == GemSpecialType.Line)
            {
                handledSpecials.Add(move.From);
                handledSpecials.Add(move.To);
                result.SpecialActivations.Add(new SpecialActivation(
                    SpecialActivationKind.LineLineCombo, move.To));
                for (int column = 0; column < board.Columns; column++)
                    AddPosition(clear, reserved, new BoardPos(move.To.Row, column));
                for (int row = 0; row < board.Rows; row++)
                    AddPosition(clear, reserved, new BoardPos(row, move.To.Col));
                return true;
            }

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
                return ResolveFallbackSpecialCombo(board, move, clear,
                    handledSpecials, result);
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

        static bool ResolveFallbackSpecialCombo(BoardSnapshot board, MoveResult move,
            HashSet<BoardPos> clear, HashSet<BoardPos> handledSpecials,
            MatchResult result)
        {
            GemSpecialType first = board.SpecialAt(move.From);
            GemSpecialType second = board.SpecialAt(move.To);
            if (first == GemSpecialType.None || second == GemSpecialType.None
                || first == GemSpecialType.Rainbow || second == GemSpecialType.Rainbow
                || IsBombBombSwap(board, move)
                || (first == GemSpecialType.Line && second == GemSpecialType.Line)
                || (first == GemSpecialType.AreaBomb && second == GemSpecialType.Line)
                || (second == GemSpecialType.AreaBomb && first == GemSpecialType.Line))
                return false;

            clear.Add(move.From);
            clear.Add(move.To);
            handledSpecials.Add(move.From);
            handledSpecials.Add(move.To);
            if (first != GemSpecialType.Lightning)
                AddFallbackSpecialEffect(board, move.From, first, clear, result);
            if (second != GemSpecialType.Lightning)
                AddFallbackSpecialEffect(board, move.To, second, clear, result);
            if (first == GemSpecialType.Lightning)
                AddFallbackSpecialEffect(board, move.From, first, clear, result);
            if (second == GemSpecialType.Lightning)
                AddFallbackSpecialEffect(board, move.To, second, clear, result);
            return true;
        }

        static void AddFallbackSpecialEffect(BoardSnapshot board, BoardPos source,
            GemSpecialType special, HashSet<BoardPos> clear, MatchResult result)
        {
            switch (special)
            {
                case GemSpecialType.AreaBomb:
                    result.SpecialActivations.Add(new SpecialActivation(
                        SpecialActivationKind.AreaBomb, source));
                    AddAreaBomb(board, source, clear, new HashSet<BoardPos>());
                    break;
                case GemSpecialType.Line:
                    result.SpecialActivations.Add(new SpecialActivation(
                        SpecialActivationKind.Line, source));
                    for (int offset = -2; offset <= 2; offset++)
                    {
                        int row = source.Row + offset;
                        int column = source.Col + offset;
                        if (row >= 0 && row < board.Rows)
                            clear.Add(new BoardPos(row, source.Col));
                        if (column >= 0 && column < board.Columns)
                            clear.Add(new BoardPos(source.Row, column));
                    }
                    break;
                case GemSpecialType.Lightning:
                    AddLightningTargets(board, source, clear);
                    result.SpecialActivations.Add(new SpecialActivation(
                        SpecialActivationKind.Lightning, source));
                    break;
            }
        }

        static void AddFallbackBaguaActivations(BoardSnapshot board, MoveResult move,
            MatchResult result)
        {
            int sequence = 0;
            if (board.SpecialAt(move.From) == GemSpecialType.Bagua)
                result.SpecialActivations.Add(new SpecialActivation(
                    SpecialActivationKind.FallbackBaguaShuffle, move.From,
                    sequenceIndex: sequence++));
            if (board.SpecialAt(move.To) == GemSpecialType.Bagua)
                result.SpecialActivations.Add(new SpecialActivation(
                    SpecialActivationKind.FallbackBaguaShuffle, move.To,
                    sequenceIndex: sequence));
        }

        static bool IsFallbackSpecialSwap(BoardSnapshot board, MoveResult move)
        {
            GemSpecialType first = board.SpecialAt(move.From);
            GemSpecialType second = board.SpecialAt(move.To);
            return first != GemSpecialType.None && second != GemSpecialType.None
                && first != GemSpecialType.Rainbow && second != GemSpecialType.Rainbow
                && !IsBombBombSwap(board, move)
                && !(first == GemSpecialType.Line && second == GemSpecialType.Line)
                && !(first == GemSpecialType.AreaBomb && second == GemSpecialType.Line)
                && !(second == GemSpecialType.AreaBomb && first == GemSpecialType.Line);
        }

        static bool IsBombBombSwap(BoardSnapshot board, MoveResult move) =>
            board.SpecialAt(move.From) == GemSpecialType.AreaBomb
            && board.SpecialAt(move.To) == GemSpecialType.AreaBomb;

        static void ResolveRainbowLineCombo(BoardSnapshot board,
            BoardPos rainbowPosition, BoardPos linePosition,
            HashSet<BoardPos> clear, HashSet<BoardPos> handledSpecials,
            MatchResult result)
        {
            clear.Add(rainbowPosition);
            clear.Add(linePosition);
            handledSpecials.Add(rainbowPosition);
            handledSpecials.Add(linePosition);

            List<int> rows = Enumerable.Range(0, board.Rows).ToList();
            List<int> columns = Enumerable.Range(0, board.Columns).ToList();
            ShuffleInPlace(rows);
            ShuffleInPlace(columns);
            int totalLines = UnityEngine.Random.Range(4, 7);
            int horizontalLines = UnityEngine.Random.Range(1, totalLines);
            int verticalLines = totalLines - horizontalLines;

            for (int index = 0; index < horizontalLines; index++)
            {
                int row = rows[index];
                result.SpecialActivations.Add(new SpecialActivation(
                    SpecialActivationKind.RainbowLineHorizontal,
                    new BoardPos(row, 0)));
                for (int column = 0; column < board.Columns; column++)
                    clear.Add(new BoardPos(row, column));
            }
            for (int index = 0; index < verticalLines; index++)
            {
                int column = columns[index];
                result.SpecialActivations.Add(new SpecialActivation(
                    SpecialActivationKind.RainbowLineVertical,
                    new BoardPos(0, column)));
                for (int row = 0; row < board.Rows; row++)
                    clear.Add(new BoardPos(row, column));
            }

        }

        static void ResolveRainbowLightningCombo(BoardSnapshot board,
            BoardPos rainbowPosition, BoardPos lightningPosition,
            HashSet<BoardPos> clear, HashSet<BoardPos> handledSpecials,
            MatchResult result)
        {
            clear.Add(rainbowPosition);
            clear.Add(lightningPosition);
            handledSpecials.Add(rainbowPosition);
            handledSpecials.Add(lightningPosition);

            for (int sequence = 0; sequence < 3; sequence++)
            {
                List<BoardPos> candidates = new List<BoardPos>();
                for (int row = 0; row < board.Rows; row++)
                for (int column = 0; column < board.Columns; column++)
                {
                    BoardPos position = new BoardPos(row, column);
                    if (!clear.Contains(position) && IsOrdinaryGem(board, position))
                        candidates.Add(position);
                }

                if (candidates.Count == 0)
                    break;

                ShuffleInPlace(candidates);
                int targetCount = Mathf.Min(
                    UnityEngine.Random.Range(5, 11), candidates.Count);
                for (int index = 0; index < targetCount; index++)
                {
                    BoardPos target = candidates[index];
                    clear.Add(target);
                    result.SpecialActivations.Add(new SpecialActivation(
                        SpecialActivationKind.RainbowLightningCombo,
                        lightningPosition, targetPosition: target,
                        sequenceIndex: sequence));
                }
            }
        }

        bool ResolveRainbowBombCombo(BoardSnapshot board, BoardPos rainbowPosition,
            BoardPos bombPosition, HashSet<BoardPos> clear,
            HashSet<BoardPos> reserved, HashSet<BoardPos> handledSpecials,
            MatchResult result, bool rollConversions)
        {
            GemType bombGemType = board.TypeAt(bombPosition);
            clear.Add(rainbowPosition);
            handledSpecials.Add(rainbowPosition);
            result.SpecialActivations.Add(new SpecialActivation(
                SpecialActivationKind.RainbowBombCombo, rainbowPosition, bombGemType));

            List<BoardPos> convertedBombs = new List<BoardPos>();
            if (rollConversions)
            {
                List<BoardPos> normalGems = new List<BoardPos>();
                for (int row = 0; row < board.Rows; row++)
                for (int column = 0; column < board.Columns; column++)
                {
                    BoardPos position = new BoardPos(row, column);
                    if (board.TypeAt(position) == bombGemType
                        && board.SpecialAt(position) == GemSpecialType.None)
                        normalGems.Add(position);
                }

                if (normalGems.Count >= 3)
                {
                    ShuffleInPlace(normalGems);
                    int conversionCount = UnityEngine.Random.Range(
                        3, Mathf.Min(4, normalGems.Count) + 1);
                    for (int index = 0; index < conversionCount; index++)
                    {
                        BoardPos position = normalGems[index];
                        convertedBombs.Add(position);
                        result.CreatedSpecials.Add(new SpecialGemCreation(
                            GemSpecialType.AreaBomb, bombGemType, position, null));
                    }
                }
            }

            List<BoardPos> bombsToActivate = new List<BoardPos>(convertedBombs);
            for (int row = 0; row < board.Rows; row++)
            for (int column = 0; column < board.Columns; column++)
            {
                BoardPos position = new BoardPos(row, column);
                if (board.SpecialAt(position) == GemSpecialType.AreaBomb)
                    bombsToActivate.Add(position);
            }

            foreach (BoardPos position in bombsToActivate)
            {
                handledSpecials.Add(position);
                result.SpecialActivations.Add(new SpecialActivation(
                    SpecialActivationKind.AreaBomb, position));
                for (int row = position.Row - 1; row <= position.Row + 1; row++)
                for (int column = position.Col - 1; column <= position.Col + 1; column++)
                {
                    if (row < 0 || row >= board.Rows
                        || column < 0 || column >= board.Columns)
                        continue;
                    AddPosition(clear, reserved, new BoardPos(row, column));
                }
            }
            return true;
        }

        static bool TryGetRainbowSpecialPair(BoardSnapshot board, MoveResult move,
            GemSpecialType partnerType, out BoardPos rainbowPosition,
            out BoardPos partnerPosition)
        {
            if (board.SpecialAt(move.From) == GemSpecialType.Rainbow
                && board.SpecialAt(move.To) == partnerType)
            {
                rainbowPosition = move.From;
                partnerPosition = move.To;
                return true;
            }
            if (board.SpecialAt(move.To) == GemSpecialType.Rainbow
                && board.SpecialAt(move.From) == partnerType)
            {
                rainbowPosition = move.To;
                partnerPosition = move.From;
                return true;
            }
            rainbowPosition = default;
            partnerPosition = default;
            return false;
        }

        static bool IsColoredGemType(GemType type) =>
            type >= GemType.Attack && type <= GemType.Soul;

        static void ShuffleInPlace<T>(IList<T> items)
        {
            for (int index = items.Count - 1; index > 0; index--)
            {
                int other = UnityEngine.Random.Range(0, index + 1);
                (items[index], items[other]) = (items[other], items[index]);
            }
        }

        static bool IsFullLineSwapCombination(BoardSnapshot board, MoveResult move)
        {
            GemSpecialType first = board.SpecialAt(move.From);
            GemSpecialType second = board.SpecialAt(move.To);
            return (first == GemSpecialType.Line && second == GemSpecialType.Line)
                || (first == GemSpecialType.AreaBomb && second == GemSpecialType.Line)
                || (second == GemSpecialType.AreaBomb && first == GemSpecialType.Line);
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
                if (!clear.Contains(position) && !position.Equals(source)
                    && IsOrdinaryGem(board, position))
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
                    for (int offset = -2; offset <= 2; offset++)
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
                else if (special == GemSpecialType.Lightning)
                {
                    result.SpecialActivations.Add(new SpecialActivation(
                        SpecialActivationKind.Lightning, position));
                    AddLightningTargets(board, position, clear);
                }
                else if (special == GemSpecialType.Bagua)
                {
                    result.SpecialActivations.Add(new SpecialActivation(
                        SpecialActivationKind.FallbackBaguaShuffle, position));
                }
            }
        }

        static void PreserveUnconsumedRainbows(BoardSnapshot board,
            HashSet<BoardPos> clear, ISet<BoardPos> consumedRainbows)
        {
            for (int row = 0; row < board.Rows; row++)
            for (int column = 0; column < board.Columns; column++)
            {
                BoardPos position = new BoardPos(row, column);
                if (board.SpecialAt(position) != GemSpecialType.Rainbow
                    && board.TypeAt(position) != GemType.Rainbow)
                    continue;
                if (consumedRainbows.Contains(position))
                    continue;
                clear.Remove(position);
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

        static void AddClearedPositions(BoardSnapshot board, HashSet<BoardPos> clear,
            MatchResult result)
        {
            result.ClearPositions.AddRange(clear.OrderBy(position => position.Row)
                .ThenBy(position => position.Col));
            foreach (BoardPos position in result.ClearPositions)
            {
                GemType type = board.TypeAt(position);
                result.GemCounts.TryGetValue(type, out int count);
                result.GemCounts[type] = count + 1;
            }
        }

        static void AddAll(BoardSnapshot board, HashSet<BoardPos> clear, HashSet<BoardPos> reserved)
        {
            for (int row = 0; row < board.Rows; row++)
            for (int column = 0; column < board.Columns; column++)
                AddPosition(clear, reserved, new BoardPos(row, column));
        }
    }
}
