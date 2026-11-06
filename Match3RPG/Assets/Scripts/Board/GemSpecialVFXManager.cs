using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Match3RPG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Board
{
    public sealed class GemSpecialVFXManager : MonoBehaviour
    {
        const float LineEffectSeconds = LightningLineVFX.TotalLifetime;
        const float ExplosionSeconds = .38f;
        const float CellBurstSeconds = .26f;
        const float BaguaEffectSeconds = .32f;
        const float ClearDelaySeconds = .16f;
        readonly HashSet<GameObject> activeEffects = new HashSet<GameObject>();
        readonly HashSet<GemType> warnedUnknownBombTypes = new HashSet<GemType>();
        readonly HashSet<BoardVFXEffect> warnedMissingCatalogEntries =
            new HashSet<BoardVFXEffect>();
        readonly List<LightningLineVFX> lightningPool = new List<LightningLineVFX>();
        readonly List<Color> bombGlowColors = new List<Color>();
        readonly List<int> bombGlowOverlaps = new List<int>();
        RectTransform vfxLayer;
        GridLayoutGroup grid;
        BoardCellView[,] boardCells;
        BoardVFXCatalog boardVfxCatalog;

        void OnDisable()
        {
            StopAllCoroutines();
            foreach (GameObject effect in activeEffects)
                if (effect != null)
                    Destroy(effect);
            activeEffects.Clear();
            foreach (LightningLineVFX lightning in lightningPool)
                if (lightning != null)
                    Destroy(lightning.gameObject);
            lightningPool.Clear();
        }

        public void Configure(RectTransform boardViewport, RectTransform board,
            Sprite bomb, Sprite fullBoard,
            BoardCellView[,] stableBoardCells = null, RectTransform boardVfxLayer = null,
            BoardVFXCatalog catalog = null)
        {
            vfxLayer = boardVfxLayer != null ? boardVfxLayer : boardViewport;
            grid = board != null ? board.GetComponent<GridLayoutGroup>() : null;
            boardCells = stableBoardCells;
            boardVfxCatalog = catalog != null
                ? catalog
                : Resources.Load<BoardVFXCatalog>("BoardVFX/BoardVFXCatalog");
            if (boardVfxCatalog == null)
                Debug.LogError("Board VFX catalog is missing at Resources/BoardVFX/BoardVFXCatalog.", this);
        }

        public void RefreshBombRadiusGlows(IEnumerable<GemView> boardGems)
        {
            if (boardCells == null || boardVfxCatalog == null)
                return;

            int rows = boardCells.GetLength(0);
            int columns = boardCells.GetLength(1);
            int cellCount = rows * columns;
            while (bombGlowColors.Count < cellCount)
            {
                bombGlowColors.Add(Color.clear);
                bombGlowOverlaps.Add(0);
            }
            for (int index = 0; index < cellCount; index++)
            {
                bombGlowColors[index] = Color.clear;
                bombGlowOverlaps[index] = 0;
            }

            bool hasGlowEntry = boardVfxCatalog.TryGetEffect(
                BoardVFXEffect.BombRadiusGlow, out BoardVFXCatalog.EffectEntry glowEntry);
            if (!hasGlowEntry || glowEntry.mode != BoardVFXMode.CodeDrawn)
            {
                if (warnedMissingCatalogEntries.Add(BoardVFXEffect.BombRadiusGlow))
                    Debug.LogError("Board VFX catalog has no valid CodeDrawn BombRadiusGlow entry.", this);
                return;
            }

            if (boardGems != null)
            {
                foreach (GemView gem in boardGems)
                {
                    if (gem == null || gem.SpecialType != GemSpecialType.AreaBomb)
                        continue;

                    Color color = glowEntry.color;
                    if (!boardVfxCatalog.TryGetBombGlowColor(gem.Type, out color))
                    {
                        WarnUnknownBombType(gem.Type);
                        continue;
                    }

                    for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
                    for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
                    {
                        if (rowOffset == 0 && columnOffset == 0)
                            continue;
                        int row = gem.Position.Row + rowOffset;
                        int column = gem.Position.Col + columnOffset;
                        if (row < 0 || row >= rows || column < 0 || column >= columns
                            || boardCells[row, column] == null)
                            continue;

                        int index = row * columns + column;
                        Color accumulated = bombGlowColors[index];
                        accumulated += color;
                        bombGlowColors[index] = accumulated;
                        bombGlowOverlaps[index]++;
                    }
                }
            }

            float scale = Mathf.Max(1f, glowEntry.scale);
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                BoardCellView cell = boardCells[row, column];
                if (cell == null)
                    continue;
                int index = row * columns + column;
                int count = bombGlowOverlaps[index];
                Color color = count > 0 ? bombGlowColors[index] / count : Color.clear;
                cell.GetOrCreateBombRadiusGlow().SetGlow(color, count, scale);
            }
        }

        public IEnumerator PlayActivations(IReadOnlyList<SpecialActivation> activations,
            IReadOnlyList<GemView> clearedGems, Action clearMatchedGems = null)
        {
            if (vfxLayer == null || grid == null || boardCells == null)
            {
                clearMatchedGems?.Invoke();
                yield break;
            }

            if (activations != null && activations.Any(activation =>
                activation.Kind == SpecialActivationKind.RainbowLightningCombo))
            {
                yield return PlayRainbowLightningWaves(activations, clearMatchedGems);
                yield break;
            }

            float duration = 0f;
            int lightningIndex = 0;
            List<BoardBurstVFX.Burst> bursts = new List<BoardBurstVFX.Burst>();
            List<BoardBurstVFX.Arc> arcs = new List<BoardBurstVFX.Arc>();
            List<BoardPos> fallbackBaguaShuffles = new List<BoardPos>();
            bool rainbowLineFlashesAdded = false;
            bool fullBoardClear = activations != null
                && activations.Any(activation => activation.Kind == SpecialActivationKind.RainbowFullBoard);

            if (fullBoardClear)
            {
                bursts.Add(new BoardBurstVFX.Burst(vfxLayer.rect.center,
                    Mathf.Max(vfxLayer.rect.width, vfxLayer.rect.height),
                    BoardBurstStyle.RainbowBoard));
                duration = ExplosionSeconds;
            }
            else
            {
                if (activations != null)
                {
                    for (int i = 0; i < activations.Count; i++)
                    {
                        SpecialActivation activation = activations[i];
                        switch (activation.Kind)
                        {
                            case SpecialActivationKind.Line:
                                StartCoroutine(ShowLineCross(lightningIndex, activation.Position));
                                lightningIndex += 2;
                                AddCellFlashes(bursts, clearedGems, i);
                                duration = Mathf.Max(duration, LineEffectSeconds);
                                break;
                            case SpecialActivationKind.LineLineCombo:
                                StartCoroutine(ShowBoardLine(lightningIndex++, true,
                                    activation.Position.Row));
                                StartCoroutine(ShowBoardLine(lightningIndex++, false,
                                    activation.Position.Col));
                                AddCellFlashes(bursts, clearedGems, i);
                                duration = Mathf.Max(duration, LineEffectSeconds);
                                break;
                            case SpecialActivationKind.RainbowLineHorizontal:
                                StartCoroutine(ShowBoardLine(lightningIndex++, true,
                                    activation.Position.Row));
                                if (!rainbowLineFlashesAdded)
                                {
                                    AddCellFlashes(bursts, clearedGems, i);
                                    rainbowLineFlashesAdded = true;
                                }
                                duration = Mathf.Max(duration, LineEffectSeconds);
                                break;
                            case SpecialActivationKind.RainbowLineVertical:
                                StartCoroutine(ShowBoardLine(lightningIndex++, false,
                                    activation.Position.Col));
                                if (!rainbowLineFlashesAdded)
                                {
                                    AddCellFlashes(bursts, clearedGems, i);
                                    rainbowLineFlashesAdded = true;
                                }
                                duration = Mathf.Max(duration, LineEffectSeconds);
                                break;
                            case SpecialActivationKind.BombLineCombo:
                                for (int offset = -1; offset <= 1; offset++)
                                {
                                    int row = activation.Position.Row + offset;
                                    int column = activation.Position.Col + offset;
                                    if (row >= 0 && row < boardCells.GetLength(0))
                                        StartCoroutine(ShowBoardLine(lightningIndex++, true, row));
                                    if (column >= 0 && column < boardCells.GetLength(1))
                                        StartCoroutine(ShowBoardLine(lightningIndex++, false, column));
                                }
                                AddCellFlashes(bursts, clearedGems, i);
                                duration = Mathf.Max(duration, LineEffectSeconds);
                                break;
                            case SpecialActivationKind.AreaBomb:
                                GemView bomb = clearedGems != null
                                    ? clearedGems.FirstOrDefault(gem => gem != null
                                        && gem.Position.Equals(activation.Position)
                                        && gem.SpecialType == GemSpecialType.AreaBomb)
                                    : null;
                                if (bomb == null)
                                {
                                    Debug.LogError($"AreaBomb activation at ({activation.Position.Row},"
                                        + $"{activation.Position.Col}) has no matching cleared Gem.", this);
                                    StartCoroutine(ShowAreaBomb(activation.Position, GemType.Attack));
                                }
                                else
                                {
                                    StartCoroutine(ShowAreaBomb(activation.Position, bomb.Type));
                                }
                                duration = Mathf.Max(duration, GetBombExplosionDuration());
                                break;
                            case SpecialActivationKind.AreaBombFiveByFive:
                                StartCoroutine(ShowBombFiveByFive(activation.Position,
                                    activation.TargetGemType ?? GemType.Attack));
                                duration = Mathf.Max(duration, GetBombExplosionDuration());
                                break;
                            case SpecialActivationKind.Lightning:
                                bursts.Add(new BoardBurstVFX.Burst(CellCenter(activation.Position),
                                    Mathf.Min(grid.cellSize.x, grid.cellSize.y),
                                    BoardBurstStyle.Electric, i));
                                AddCellFlashes(bursts, clearedGems, i);
                                duration = Mathf.Max(duration, CellBurstSeconds);
                                break;
                            case SpecialActivationKind.BaguaShuffle:
                                PlayBaguaShuffle(activation.Position);
                                break;
                            case SpecialActivationKind.FallbackBaguaShuffle:
                                fallbackBaguaShuffles.Add(activation.Position);
                                break;
                            case SpecialActivationKind.RainbowColor:
                                AddRainbowConnections(activation.Position, activation.TargetGemType,
                                    clearedGems, bursts, arcs);
                                duration = Mathf.Max(duration, LineEffectSeconds);
                                break;
                            case SpecialActivationKind.RainbowBombCombo:
                                AddRainbowConnections(activation.Position, activation.TargetGemType,
                                    clearedGems, bursts, arcs);
                                duration = Mathf.Max(duration, LineEffectSeconds);
                                break;
                            case SpecialActivationKind.RainbowFullBoard:
                                break;
                        }
                    }
                }

                if (activations == null || activations.Count == 0)
                {
                    if (clearedGems != null)
                    {
                        for (int index = 0; index < clearedGems.Count; index++)
                        {
                            GemView gem = clearedGems[index];
                            if (gem == null)
                                continue;
                            bursts.Add(new BoardBurstVFX.Burst(CellCenter(gem.Position),
                                Mathf.Min(grid.cellSize.x, grid.cellSize.y),
                                StyleFor(gem), index));
                        }
                    }
                    duration = bursts.Count > 0 ? CellBurstSeconds : 0f;
                }
            }

            if (bursts.Count > 0 || arcs.Count > 0)
                StartCoroutine(ShowBurstEffect(bursts, arcs, duration));

            if (fallbackBaguaShuffles.Count > 0)
            {
                if (duration > 0f)
                    yield return new WaitForSecondsRealtime(duration);
                foreach (BoardPos position in fallbackBaguaShuffles)
                {
                    PlayBaguaShuffle(position);
                    yield return new WaitForSecondsRealtime(BaguaEffectSeconds);
                }
                clearMatchedGems?.Invoke();
                yield break;
            }

            if (duration <= 0f)
            {
                clearMatchedGems?.Invoke();
                yield break;
            }

            float clearDelay = Mathf.Min(ClearDelaySeconds, duration);
            yield return new WaitForSecondsRealtime(clearDelay);
            clearMatchedGems?.Invoke();
            float remainingDuration = duration - clearDelay;
            if (remainingDuration > 0f)
                yield return new WaitForSecondsRealtime(remainingDuration);
        }

        IEnumerator PlayRainbowLightningWaves(
            IReadOnlyList<SpecialActivation> activations, Action clearMatchedGems)
        {
            float cellSize = Mathf.Min(grid.cellSize.x, grid.cellSize.y);
            for (int sequence = 0; sequence < 3; sequence++)
            {
                List<BoardBurstVFX.Burst> bursts = new List<BoardBurstVFX.Burst>();
                List<BoardBurstVFX.Arc> arcs = new List<BoardBurstVFX.Arc>();
                int targetIndex = 0;
                foreach (SpecialActivation activation in activations)
                {
                    if (activation.Kind != SpecialActivationKind.RainbowLightningCombo
                        || activation.SequenceIndex != sequence
                        || !activation.TargetPosition.HasValue)
                        continue;

                    Vector2 source = CellCenter(activation.Position);
                    Vector2 target = CellCenter(activation.TargetPosition.Value);
                    if (targetIndex == 0)
                        bursts.Add(new BoardBurstVFX.Burst(source, cellSize,
                            BoardBurstStyle.Electric, sequence));
                    arcs.Add(new BoardBurstVFX.Arc(source, target, cellSize,
                        sequence * 10 + targetIndex));
                    bursts.Add(new BoardBurstVFX.Burst(target, cellSize * .82f,
                        BoardBurstStyle.WhiteBlueFlash, sequence * 10 + targetIndex));
                    targetIndex++;
                }

                if (targetIndex == 0)
                    continue;

                StartCoroutine(ShowBurstEffect(bursts, arcs, CellBurstSeconds));
                yield return new WaitForSecondsRealtime(CellBurstSeconds);
            }

            clearMatchedGems?.Invoke();
        }

        public void PlayBaguaShuffle(BoardPos center)
        {
            if (vfxLayer == null || grid == null || boardCells == null)
                return;

            float boardSize = Mathf.Max(vfxLayer.rect.width, vfxLayer.rect.height);
            float cellSize = Mathf.Min(grid.cellSize.x, grid.cellSize.y);
            List<BoardBurstVFX.Burst> bursts = new List<BoardBurstVFX.Burst>
            {
                new BoardBurstVFX.Burst(vfxLayer.rect.center, boardSize,
                    BoardBurstStyle.BaguaClouds),
                new BoardBurstVFX.Burst(CellCenter(center), cellSize * 1.3f,
                    BoardBurstStyle.RunicAura)
            };
            StartCoroutine(ShowBurstEffect(bursts,
                new List<BoardBurstVFX.Arc>(), BaguaEffectSeconds));
        }

        void AddCellFlashes(List<BoardBurstVFX.Burst> bursts,
            IReadOnlyList<GemView> clearedGems, int seed)
        {
            if (clearedGems == null)
                return;
            float cellSize = Mathf.Min(grid.cellSize.x, grid.cellSize.y);
            for (int index = 0; index < clearedGems.Count; index++)
            {
                GemView gem = clearedGems[index];
                if (gem == null)
                    continue;
                bursts.Add(new BoardBurstVFX.Burst(CellCenter(gem.Position),
                    cellSize * .82f, BoardBurstStyle.WhiteBlueFlash, seed + index));
            }
        }

        void AddRainbowConnections(BoardPos origin, GemType? targetType,
            IReadOnlyList<GemView> clearedGems,
            List<BoardBurstVFX.Burst> bursts, List<BoardBurstVFX.Arc> arcs)
        {
            float cellSize = Mathf.Min(grid.cellSize.x, grid.cellSize.y);
            Vector2 start = CellCenter(origin);
            bursts.Add(new BoardBurstVFX.Burst(start, cellSize,
                BoardBurstStyle.WhiteBlueFlash));
            if (clearedGems == null || !targetType.HasValue)
                return;

            int arcIndex = 0;
            foreach (GemView gem in clearedGems)
            {
                if (gem == null || gem.Position.Equals(origin) || gem.Type != targetType.Value
                    || gem.SpecialType != GemSpecialType.None)
                    continue;
                Vector2 target = CellCenter(gem.Position);
                arcs.Add(new BoardBurstVFX.Arc(start, target, cellSize, arcIndex));
                bursts.Add(new BoardBurstVFX.Burst(target, cellSize * .72f,
                    BoardBurstStyle.WhiteBlueFlash, arcIndex));
                arcIndex++;
            }
        }

        static BoardBurstStyle StyleFor(GemView gem)
        {
            switch (gem.SpecialType)
            {
                case GemSpecialType.Line:
                case GemSpecialType.Lightning:
                    return BoardBurstStyle.Electric;
                case GemSpecialType.AreaBomb:
                    return BoardBurstStyle.Fire;
                case GemSpecialType.Rainbow:
                    return BoardBurstStyle.WhiteBlueFlash;
            }

            switch (gem.Type)
            {
                case GemType.Attack: return BoardBurstStyle.Fire;
                case GemType.Mana: return BoardBurstStyle.Water;
                case GemType.ExpDrain: return BoardBurstStyle.Lotus;
                case GemType.Rage: return BoardBurstStyle.Sun;
                case GemType.Health: return BoardBurstStyle.Leaf;
                case GemType.Shield: return BoardBurstStyle.ShieldShards;
                case GemType.Soul: return BoardBurstStyle.Soul;
                case GemType.Rainbow: return BoardBurstStyle.WhiteBlueFlash;
                default: return BoardBurstStyle.Electric;
            }
        }

        IEnumerator ShowBurstEffect(IReadOnlyList<BoardBurstVFX.Burst> bursts,
            IReadOnlyList<BoardBurstVFX.Arc> arcs, float duration)
        {
            GameObject effect = new GameObject("BoardBurstVFX",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(BoardBurstVFX));
            effect.transform.SetParent(vfxLayer, false);
            effect.transform.SetAsLastSibling();
            activeEffects.Add(effect);
            BoardBurstVFX graphic = effect.GetComponent<BoardBurstVFX>();
            graphic.raycastTarget = false;
            if (bursts != null)
                for (int index = 0; index < bursts.Count; index++)
                {
                    BoardBurstVFX.Burst burst = bursts[index];
                    graphic.AddBurst(burst.Position, burst.Size, burst.Style,
                        burst.Seed, burst.Tint);
                }
            if (arcs != null)
                for (int index = 0; index < arcs.Count; index++)
                {
                    BoardBurstVFX.Arc arc = arcs[index];
                    graphic.AddArc(arc.Start, arc.End, arc.CellSize, arc.Seed);
                }
            graphic.Play(duration);
            yield return new WaitForSecondsRealtime(duration);
            activeEffects.Remove(effect);
            if (effect != null)
                Destroy(effect);
        }

        IEnumerator ShowLineCross(int poolIndex, BoardPos center)
        {
            if (!TryGetCrossLineGeometry(center, true,
                out Vector2 horizontalStart, out Vector2 horizontalEnd, out float horizontalCellSize)
                || !TryGetCrossLineGeometry(center, false,
                    out Vector2 verticalStart, out Vector2 verticalEnd, out float verticalCellSize))
                yield break;

            StartCoroutine(GetLightning(poolIndex).Animate(horizontalStart, horizontalEnd, true,
                horizontalCellSize, LineEffectSeconds));
            StartCoroutine(GetLightning(poolIndex + 1).Animate(verticalStart, verticalEnd, false,
                verticalCellSize, LineEffectSeconds));
            yield return new WaitForSecondsRealtime(LineEffectSeconds);
        }

        IEnumerator ShowBoardLine(int poolIndex, bool horizontal, int index)
        {
            if (!TryGetBoardLineGeometry(horizontal, index,
                out Vector2 start, out Vector2 end, out float cellSize))
                yield break;
            yield return GetLightning(poolIndex).Animate(start, end, horizontal,
                cellSize, LineEffectSeconds);
        }

        bool TryGetCrossLineGeometry(BoardPos center, bool horizontal,
            out Vector2 start, out Vector2 end, out float cellSize)
        {
            start = Vector2.zero;
            end = Vector2.zero;
            cellSize = 0f;
            if (grid == null || vfxLayer == null || boardCells == null)
            {
                Debug.LogError("Line-clear VFX requires a GridLayoutGroup, BoardVFXLayer, and stable BoardCellView lookup.", this);
                return false;
            }

            if (center.Row < 0 || center.Row >= boardCells.GetLength(0)
                || center.Col < 0 || center.Col >= boardCells.GetLength(1))
            {
                Debug.LogError($"Line-cross VFX center ({center.Row},{center.Col}) is outside the board.", this);
                return false;
            }

            if (horizontal)
            {
                int firstColumn = Mathf.Max(0, center.Col - 2);
                int lastColumn = Mathf.Min(boardCells.GetLength(1) - 1, center.Col + 2);
                start = CellCenter(new BoardPos(center.Row, firstColumn));
                end = CellCenter(new BoardPos(center.Row, lastColumn));
                cellSize = grid.cellSize.x;
                return true;
            }

            int firstRow = Mathf.Max(0, center.Row - 2);
            int lastRow = Mathf.Min(boardCells.GetLength(0) - 1, center.Row + 2);
            start = CellCenter(new BoardPos(lastRow, center.Col));
            end = CellCenter(new BoardPos(firstRow, center.Col));
            cellSize = grid.cellSize.y;
            return true;
        }

        bool TryGetBoardLineGeometry(bool horizontal, int index,
            out Vector2 start, out Vector2 end, out float cellSize)
        {
            start = Vector2.zero;
            end = Vector2.zero;
            cellSize = 0f;
            if (grid == null || vfxLayer == null || boardCells == null)
            {
                Debug.LogError("Line-clear VFX requires a GridLayoutGroup, BoardVFXLayer, and stable BoardCellView lookup.", this);
                return false;
            }

            if (horizontal)
            {
                if (index < 0 || index >= boardCells.GetLength(0))
                    return false;
                float centerY = CellCenter(new BoardPos(index, 0)).y;
                start = new Vector2(vfxLayer.rect.xMin - vfxLayer.rect.width * .02f, centerY);
                end = new Vector2(vfxLayer.rect.xMax + vfxLayer.rect.width * .02f, centerY);
                cellSize = grid.cellSize.y;
                return true;
            }

            if (index < 0 || index >= boardCells.GetLength(1))
                return false;
            float centerX = CellCenter(new BoardPos(0, index)).x;
            start = new Vector2(centerX, vfxLayer.rect.yMax + vfxLayer.rect.height * .02f);
            end = new Vector2(centerX, vfxLayer.rect.yMin - vfxLayer.rect.height * .02f);
            cellSize = grid.cellSize.x;
            return true;
        }

        LightningLineVFX GetLightning(int poolIndex)
        {
            while (lightningPool.Count <= poolIndex)
            {
                GameObject lightningObject = new GameObject("ProceduralLineLightning",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(LightningLineVFX));
                lightningObject.transform.SetParent(vfxLayer, false);
                lightningObject.transform.SetAsLastSibling();
                LightningLineVFX lightning = lightningObject.GetComponent<LightningLineVFX>();
                lightning.raycastTarget = false;
                lightning.Stop();
                lightning.SetUniqueSeed(unchecked(0x51f15e + poolIndex * 7919));
                lightningPool.Add(lightning);
            }
            return lightningPool[poolIndex];
        }

#if UNITY_EDITOR
        public bool TryGetLineGeometryForDebug(BoardPos center, bool horizontal,
            out Vector2 start, out Vector2 end, out float cellSize) =>
            TryGetCrossLineGeometry(center, horizontal, out start, out end, out cellSize);

        public bool TryGetAreaExplosionGeometryForDebug(BoardPos center,
            out Vector2 centerPoint, out Vector2 cellSize, out Vector2[] cellCenters)
        {
            centerPoint = Vector2.zero;
            cellSize = Vector2.zero;
            cellCenters = null;
            if (!TryGetAreaExplosionGeometry(center, out centerPoint, out cellSize, out cellCenters))
                return false;
            return true;
        }
#endif

        bool TryGetAreaExplosionGeometry(BoardPos position, out Vector2 center,
            out Vector2 cellSize, out Vector2[] validCellCenters)
        {
            center = Vector2.zero;
            cellSize = Vector2.zero;
            validCellCenters = null;
            if (boardCells == null || grid == null
                || position.Row < 0 || position.Row >= boardCells.GetLength(0)
                || position.Col < 0 || position.Col >= boardCells.GetLength(1))
            {
                Debug.LogError($"Cannot position AreaBomb VFX at ({position.Row},{position.Col}).", this);
                return false;
            }

            center = CellCenter(position);
            cellSize = grid.cellSize;
            validCellCenters = new Vector2[9];
            int count = 0;
            for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
            for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
            {
                int row = position.Row + rowOffset;
                int column = position.Col + columnOffset;
                if (row < 0 || row >= boardCells.GetLength(0)
                    || column < 0 || column >= boardCells.GetLength(1)
                    || boardCells[row, column] == null)
                    continue;
                validCellCenters[count++] = CellCenter(new BoardPos(row, column));
            }
            Array.Resize(ref validCellCenters, count);
            return true;
        }

        IEnumerator ShowAreaBomb(BoardPos position, GemType gemType)
        {
            if (!TryGetAreaExplosionGeometry(position, out Vector2 center,
                out Vector2 cellSize, out _))
                yield break;

            BoardVFXCatalog.EffectEntry effectEntry = GetBomb3x3Entry();
            BoardVFXCatalog.GemStyleEntry styleEntry = GetBombStyleEntry(gemType, effectEntry.color);

            float size = Mathf.Max(cellSize.x, cellSize.y) * Mathf.Max(1f, effectEntry.scale);
            List<BoardBurstVFX.Burst> bursts = new List<BoardBurstVFX.Burst>
            {
                new BoardBurstVFX.Burst(center, size, styleEntry.style, 0, styleEntry.color)
            };
            yield return ShowBurstEffect(bursts, Array.Empty<BoardBurstVFX.Arc>(),
                Mathf.Max(.12f, effectEntry.duration));
        }

        IEnumerator ShowBombFiveByFive(BoardPos position, GemType gemType)
        {
            if (!TryGetAreaExplosionGeometry(position, out _, out Vector2 cellSize, out _))
                yield break;

            BoardVFXCatalog.EffectEntry effectEntry = GetBomb3x3Entry();
            BoardVFXCatalog.GemStyleEntry styleEntry =
                GetBombStyleEntry(gemType, effectEntry.color);
            float baseSize = Mathf.Max(cellSize.x, cellSize.y);
            List<BoardBurstVFX.Burst> bursts = new List<BoardBurstVFX.Burst>();
            for (int row = position.Row - 2; row <= position.Row + 2; row++)
            for (int column = position.Col - 2; column <= position.Col + 2; column++)
            {
                if (row < 0 || row >= boardCells.GetLength(0)
                    || column < 0 || column >= boardCells.GetLength(1)
                    || boardCells[row, column] == null)
                    continue;

                bool center = row == position.Row && column == position.Col;
                bursts.Add(new BoardBurstVFX.Burst(
                    CellCenter(new BoardPos(row, column)),
                    baseSize * (center ? 1.35f : .82f),
                    styleEntry.style, row * boardCells.GetLength(1) + column,
                    styleEntry.color));
            }

            yield return ShowBurstEffect(bursts, Array.Empty<BoardBurstVFX.Arc>(),
                Mathf.Max(.12f, effectEntry.duration));
        }

        float GetBombExplosionDuration() => Mathf.Max(.12f, GetBomb3x3Entry().duration);

        BoardVFXCatalog.EffectEntry GetBomb3x3Entry()
        {
            if (boardVfxCatalog != null
                && boardVfxCatalog.TryGetEffect(BoardVFXEffect.Bomb3x3, out var entry)
                && entry.mode == BoardVFXMode.CodeDrawn)
                return entry;

            if (warnedMissingCatalogEntries.Add(BoardVFXEffect.Bomb3x3))
                Debug.LogError("Board VFX catalog has no valid CodeDrawn Bomb3x3 entry; using procedural fire defaults.", this);
            return new BoardVFXCatalog.EffectEntry
            {
                effect = BoardVFXEffect.Bomb3x3,
                mode = BoardVFXMode.CodeDrawn,
                style = BoardBurstStyle.Fire,
                color = Color.white,
                scale = 3f,
                duration = ExplosionSeconds
            };
        }

        BoardVFXCatalog.GemStyleEntry GetBombStyleEntry(GemType gemType, Color fallbackColor)
        {
            if (boardVfxCatalog != null
                && boardVfxCatalog.TryGetBombStyle(gemType, out BoardVFXCatalog.GemStyleEntry entry))
                return entry;

            WarnUnknownBombType(gemType);
            return new BoardVFXCatalog.GemStyleEntry
            {
                gemType = gemType,
                style = BoardBurstStyle.Fire,
                color = fallbackColor
            };
        }

#if UNITY_EDITOR
        public BoardVFXCatalog.GemStyleEntry GetBombStyleForDebug(GemType gemType)
        {
            BoardVFXCatalog.EffectEntry effectEntry = GetBomb3x3Entry();
            return GetBombStyleEntry(gemType, effectEntry.color);
        }
#endif

        void WarnUnknownBombType(GemType gemType)
        {
            if (warnedUnknownBombTypes.Add(gemType))
                Debug.LogWarning($"Bomb has no catalog style/color for GemType {gemType}; using Fire fallback.", this);
        }

        Vector2 CellCenter(BoardPos position)
        {
            if (boardCells == null
                || position.Row < 0 || position.Row >= boardCells.GetLength(0)
                || position.Col < 0 || position.Col >= boardCells.GetLength(1)
                || boardCells[position.Row, position.Col] == null)
            {
                Debug.LogError($"Cannot position special-gem VFX without a stable cell for ({position.Row},{position.Col}).", this);
                return Vector2.zero;
            }

            RectTransform cell = boardCells[position.Row, position.Col].RectTransform;
            return cell != null
                ? vfxLayer.InverseTransformPoint(cell.TransformPoint(cell.rect.center))
                : Vector2.zero;
        }

    }
}
