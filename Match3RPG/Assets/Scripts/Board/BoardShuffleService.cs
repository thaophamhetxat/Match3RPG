using System;
using System.Collections;
using System.Collections.Generic;
using Match3RPG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Board
{
    public sealed class BoardShuffleService
    {
        readonly int maximumAttempts;
        readonly float animationDuration;

        public BoardShuffleService(int attempts = 100, float duration = .25f)
        {
            maximumAttempts = Mathf.Clamp(attempts, 1, 100);
            animationDuration = Mathf.Max(0f, duration);
        }

        public IEnumerator Shuffle(BoardManager board, Action<bool> completed,
            bool allowRegeneration = true)
        {
            yield return null;
            List<GemView> originalOrder = board.GetGemsInLayoutOrder();
            Vector2[] slotPositions = board.GetSlotAnchoredPositions();
            List<GemView> candidate = new List<GemView>(originalOrder);

            for (int attempt = 0; attempt < maximumAttempts; attempt++)
            {
                ShuffleInPlace(candidate);
                if (!board.IsValidGemOrder(candidate)) continue;
                yield return AnimateAndApply(board, originalOrder, candidate, slotPositions);
                board.ValidateGemVisualsAfterBoardChange();
                completed?.Invoke(true);
                yield break;
            }

            if (!allowRegeneration)
            {
                completed?.Invoke(false);
                yield break;
            }

            Debug.LogWarning("No valid shuffled permutation found after 100 attempts; regenerating a playable layout.", board);
            bool fallbackReady = board.RegenerateFallbackBoard();
            if (fallbackReady)
            {
                List<GemView> fallbackOrder = board.GetGemsInLayoutOrder();
                yield return AnimateAndApply(board, originalOrder, fallbackOrder, slotPositions);
            }
            if (fallbackReady)
                board.ValidateGemVisualsAfterBoardChange();
            completed?.Invoke(fallbackReady);
        }

        IEnumerator AnimateAndApply(BoardManager board, List<GemView> originalOrder, List<GemView> newOrder, Vector2[] slots)
        {
            Dictionary<GemView, Vector2> startPositions = new Dictionary<GemView, Vector2>(originalOrder.Count);
            for (int index = 0; index < originalOrder.Count; index++)
                startPositions[originalOrder[index]] = slots[index];

            GridLayoutGroup layout = board.BoardRoot.GetComponent<GridLayoutGroup>();
            bool wasEnabled = layout != null && layout.enabled;
            if (layout != null) layout.enabled = false;
            board.ApplyGemOrder(newOrder);

            if (animationDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < animationDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float normalized = Mathf.Clamp01(elapsed / animationDuration);
                    float eased = normalized * normalized * (3f - 2f * normalized);
                    for (int index = 0; index < newOrder.Count; index++)
                    {
                        RectTransform rect = (RectTransform)newOrder[index].transform;
                        rect.anchoredPosition = Vector2.Lerp(startPositions[newOrder[index]], slots[index], eased);
                    }
                    yield return null;
                }
            }

            for (int index = 0; index < newOrder.Count; index++)
                ((RectTransform)newOrder[index].transform).anchoredPosition = slots[index];
            if (layout != null)
            {
                layout.enabled = wasEnabled;
                if (wasEnabled) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)board.BoardRoot);
            }
        }

        static void ShuffleInPlace<T>(IList<T> items)
        {
            for (int index = items.Count - 1; index > 0; index--)
            {
                int other = UnityEngine.Random.Range(0, index + 1);
                (items[index], items[other]) = (items[other], items[index]);
            }
        }
    }
}