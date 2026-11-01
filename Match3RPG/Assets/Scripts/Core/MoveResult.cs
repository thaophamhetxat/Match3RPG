using System.Collections.Generic;
using System.Linq;

namespace Match3RPG.Core
{
    public readonly struct StarChartMatchRewards
    {
        public int StandardDiamonds { get; }
        public int SpecialDiamonds { get; }
        public IReadOnlyDictionary<GemType, int> BaseGemCounts { get; }

        internal StarChartMatchRewards(int standardDiamonds, int specialDiamonds,
            Dictionary<GemType, int> baseGemCounts)
        {
            StandardDiamonds = standardDiamonds;
            SpecialDiamonds = specialDiamonds;
            BaseGemCounts = baseGemCounts;
        }
    }

    public static class StarChartMatchRewardCalculator
    {
        public static StarChartMatchRewards Calculate(MatchResult match)
        {
            if (match == null)
                throw new System.ArgumentNullException(nameof(match));

            Dictionary<GemType, int> baseGemCounts = new Dictionary<GemType, int>();
            HashSet<BoardPos> matchedPositions = new HashSet<BoardPos>();
            List<MatchSegment> segments = new List<MatchSegment>();
            foreach (MatchSegment segment in match.Segments)
            {
                if (segment == null || segment.Length < 3)
                    continue;

                segments.Add(segment);
                foreach (BoardPos position in segment.Positions)
                {
                    if (!IsBaseGemType(segment.Type) || !matchedPositions.Add(position))
                        continue;
                    baseGemCounts.TryGetValue(segment.Type, out int count);
                    baseGemCounts[segment.Type] = count + 1;
                }
            }

            int standardDiamonds = 0;
            int specialDiamonds = 0;
            bool[] visited = new bool[segments.Count];
            for (int start = 0; start < segments.Count; start++)
            {
                if (visited[start])
                    continue;

                Queue<int> pending = new Queue<int>();
                HashSet<BoardPos> shapePositions = new HashSet<BoardPos>();
                bool isIntersectingShape = false;
                visited[start] = true;
                pending.Enqueue(start);

                while (pending.Count > 0)
                {
                    MatchSegment current = segments[pending.Dequeue()];
                    foreach (BoardPos position in current.Positions)
                        shapePositions.Add(position);

                    for (int candidateIndex = 0; candidateIndex < segments.Count; candidateIndex++)
                    {
                        MatchSegment candidate = segments[candidateIndex];
                        if (visited[candidateIndex] || candidate.Type != current.Type)
                            continue;

                        bool intersects = false;
                        foreach (BoardPos position in candidate.Positions)
                            if (current.Positions.Contains(position))
                            {
                                intersects = true;
                                break;
                            }
                        if (!intersects)
                            continue;

                        if (candidate.IsHorizontal != current.IsHorizontal)
                            isIntersectingShape = true;
                        visited[candidateIndex] = true;
                        pending.Enqueue(candidateIndex);
                    }
                }

                if (isIntersectingShape || shapePositions.Count >= 5)
                    specialDiamonds = checked(specialDiamonds + 2);
                else if (shapePositions.Count == 4)
                    specialDiamonds = checked(specialDiamonds + 1);
                else if (shapePositions.Count == 3)
                    standardDiamonds = checked(standardDiamonds + 3);
            }

            return new StarChartMatchRewards(standardDiamonds, specialDiamonds,
                baseGemCounts);
        }

        static bool IsBaseGemType(GemType type) =>
            type >= GemType.Attack && type <= GemType.Soul;
    }

    public sealed class MoveResult
    {
        public BoardPos From { get; }
        public BoardPos To { get; }
        public bool IsValid { get; internal set; }
        public bool IsDirectActivation { get; internal set; }
        public bool IsBaguaShuffle { get; internal set; }
        public bool WasValid => IsValid;
        public bool WasSpecialActivation { get; internal set; }
        public int Score { get; set; }
        public MatchResult InitialMatch { get; internal set; }
        public Dictionary<GemType, int> GemCounts { get; } = new Dictionary<GemType, int>();
        public IReadOnlyDictionary<GemType, int> MatchedGemCounts => GemCounts;
        public bool GrantsExtraTurn { get; internal set; }
        public int ExtraTurnsGranted { get; internal set; }
        public bool CreatedMatch4 { get; internal set; }
        public bool CreatedMatch5 { get; internal set; }
        public int CreatedLineCount { get; private set; }
        public int CreatedAreaBombCount { get; private set; }
        public int CreatedRainbowCount { get; private set; }
        public int CreatedTLBombCount { get; private set; }
        public bool WasInitialMove { get; internal set; } = true;
        public int ComboCount { get; internal set; }
        public int CascadeCount { get; internal set; }
        public int LongestMatch { get; internal set; }
        public int StandardDiamondReward { get; private set; }
        public int SpecialDiamondReward { get; private set; }
        readonly Dictionary<GemType, int> baseGemMatchCounts =
            new Dictionary<GemType, int>();
        public IReadOnlyDictionary<GemType, int> BaseGemMatchCounts =>
            baseGemMatchCounts;

        public MoveResult(BoardPos from, BoardPos to)
        {
            From = from;
            To = to;
        }

        internal void AddMatch(MatchResult match)
        {
            bool isInitialMatch = ComboCount == 0;
            StarChartMatchRewards starChartRewards =
                StarChartMatchRewardCalculator.Calculate(match);
            StandardDiamondReward = checked(StandardDiamondReward
                + starChartRewards.StandardDiamonds);
            SpecialDiamondReward = checked(SpecialDiamondReward
                + starChartRewards.SpecialDiamonds);
            foreach (KeyValuePair<GemType, int> pair in starChartRewards.BaseGemCounts)
            {
                baseGemMatchCounts.TryGetValue(pair.Key, out int existing);
                baseGemMatchCounts[pair.Key] = checked(existing + pair.Value);
            }

            foreach (KeyValuePair<GemType, int> pair in match.GemCounts)
            {
                GemCounts.TryGetValue(pair.Key, out int existing);
                GemCounts[pair.Key] = checked(existing + pair.Value);
            }

            ComboCount++;
            CascadeCount = ComboCount - 1;
            if (match.LongestMatch > LongestMatch) LongestMatch = match.LongestMatch;
            CreatedMatch4 |= match.Segments.Any(segment => segment.Length == 4);
            CreatedMatch5 |= match.Segments.Any(segment => segment.Length >= 5);
            CreatedLineCount += match.CreatedLineCount;
            CreatedAreaBombCount += match.CreatedAreaBombCount;
            CreatedRainbowCount += match.CreatedRainbowCount;
            CreatedTLBombCount += match.CreatedTLBombCount;
            ExtraTurnsGranted += match.ExtraTurnsGranted;
            GrantsExtraTurn = ExtraTurnsGranted > 0;
            if (isInitialMatch)
            {
                InitialMatch = match;
            }
        }
    }
}