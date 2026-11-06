using System.Collections.Generic;
using System.Linq;
using Match3RPG.Board;
using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Battle
{
    public sealed class BossAI : MonoBehaviour
    {
        public MoveResult ChooseMove(BoardManager board, float bossHealthRatio, float bossRageRatio = 0f)
        {
            List<MoveResult> legalMoves = board.GetLegalMoves();
            if (legalMoves.Count == 0) return null;
            List<MoveResult> ranked = legalMoves.OrderByDescending(move => Score(move, bossHealthRatio, bossRageRatio)).ToList();
            return ChooseMove(ranked, UnityEngine.Random.value, ranked.Count > 1 ? UnityEngine.Random.Range(1, ranked.Count) : 0);
        }

        public MoveResult ChooseMove(IReadOnlyList<MoveResult> rankedMoves, float choiceRoll, int alternativeIndex)
        {
            if (rankedMoves == null || rankedMoves.Count == 0) return null;
            if (choiceRoll < .7f || rankedMoves.Count == 1) return rankedMoves[0];
            int index = Mathf.Clamp(alternativeIndex, 1, rankedMoves.Count - 1);
            return rankedMoves[index];
        }

        public int Score(MoveResult move, float bossHealthRatio, float bossRageRatio = 0f)
        {
            if (move == null || !move.IsValid || move.InitialMatch == null) return int.MinValue;
            int score = 0;
            score += move.InitialMatch.CreatedRainbowCount * 900;
            score += move.InitialMatch.CreatedAreaBombCount * 500;
            score += move.InitialMatch.CreatedTLBombCount * 75;
            score += move.InitialMatch.CreatedLineCount * 180;

            foreach (SpecialActivation activation in move.InitialMatch.SpecialActivations)
            {
                switch (activation.Kind)
                {
                    case SpecialActivationKind.RainbowFullBoard: score += 1500; break;
                    case SpecialActivationKind.RainbowBaguaCombo: score += 800; break;
                    case SpecialActivationKind.RainbowBombCombo: score += 800; break;
                    case SpecialActivationKind.RainbowColor: score += 700; break;
                    case SpecialActivationKind.AreaBomb: score += 400; break;
                    case SpecialActivationKind.Lightning: score += 250; break;
                    case SpecialActivationKind.BaguaShuffle: score += 100; break;
                    case SpecialActivationKind.Line: score += 300; break;
                    case SpecialActivationKind.LineLineCombo: score += 600; break;
                    case SpecialActivationKind.BombLineCombo: score += 450; break;
                    case SpecialActivationKind.RainbowLineHorizontal: score += 250; break;
                    case SpecialActivationKind.RainbowLineVertical: score += 250; break;
                    case SpecialActivationKind.RainbowLightningCombo: score += 30; break;
                    case SpecialActivationKind.AreaBombFiveByFive: score += 800; break;
                    case SpecialActivationKind.FallbackBaguaShuffle: score += 100; break;
                }
            }

            foreach (KeyValuePair<GemType, int> pair in move.InitialMatch.GemCounts)
            {
                int perGem = pair.Key switch
                {
                    GemType.Attack => 120,
                    GemType.Health => bossHealthRatio < .4f ? 110 : 25,
                    GemType.Mana => 40,
                    GemType.Rage => bossRageRatio >= 1f
                        ? 2
                        : Mathf.RoundToInt(Mathf.Lerp(55f, 120f, Mathf.Clamp01(bossRageRatio))),
                    GemType.ExpDrain => 95,
                    GemType.Soul => 5,
                    _ => 20
                };
                score += perGem * pair.Value;
            }

            if (bossRageRatio >= 1f && move.InitialMatch.GemCounts.ContainsKey(GemType.Attack))
                score += 200;
            score += Mathf.Max(0, move.InitialMatch.LongestMatch - 3) * 25;
            score += move.InitialMatch.ExtraTurnsGranted * 35;
            return score;
        }
    }
}