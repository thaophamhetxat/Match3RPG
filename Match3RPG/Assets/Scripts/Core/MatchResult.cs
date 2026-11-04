using System.Collections.Generic;

namespace Match3RPG.Core
{
    public enum SpecialActivationKind
    {
        Line,
        BombLineCombo,
        AreaBomb,
        Lightning,
        BaguaShuffle,
        RainbowColor,
        RainbowFullBoard,
        LineLineCombo,
        RainbowBaguaCombo,
        RainbowBombCombo,
        RainbowLineHorizontal,
        RainbowLineVertical,
        RainbowLightningCombo,
        AreaBombFiveByFive,
        FallbackBaguaShuffle
    }

    public readonly struct SpecialActivation
    {
        public SpecialActivationKind Kind { get; }
        public BoardPos Position { get; }
        public GemType? TargetGemType { get; }
        public BoardPos? TargetPosition { get; }
        public int SequenceIndex { get; }

        public SpecialActivation(SpecialActivationKind kind, BoardPos position,
            GemType? targetGemType = null, BoardPos? targetPosition = null,
            int sequenceIndex = 0)
        {
            Kind = kind;
            Position = position;
            TargetGemType = targetGemType;
            TargetPosition = targetPosition;
            SequenceIndex = sequenceIndex;
        }
    }

    public sealed class MatchSegment
    {
        public GemType Type { get; }
        public bool IsHorizontal { get; }
        public IReadOnlyList<BoardPos> Positions { get; }
        public int Length => Positions.Count;

        public MatchSegment(GemType type, bool isHorizontal, IReadOnlyList<BoardPos> positions)
        {
            Type = type;
            IsHorizontal = isHorizontal;
            Positions = positions;
        }
    }

    public sealed class SpecialGemCreation
    {
        public GemSpecialType SpecialType { get; }
        public GemType GemType { get; }
        public BoardPos Position { get; }
        public MatchSegment Segment { get; }
        public bool IsTLBomb { get; }

        public SpecialGemCreation(GemSpecialType specialType, GemType gemType,
            BoardPos position, MatchSegment segment, bool isTLBomb = false)
        {
            SpecialType = specialType;
            GemType = gemType;
            Position = position;
            Segment = segment;
            IsTLBomb = isTLBomb;
        }
    }

    public sealed class MatchResult
    {
        public List<BoardPos> ClearPositions { get; } = new List<BoardPos>();
        public List<MatchSegment> Segments { get; } = new List<MatchSegment>();
        public List<SpecialGemCreation> CreatedSpecials { get; } = new List<SpecialGemCreation>();
        public List<GemSpecialType> RefillSpecials { get; } = new List<GemSpecialType>();
        public List<SpecialActivation> SpecialActivations { get; } = new List<SpecialActivation>();
        public Dictionary<GemType, int> GemCounts { get; } = new Dictionary<GemType, int>();
        public GemSpecialType CreatedSpecial { get; internal set; }
        public GemType CreatedGemType { get; internal set; }
        public BoardPos SpecialPosition { get; internal set; }
        public bool HasCreatedSpecial => CreatedSpecial != GemSpecialType.None;
        public bool GrantsExtraTurn { get; internal set; }
        public int ExtraTurnsGranted { get; internal set; }
        public int CreatedLineCount { get; internal set; }
        public int CreatedAreaBombCount { get; internal set; }
        public int CreatedRainbowCount { get; internal set; }
        public int CreatedTLBombCount { get; internal set; }
        public int LongestMatch { get; internal set; }
        public int ComboIndex { get; internal set; }
        public int ClearedCount => ClearPositions.Count;
        public bool HasMatch => ClearPositions.Count > 0;
    }
}