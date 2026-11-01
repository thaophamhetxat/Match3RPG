using System;
using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Board
{
    public enum BoardVFXEffect
    {
        BombRadiusGlow,
        Bomb3x3
    }

    public enum BoardVFXMode
    {
        CodeDrawn
    }

    [CreateAssetMenu(menuName = "Match3 RPG/Board/Board VFX Catalog")]
    public sealed class BoardVFXCatalog : ScriptableObject
    {
        [Serializable]
        public struct EffectEntry
        {
            public BoardVFXEffect effect;
            public BoardVFXMode mode;
            public BoardBurstStyle style;
            public Color color;
            public float scale;
            public float duration;
        }

        [Serializable]
        public struct GemStyleEntry
        {
            public GemType gemType;
            public BoardBurstStyle style;
            public Color color;
        }

        [Serializable]
        public struct GemColorEntry
        {
            public GemType gemType;
            public Color color;
        }

        [SerializeField] EffectEntry[] entries = Array.Empty<EffectEntry>();
        [SerializeField] GemStyleEntry[] bombStyles = Array.Empty<GemStyleEntry>();
        [SerializeField] GemColorEntry[] bombGlowColors = Array.Empty<GemColorEntry>();

        public bool TryGetEffect(BoardVFXEffect effect, out EffectEntry entry)
        {
            if (entries != null)
            {
                foreach (EffectEntry candidate in entries)
                {
                    if (candidate.effect != effect)
                        continue;
                    entry = candidate;
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public bool TryGetBombStyle(GemType gemType, out GemStyleEntry entry)
        {
            if (bombStyles != null)
            {
                foreach (GemStyleEntry candidate in bombStyles)
                {
                    if (candidate.gemType != gemType)
                        continue;
                    entry = candidate;
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public bool TryGetBombGlowColor(GemType gemType, out Color color)
        {
            if (bombGlowColors != null)
            {
                foreach (GemColorEntry candidate in bombGlowColors)
                {
                    if (candidate.gemType != gemType)
                        continue;
                    color = candidate.color;
                    return true;
                }
            }

            color = default;
            return false;
        }
    }
}
