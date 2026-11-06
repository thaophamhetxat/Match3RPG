using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Match3RPG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Board
{
    public sealed class BoardManager : MonoBehaviour
    {
        public const int DefaultBoardSize = 7;

        [SerializeField, Min(3)] int rows = DefaultBoardSize;
        [SerializeField, Min(3)] int columns = DefaultBoardSize;
        [SerializeField] Transform boardRoot;
        [SerializeField] GemView gemPrefab;
        [SerializeField] Sprite[] gemSprites = new Sprite[10];
        [SerializeField] float stepDelay = .12f;
        [SerializeField, Min(0f)] float gemFallSecondsPerCell = .07f;
        [SerializeField, Min(0f)] float gemFallMinDuration = .20f;
        [SerializeField, Min(0f)] float gemFallMaxDuration = .50f;
        [SerializeField, Min(0f)] float gemSettleDelay = .08f;
        [SerializeField, Min(1)] int maximumCascadeSteps = 50;

        GemView[,] grid;
        BoardCellView[,] boardCells;
        Vector3[,] fallbackCellPositions;
        GemView selected;
        bool resolving;
        bool inputEnabled;
        bool loggedFirstRefillGem;
        bool soulGemEnabled;
#if UNITY_EDITOR
        DevelopmentBoardSnapshot lastRefillSnapshot;
        readonly List<GravityMovementDebugRecord> lastGravityMovementRecords =
            new List<GravityMovementDebugRecord>();
        bool loggedGem19GravityTrace;
#endif
        readonly SpecialGemResolver specialResolver = new SpecialGemResolver();
        readonly BoardShuffleService baguaShuffleService = new BoardShuffleService(100, .25f);
        static readonly HashSet<GemType> LoggedMissingGemSprites = new HashSet<GemType>();
        readonly HashSet<string> loggedGemVisualMismatches = new HashSet<string>();
        static readonly GemType[] SpawnableGemTypes =
        {
            GemType.Attack, GemType.Mana, GemType.ExpDrain,
            GemType.Rage, GemType.Health, GemType.Shield
        };
        static readonly GemType[] SoulSpawnableGemTypes =
        {
            GemType.Attack, GemType.Mana, GemType.ExpDrain,
            GemType.Rage, GemType.Health, GemType.Shield, GemType.Soul
        };
        GemType[] ActiveSpawnableGemTypes =>
            soulGemEnabled ? SoulSpawnableGemTypes : SpawnableGemTypes;
        BoardMoveScanner moveScanner;
        GemSpecialVFXManager specialVfx;
        GemVisualDatabase visualDatabase;
        BoardSelectionOverlay selectionOverlay;
        public event Action<MoveResult> PlayerMoveRequested;
        public int Rows => rows;
        public int Columns => columns;
        public bool IsResolving => resolving;
        public bool CanAcceptPlayerInput => inputEnabled && !resolving;
        public bool IsBoardReady => grid != null;
        public bool SoulGemEnabled => soulGemEnabled;
        public Transform BoardRoot => boardRoot;
        public BoardMoveScanner MoveScanner => moveScanner ?? (moveScanner = new BoardMoveScanner(specialResolver));

#if UNITY_EDITOR
        public IReadOnlyList<GravityMovementDebugRecord> LastGravityMovementRecords =>
            lastGravityMovementRecords;

        public readonly struct GravityMovementDebugRecord
        {
            public readonly long RuntimeId;
            public readonly BoardPos Source;
            public readonly BoardPos Destination;
            public readonly Vector3 SourcePosition;
            public readonly Vector3 DestinationPosition;

            public GravityMovementDebugRecord(long runtimeId, BoardPos source, BoardPos destination,
                Vector3 sourcePosition, Vector3 destinationPosition)
            {
                RuntimeId = runtimeId;
                Source = source;
                Destination = destination;
                SourcePosition = sourcePosition;
                DestinationPosition = destinationPosition;
            }
        }
#endif

        public void Configure(Transform root, GemView prefab, Sprite[] sprites,
            int boardRows = DefaultBoardSize, int boardColumns = DefaultBoardSize,
            GemSpecialVFXManager vfxManager = null,
            GemVisualDatabase gemVisualDatabase = null,
            BoardCellView[,] stableBoardCells = null,
            BoardSelectionOverlay boardSelectionOverlay = null,
            bool enableSoulGem = false)
        {
            if (grid != null && soulGemEnabled != enableSoulGem)
            {
                Debug.LogError("Soul gem availability cannot change after the board is initialized.", this);
                return;
            }
            boardRoot = root;
            gemPrefab = prefab;
            gemSprites = sprites;
            rows = boardRows;
            columns = boardColumns;
            specialVfx = vfxManager;
            visualDatabase = gemVisualDatabase;
            selectionOverlay = boardSelectionOverlay;
            soulGemEnabled = enableSoulGem;
            ConfigureStableCells(stableBoardCells);
            ValidateGemSprites();
        }

        void ConfigureStableCells(BoardCellView[,] stableBoardCells)
        {
            if (stableBoardCells == null)
            {
                boardCells = null;
                return;
            }

            if (stableBoardCells.GetLength(0) != rows || stableBoardCells.GetLength(1) != columns)
            {
                Debug.LogError("Stable board-cell lookup dimensions must match the configured board.", this);
                return;
            }

            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                BoardCellView cell = stableBoardCells[row, column];
                if (cell == null || !cell.Position.Equals(new BoardPos(row, column)))
                {
                    Debug.LogError($"Stable board-cell lookup is missing ({row},{column}) or has an invalid BoardPos.", this);
                    return;
                }
            }

            boardCells = stableBoardCells;
            fallbackCellPositions = null;
        }

        void Start()
        {
            EnsureBoardReady();
        }

        public void EnsureBoardReady()
        {
            if (grid == null && boardRoot != null && gemPrefab != null && gemSprites != null
                && gemSprites.Length > (int)GemType.Bagua)
                BuildBoard();
        }

        void BuildBoard()
        {
            if (boardRoot == null || gemPrefab == null || gemSprites == null
                || gemSprites.Length <= (int)GemType.Bagua)
            {
                Debug.LogError("BoardManager requires a board root, gem prefab, and all ten gem sprites.", this);
                return;
            }

            grid = new GemView[rows, columns];
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
            {
                GemType type;
                do type = RandomType(); while (CreatesInitialMatch(r, c, type));
                var view = Instantiate(gemPrefab, boardRoot);
                SetCell(view, new BoardPos(r, c), type);
            }
            CacheFallbackCellPositions();
            if (!HasAnyLegalMove())
            {
                Debug.LogWarning("Initial board has no legal moves; generating a playable fallback layout.", this);
                RegenerateGuaranteedMoveLayout();
            }
            ValidateGemVisualsAfterBoardChange();
            RefreshBombRadiusGlows();
            SetInput(inputEnabled);
        }

        void CacheFallbackCellPositions()
        {
            if (boardCells != null || fallbackCellPositions != null || grid == null)
                return;

            GridLayoutGroup layout = boardRoot.GetComponent<GridLayoutGroup>();
            if (layout != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)boardRoot);
            }

            fallbackCellPositions = new Vector3[rows, columns];
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                RectTransform gemRect = (RectTransform)grid[row, column].transform;
                Vector3 worldCenter = gemRect.TransformPoint(gemRect.rect.center);
                fallbackCellPositions[row, column] = boardRoot.InverseTransformPoint(worldCenter);
            }
        }

        Vector3 GetCellBoardPosition(BoardPos position)
        {
            if (boardCells != null)
            {
                RectTransform cell = boardCells[position.Row, position.Col].RectTransform;
                return boardRoot.InverseTransformPoint(cell.TransformPoint(cell.rect.center));
            }

            if (fallbackCellPositions == null)
                CacheFallbackCellPositions();
            return fallbackCellPositions[position.Row, position.Col];
        }

        Vector3 GetCellWorldPosition(BoardPos position) =>
            boardRoot.TransformPoint(GetCellBoardPosition(position));

        GemType RandomType()
        {
            GemType[] spawnableTypes = ActiveSpawnableGemTypes;
            return spawnableTypes[UnityEngine.Random.Range(0, spawnableTypes.Length)];
        }
        Sprite SpriteFor(GemType type)
        {
            if (visualDatabase != null)
                return visualDatabase.GetSprite(type, GemSpecialType.None);

            int index;
            switch (type)
            {
                case GemType.Attack: index = 0; break;
                case GemType.Mana: index = 1; break;
                case GemType.ExpDrain: index = 2; break;
                case GemType.Rage: index = 3; break;
                case GemType.Health: index = 4; break;
                case GemType.Shield: index = 5; break;
                case GemType.Soul: index = 6; break;
                case GemType.Rainbow: index = 7; break;
                case GemType.Lightning: index = 8; break;
                case GemType.Bagua: index = 9; break;
                default: return null;
            }
            return gemSprites != null && index < gemSprites.Length ? gemSprites[index] : null;
        }

        void ValidateGemSprites()
        {
            foreach (GemType type in Enum.GetValues(typeof(GemType)))
            {
                if (SpriteFor(type) != null || !LoggedMissingGemSprites.Add(type))
                    continue;
                Debug.LogWarning($"Gem sprite is missing for {type}; expected "
                    + $"{GemVisualDatabase.ExpectedBaseMapping(type)}.", this);
            }
        }

        bool CreatesInitialMatch(int r, int c, GemType t)
        {
            bool horizontal = c >= 2 && grid[r,c-1].Type == t && grid[r,c-2].Type == t;
            bool vertical = r >= 2 && grid[r-1,c].Type == t && grid[r-2,c].Type == t;
            return horizontal || vertical;
        }

        void SetCell(GemView gem, BoardPos pos, GemType type)
        {
            GemView previous = grid[pos.Row, pos.Col];
            if (previous != null && previous != gem)
            {
                if (selected == previous) SetSelected(null);
                previous.SetFocused(false);
            }
            grid[pos.Row, pos.Col] = gem;
            gem.Initialize(this, pos, type, GemSpecialType.None, SpriteFor(type),
                visualDatabase);
            gem.transform.SetSiblingIndex(pos.Row * columns + pos.Col);
            gem.SetInteractable(inputEnabled && !resolving);
            gem.gameObject.SetActive(true);
        }

        public void Select(GemView gem)
        {
            if (resolving || !inputEnabled) return;
            if (gem == null) return;
            if (selected == null)
            {
                if (CanActivateByClick(gem.SpecialType))
                {
                    MoveResult activation = specialResolver.EvaluateDirectActivation(CreateSnapshot(), gem.Position);
                    if (activation.IsValid) PlayerMoveRequested?.Invoke(activation);
                    return;
                }
                SetSelected(gem);
                return;
            }
            if (selected == gem)
            {
                if (CanActivateByClick(gem.SpecialType))
                {
                    MoveResult activation = specialResolver.EvaluateDirectActivation(CreateSnapshot(), gem.Position);
                    SetSelected(null);
                    if (activation.IsValid) PlayerMoveRequested?.Invoke(activation);
                }
                else SetSelected(null);
                return;
            }
            if (!selected.Position.IsAdjacent(gem.Position)) { SetSelected(gem); return; }
            MoveResult move = EvaluateMove(selected.Position, gem.Position);
            SetSelected(null);
            PlayerMoveRequested?.Invoke(move);
        }

        static bool CanActivateByClick(GemSpecialType special)
        {
            return special == GemSpecialType.AreaBomb
                || special == GemSpecialType.Lightning
                || special == GemSpecialType.Bagua;
        }

        void SetSelected(GemView gem)
        {
            if (selected != null) selected.SetFocused(false);
            selected = gem;
            if (selected != null)
            {
                selected.SetFocused(true);
                if (boardCells != null)
                    selectionOverlay?.Focus(selected,
                        boardCells[selected.Position.Row, selected.Position.Col].RectTransform,
                        rows, columns);
                else
                    selectionOverlay?.Hide();
            }
            else
                selectionOverlay?.Hide();
        }

        public void ClearSelection() => SetSelected(null);

        public void ClearSelectionForDestroyedGem(GemView gem)
        {
            if (!ReferenceEquals(selected, gem))
                return;
            selected = null;
            selectionOverlay?.Hide();
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            SetInput(enabled && !resolving);
            if (!enabled) SetSelected(null);
        }

        public GemType GetTypeAt(BoardPos position) => grid[position.Row, position.Col].Type;
        public GemSpecialType GetSpecialAt(BoardPos position) => grid[position.Row, position.Col].SpecialType;

        public BoardSnapshot CreateSnapshot()
        {
            GemType[,] types = new GemType[rows, columns];
            GemSpecialType[,] specials = new GemSpecialType[rows, columns];
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                GemView gem = grid[row, column];
                types[row, column] = gem.Type;
                specials[row, column] = gem.SpecialType;
            }
            return new BoardSnapshot(types, specials);
        }

        public MoveResult EvaluateMove(BoardPos from, BoardPos to)
        {
            return specialResolver.EvaluateSwap(CreateSnapshot(), from, to);
        }

        public List<MoveResult> GetLegalMoves()
        {
            return MoveScanner.GetLegalMoves(CreateSnapshot());
        }

        public bool HasAnyLegalMove()
        {
            return MoveScanner.HasAnyLegalMove(CreateSnapshot());
        }

        public IEnumerator ResolveMove(MoveResult requestedMove, Action<MoveResult> completed)
        {
            if (resolving || requestedMove == null)
            {
                completed?.Invoke(null);
                yield break;
            }

            resolving = true;
            SetInput(false);
            MoveResult result = new MoveResult(requestedMove.From, requestedMove.To)
            {
                IsValid = true,
                IsDirectActivation = requestedMove.IsDirectActivation,
                IsBaguaShuffle = requestedMove.IsBaguaShuffle,
                WasSpecialActivation = requestedMove.WasSpecialActivation,
                WasInitialMove = requestedMove.WasInitialMove
            };

            if (!result.IsDirectActivation)
            {
                if (!result.From.IsAdjacent(result.To))
                {
                    FinishResolution();
                    completed?.Invoke(null);
                    yield break;
                }
                yield return AnimateSwap(result.From, result.To);
            }

            if (!requestedMove.IsValid)
            {
                result.IsValid = false;
                if (!result.IsDirectActivation) yield return AnimateSwap(result.From, result.To);
                FinishResolution();
                completed?.Invoke(result);
                yield break;
            }

            if (result.IsBaguaShuffle)
            {
                BoardPos consumedPosition = result.From;
                GemView consumedGem = grid[consumedPosition.Row, consumedPosition.Col];
                if (consumedGem == null || consumedGem.SpecialType != GemSpecialType.Bagua)
                {
                    Debug.LogError("Bagua activation no longer points to a Bagua Gem.", this);
                    FinishResolution();
                    completed?.Invoke(null);
                    yield break;
                }

                GridLayoutGroup layout = boardRoot.GetComponent<GridLayoutGroup>();
                bool wasLayoutEnabled = layout != null && layout.enabled;
                if (wasLayoutEnabled)
                    layout.enabled = false;
                grid[consumedPosition.Row, consumedPosition.Col] = null;
                DestroyMatchedGem(consumedGem);
                RefreshBombRadiusGlows();
                yield return CollapseAndRefill(new HashSet<int> { consumedPosition.Col },
                    wasLayoutEnabled, Array.Empty<GemSpecialType>());
                if (gemSettleDelay > 0f)
                    yield return new WaitForSecondsRealtime(gemSettleDelay);

                bool shuffleSucceeded = false;
                specialVfx?.PlayBaguaShuffle(consumedPosition);
                yield return baguaShuffleService.Shuffle(this,
                    succeeded => shuffleSucceeded = succeeded);
                if (!shuffleSucceeded)
                {
                    Debug.LogError("Bagua could not find a match-free playable permutation without changing gems.", this);
                    FinishResolution();
                    completed?.Invoke(null);
                    yield break;
                }
                RefreshBombRadiusGlows();

                result.ExtraTurnsGranted = 1;
                result.GrantsExtraTurn = true;
                FinishResolution();
                completed?.Invoke(result);
                yield break;
            }

            bool rainbowBaguaCombo = requestedMove.InitialMatch != null
                && requestedMove.InitialMatch.SpecialActivations.Any(activation =>
                    activation.Kind == SpecialActivationKind.RainbowBaguaCombo);
            MatchResult match;
            if (rainbowBaguaCombo)
            {
                GemView firstSwappedGem = grid[result.From.Row, result.From.Col];
                GemView secondSwappedGem = grid[result.To.Row, result.To.Col];
                if (firstSwappedGem == null || secondSwappedGem == null
                    || (firstSwappedGem.SpecialType != GemSpecialType.Rainbow
                        && secondSwappedGem.SpecialType != GemSpecialType.Rainbow)
                    || (firstSwappedGem.SpecialType != GemSpecialType.Bagua
                        && secondSwappedGem.SpecialType != GemSpecialType.Bagua))
                {
                    Debug.LogError("Rainbow+Bagua swap no longer points to its two special Gems.", this);
                    FinishResolution();
                    completed?.Invoke(null);
                    yield break;
                }
                GemView rainbowGem = firstSwappedGem.SpecialType == GemSpecialType.Rainbow
                    ? firstSwappedGem : secondSwappedGem;
                GemView baguaGem = firstSwappedGem.SpecialType == GemSpecialType.Bagua
                    ? firstSwappedGem : secondSwappedGem;

                bool shuffleSucceeded = false;
                yield return baguaShuffleService.Shuffle(this,
                    succeeded => shuffleSucceeded = succeeded, allowRegeneration: false);
                if (!shuffleSucceeded)
                {
                    Debug.LogError("Rainbow+Bagua could not find a match-free playable shuffle without changing the board.", this);
                    yield return AnimateSwap(result.From, result.To);
                    FinishResolution();
                    completed?.Invoke(null);
                    yield break;
                }

                match = specialResolver.ResolveRainbowBaguaCombo(CreateSnapshot(),
                    rainbowGem.Position, baguaGem.Position);
            }
            else
            {
                match = specialResolver.ResolveCurrent(CreateSnapshot(), result,
                    !result.IsDirectActivation, 1, result.IsDirectActivation, rollBonusSpecials: true);
            }
            if (!match.HasMatch)
            {
                if (!result.IsDirectActivation) yield return AnimateSwap(result.From, result.To);
                FinishResolution();
                completed?.Invoke(null);
                yield break;
            }

            int combo = 1;
            while (match.HasMatch && combo <= maximumCascadeSteps)
            {
                match.ComboIndex = combo;
                result.AddMatch(match);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                int matchThreeCount = match.Segments.Count(segment => segment.Length == 3);
                Debug.Log($"Wave {match.ComboIndex}: Match3={matchThreeCount} "
                    + $"Line={match.CreatedLineCount} "
                    + $"AreaBomb={match.CreatedAreaBombCount} Rainbow={match.CreatedRainbowCount} "
                    + $"ExtraTurns={match.ExtraTurnsGranted}", this);
#endif
                foreach (SpecialGemCreation creation in match.CreatedSpecials)
                {
                    GemView created = grid[creation.Position.Row, creation.Position.Col];
                    created.SetState(creation.GemType, creation.SpecialType,
                        SpriteFor(creation.GemType), playCreationFlash: true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    string spriteName = created.SpriteImage != null && created.SpriteImage.sprite != null
                        ? created.SpriteImage.sprite.name
                        : "null";
                    Debug.Log($"Created special: Id={created.RuntimeId} "
                        + $"Pos=({creation.Position.Row},{creation.Position.Col}) "
                        + $"Type={created.Type} Special={created.SpecialType} Sprite={spriteName}", created);
#endif
                }
                RefreshBombRadiusGlows();
                ValidateGemVisualsAfterBoardChange();

                HashSet<BoardPos> affectedPositions = new HashSet<BoardPos>(match.ClearPositions);
                HashSet<int> affectedColumns = new HashSet<int>();
                foreach (BoardPos position in affectedPositions)
                    affectedColumns.Add(position.Col);
                List<GemView> clearedGems = new List<GemView>(match.ClearPositions.Count);
                foreach (BoardPos position in match.ClearPositions)
                {
                    GemView gem = grid[position.Row, position.Col];
                    if (gem != null)
                        clearedGems.Add(gem);
                }
#if UNITY_EDITOR
                BoardSnapshot beforeResolution = CreateSnapshot();
                DevelopmentBoardSnapshot beforeResolutionIdentity = CaptureDevelopmentSnapshot();
#endif
                GridLayoutGroup boardLayout = boardRoot.GetComponent<GridLayoutGroup>();
                bool wasLayoutEnabled = boardLayout != null && boardLayout.enabled;
                if (wasLayoutEnabled)
                    boardLayout.enabled = false;
                Action clearMatchedGems = () =>
                {
                    foreach (BoardPos position in affectedPositions)
                    {
                        GemView gem = grid[position.Row, position.Col];
                        if (gem == null) continue;
                        DestroyMatchedGem(gem);
                        grid[position.Row, position.Col] = null;
                    }
                    RefreshBombRadiusGlows();
                };
                RefreshBombRadiusGlows(affectedPositions);
                if (specialVfx != null)
                    yield return specialVfx.PlayActivations(match.SpecialActivations,
                        clearedGems, clearMatchedGems);
                else
                    clearMatchedGems();

#if UNITY_EDITOR
                DevelopmentBoardSnapshot afterDestruction = CaptureDevelopmentSnapshot();
                CompareDevelopmentSnapshots(beforeResolutionIdentity, afterDestruction,
                    affectedColumns, "after destruction");
#endif
                yield return new WaitForSeconds(stepDelay);
                yield return CollapseAndRefill(affectedColumns, wasLayoutEnabled, match.RefillSpecials);
                ValidateGemVisualsAfterBoardChange();
#if UNITY_EDITOR
                CompareBoardSnapshots(beforeResolution, CreateSnapshot(), affectedColumns);
#endif
                int baguaShuffleCount = match.SpecialActivations.Count(activation =>
                    activation.Kind == SpecialActivationKind.FallbackBaguaShuffle);
                for (int shuffleIndex = 0; shuffleIndex < baguaShuffleCount; shuffleIndex++)
                {
                    bool shuffleSucceeded = false;
                    yield return baguaShuffleService.Shuffle(this,
                        succeeded => shuffleSucceeded = succeeded,
                        allowRegeneration: false);
                    if (!shuffleSucceeded)
                    {
                        Debug.LogError("Bagua activation could not find a match-free playable shuffle without changing gems.", this);
                        FinishResolution();
                        completed?.Invoke(null);
                        yield break;
                    }
                }
                if (gemSettleDelay > 0f)
                    yield return new WaitForSecondsRealtime(gemSettleDelay);
                combo++;
                match = specialResolver.ResolveCurrent(CreateSnapshot(), result, false, combo,
                    false, rollBonusSpecials: true);
#if UNITY_EDITOR
                CompareDevelopmentSnapshots(lastRefillSnapshot, CaptureDevelopmentSnapshot(),
                    new HashSet<int>(), "after cascade scan");
#endif
            }

            if (combo > maximumCascadeSteps)
                Debug.LogError("Board resolution stopped at the configured cascade limit; the board was not randomized.", this);
            FinishResolution();
            completed?.Invoke(result);
        }

        static void DestroyMatchedGem(GemView gem)
        {
            gem.StopSpecialVFX();
            if (Application.isPlaying)
            {
                gem.gameObject.SetActive(false);
                Destroy(gem.gameObject);
            }
            else
            {
                DestroyImmediate(gem.gameObject);
            }
        }

        void SwapCells(BoardPos first, BoardPos second)
        {
            GemView firstGem = grid[first.Row, first.Col];
            GemView secondGem = grid[second.Row, second.Col];
            grid[first.Row, first.Col] = secondGem;
            grid[second.Row, second.Col] = firstGem;
            secondGem.SetPosition(first);
            firstGem.SetPosition(second);
            UpdateSiblingOrder();
            RefreshBombRadiusGlows();
        }

        IEnumerator AnimateSwap(BoardPos first, BoardPos second)
        {
            GemView firstGem = grid[first.Row, first.Col];
            GemView secondGem = grid[second.Row, second.Col];
            RectTransform firstRect = (RectTransform)firstGem.transform;
            RectTransform secondRect = (RectTransform)secondGem.transform;
            Vector2 firstStart = firstRect.anchoredPosition;
            Vector2 secondStart = secondRect.anchoredPosition;
            GridLayoutGroup layout = boardRoot.GetComponent<GridLayoutGroup>();
            bool wasEnabled = layout != null && layout.enabled;
            if (layout != null) layout.enabled = false;
            SwapCells(first, second);

            float elapsed = 0f;
            float duration = Mathf.Max(.01f, stepDelay);
            while (elapsed < duration)
            {
                elapsed += Mathf.Max(Time.unscaledDeltaTime, 1f / 60f);
                float progress = Mathf.Clamp01(elapsed / duration);
                firstRect.anchoredPosition = Vector2.Lerp(firstStart, secondStart, progress);
                secondRect.anchoredPosition = Vector2.Lerp(secondStart, firstStart, progress);
                yield return null;
            }

            firstRect.anchoredPosition = secondStart;
            secondRect.anchoredPosition = firstStart;
            if (layout != null)
            {
                layout.enabled = wasEnabled;
                if (wasEnabled) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)boardRoot);
            }
        }

        void FinishResolution()
        {
            resolving = false;
            SetInput(inputEnabled);
            RefreshBombRadiusGlows();
            ValidateGemVisualsAfterBoardChange();
        }

        public List<GemView> GetGemsInLayoutOrder()
        {
            List<GemView> gems = new List<GemView>(rows * columns);
            if (grid == null) return gems;
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++) gems.Add(grid[row, column]);
            return gems;
        }

        void RefreshBombRadiusGlows(ISet<BoardPos> excludedPositions = null)
        {
            if (grid == null || specialVfx == null)
                return;

            List<GemView> gems = new List<GemView>(rows * columns);
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                GemView gem = grid[row, column];
                if (gem != null && (excludedPositions == null
                    || !excludedPositions.Contains(gem.Position)))
                    gems.Add(gem);
            }
            specialVfx.RefreshBombRadiusGlows(gems);
        }

        public Vector2[] GetSlotAnchoredPositions()
        {
            Vector2[] positions = new Vector2[rows * columns];
            List<GemView> gems = GetGemsInLayoutOrder();
            for (int index = 0; index < gems.Count; index++)
                positions[index] = ((RectTransform)gems[index].transform).anchoredPosition;
            return positions;
        }

        public bool IsValidGemOrder(IReadOnlyList<GemView> orderedGems)
        {
            if (grid == null || orderedGems == null || orderedGems.Count != rows * columns) return false;
            GemType[,] types = new GemType[rows, columns];
            GemSpecialType[,] specials = new GemSpecialType[rows, columns];
            for (int index = 0; index < orderedGems.Count; index++)
            {
                int row = index / columns;
                int column = index % columns;
                types[row, column] = orderedGems[index].Type;
                specials[row, column] = orderedGems[index].SpecialType;
            }
            BoardSnapshot snapshot = new BoardSnapshot(types, specials);
            MatchResult matches = specialResolver.ResolveCurrent(snapshot, new MoveResult(default, default), false, 1, false);
            return matches.Segments.Count == 0 && MoveScanner.HasAnyLegalMove(snapshot);
        }

        public void ApplyGemOrder(IReadOnlyList<GemView> orderedGems)
        {
            for (int index = 0; index < orderedGems.Count; index++)
            {
                int row = index / columns;
                int column = index % columns;
                GemView gem = orderedGems[index];
                grid[row, column] = gem;
                gem.SetPosition(new BoardPos(row, column));
                gem.transform.SetSiblingIndex(index);
            }
            RefreshBombRadiusGlows();
        }

        public bool RegenerateFallbackBoard()
        {
            if (grid == null) return false;
            for (int attempt = 0; attempt < 100; attempt++)
            {
                GemType[] spawnableTypes = ActiveSpawnableGemTypes;
                int offset = attempt % spawnableTypes.Length;
                for (int row = 0; row < rows; row++)
                for (int column = 0; column < columns; column++)
                {
                    GemType type = spawnableTypes[(row * 2 + column + offset) % spawnableTypes.Length];
                    grid[row, column].SetType(type, SpriteFor(type));
                }
                if (!HasAnyMatches() && HasAnyLegalMove())
                {
                    ValidateGemVisualsAfterBoardChange();
                    return true;
                }
            }

            Debug.LogWarning("Board shuffle fallback regeneration was required.", this);
            RegenerateGuaranteedMoveLayout();
            bool playable = !HasAnyMatches() && HasAnyLegalMove();
            ValidateGemVisualsAfterBoardChange();
            return playable;
        }

        void RegenerateGuaranteedMoveLayout()
        {
            GemType[] spawnableTypes = ActiveSpawnableGemTypes;
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                GemType type;
                if (row == 0 && column == 0) type = GemType.Attack;
                else if (row == 0 && column == 1) type = GemType.Mana;
                else if (row == 0 && column == 2) type = GemType.Attack;
                else if (row == 1 && column == 1) type = GemType.Attack;
                else
                {
                    type = GemType.Attack;
                    bool found = false;
                    for (int attempt = 0; attempt < spawnableTypes.Length; attempt++)
                    {
                        GemType candidate = spawnableTypes[attempt];
                        if (CreatesInitialMatch(row, column, candidate)) continue;
                        type = candidate;
                        found = true;
                        break;
                    }
                    if (!found) type = spawnableTypes[(row + column) % spawnableTypes.Length];
                }
                grid[row, column].SetType(type, SpriteFor(type));
            }
        }

#if UNITY_EDITOR
        public void SetBoardForDebug(GemType[,] types, GemSpecialType[,] specials, float resolutionDelay = 0f)
        {
            if (grid != null)
            {
                foreach (GemView gem in grid)
                {
                    if (gem == null) continue;
                    gem.StopSpecialVFX();
                    if (Application.isPlaying)
                    {
                        gem.gameObject.SetActive(false);
                        Destroy(gem.gameObject);
                    }
                    else DestroyImmediate(gem.gameObject);
                }
            }
            rows = types.GetLength(0);
            columns = types.GetLength(1);
            stepDelay = resolutionDelay;
            grid = new GemView[rows, columns];
            if (boardCells == null)
                fallbackCellPositions = null;
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                GemView gem = Instantiate(gemPrefab, boardRoot);
                SetCell(gem, new BoardPos(row, column), types[row, column]);
                gem.SetSpecialType(specials[row, column]);
            }
            CacheFallbackCellPositions();
            SetInput(inputEnabled);
            ValidateGemVisualsAfterBoardChange();
        }
#endif

        internal void ValidateGemVisualsAfterBoardChange()
        {
            if (grid == null)
                return;

            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                GemView gem = grid[row, column];
                if (gem == null)
                    continue;

                Sprite expectedSprite;
                if (visualDatabase != null)
                    expectedSprite = visualDatabase.GetSprite(gem.Type, gem.SpecialType);
                else if (gem.SpecialType == GemSpecialType.Line
                    || gem.SpecialType == GemSpecialType.AreaBomb)
                    expectedSprite = null;
                else
                    expectedSprite = SpriteFor(gem.Type);
                Sprite currentSprite = gem.SpriteImage != null ? gem.SpriteImage.sprite : null;
                if (expectedSprite != null && currentSprite == expectedSprite)
                    continue;

                string currentName = currentSprite != null ? currentSprite.name : "<null>";
                string expectedName = expectedSprite != null ? expectedSprite.name : "<missing>";
                string mismatchKey = $"{row},{column}:{gem.Type}:{gem.SpecialType}:{currentName}:{expectedName}";
                if (!loggedGemVisualMismatches.Add(mismatchKey))
                    continue;

                Debug.LogWarning($"Gem visual mismatch at ({row},{column}): "
                    + $"Type={gem.Type}, Special={gem.SpecialType}, "
                    + $"CurrentSprite={currentName}, ExpectedSprite={expectedName} "
                    + $"({GemVisualDatabase.ExpectedVisualMapping(gem.Type, gem.SpecialType)}).",
                    gem);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Debug/Force Match-4 Board")]
        void ForceMatchFourDebugBoard() => ForceLineMatchDebugBoard(4);

        [ContextMenu("Debug/Force Match-5 Board")]
        void ForceMatchFiveDebugBoard() => ForceLineMatchDebugBoard(5);

        void ForceLineMatchDebugBoard(int length)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Force a Match-4 or Match-5 board while the game is in Play Mode.", this);
                return;
            }

            GemType[,] types = new GemType[rows, columns];
            GemSpecialType[,] specials = new GemSpecialType[rows, columns];
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
                types[row, column] = SpawnableGemTypes[(row * 2 + column) % SpawnableGemTypes.Length];

            types[0, 0] = GemType.Attack;
            types[0, 1] = GemType.Attack;
            types[0, 2] = GemType.Mana;
            types[0, 3] = GemType.Attack;
            types[0, 4] = length == 5 ? GemType.Attack : GemType.ExpDrain;
            types[1, 2] = GemType.Attack;
            SetBoardForDebug(types, specials);
            SetInput(inputEnabled);
        }
#endif

        bool HasAnyMatches()
        {
            MatchResult result = specialResolver.ResolveCurrent(CreateSnapshot(), new MoveResult(default, default), false, 1, false);
            return result.Segments.Count > 0;
        }

        IEnumerator CollapseAndRefill(HashSet<int> affectedColumns, bool restoreLayout,
            IReadOnlyList<GemSpecialType> refillSpecials)
        {
            GridLayoutGroup layout = boardRoot.GetComponent<GridLayoutGroup>();
            HashSet<GemView> newGems = new HashSet<GemView>();
            Dictionary<int, int> emptyTopRows = new Dictionary<int, int>();
            List<GravityMove> gravityMoves = new List<GravityMove>();
#if UNITY_EDITOR
            lastGravityMovementRecords.Clear();
#endif

#if UNITY_EDITOR
            DevelopmentBoardSnapshot beforeGravity = CaptureDevelopmentSnapshot();
#endif
            for (int column = 0; column < columns; column++)
            {
                if (!affectedColumns.Contains(column)) continue;
                bool hasEmptyCell = false;
                for (int row = 0; row < rows; row++)
                    hasEmptyCell |= grid[row, column] == null;
                if (!hasEmptyCell) continue;

                int write = rows - 1;
                for (int row = rows - 1; row >= 0; row--)
                {
                    GemView gem = grid[row, column];
                    if (gem == null) continue;

                    BoardPos sourcePosition = gem.Position;
                    BoardPos destinationPosition = new BoardPos(write, column);
                    Debug.Assert(destinationPosition.Col == sourcePosition.Col,
                        $"Illegal cross-column gravity: Gem {gem.RuntimeId} moved from column {sourcePosition.Col} to column {destinationPosition.Col}");
                    grid[row, column] = null;
                    grid[write, column] = gem;
                    gem.SetPosition(destinationPosition);
                    if (sourcePosition.Row != destinationPosition.Row)
                        gravityMoves.Add(new GravityMove(gem, sourcePosition, destinationPosition));
                    write--;
                }
                emptyTopRows[column] = write;
            }

#if UNITY_EDITOR
            DevelopmentBoardSnapshot afterGravity = CaptureDevelopmentSnapshot();
            CompareDevelopmentSnapshots(beforeGravity, afterGravity,
                affectedColumns, "after gravity");
#endif

            for (int column = 0; column < columns; column++)
            {
                if (!emptyTopRows.TryGetValue(column, out int write)) continue;
                while (write >= 0)
                {
                    GemView gem = Instantiate(gemPrefab, boardRoot);
                    SetCell(gem, new BoardPos(write, column), RandomType());
                    newGems.Add(gem);
                    write--;
                }
            }
            if (refillSpecials != null && refillSpecials.Count > 0)
            {
                List<GemView> availableGems = new List<GemView>(newGems);
                int specialsToSpawn = Mathf.Min(refillSpecials.Count, availableGems.Count);
                if (specialsToSpawn < refillSpecials.Count)
                    Debug.LogError($"Only {availableGems.Count} refill gems were available for "
                        + $"{refillSpecials.Count} rolled bonus specials.", this);
                for (int index = 0; index < specialsToSpawn; index++)
                {
                    int gemIndex = UnityEngine.Random.Range(0, availableGems.Count);
                    GemView gem = availableGems[gemIndex];
                    availableGems.RemoveAt(gemIndex);
                    GemSpecialType special = refillSpecials[index];
                    GemType type = special == GemSpecialType.Lightning
                        ? GemType.Lightning
                        : GemType.Bagua;
                    gem.SetState(type, special, SpriteFor(type), playCreationFlash: true);
                }
            }
            UpdateSiblingOrder();
            RefreshBombRadiusGlows();
#if UNITY_EDITOR
            lastRefillSnapshot = CaptureDevelopmentSnapshot();
            CompareDevelopmentSnapshots(afterGravity, lastRefillSnapshot,
                affectedColumns, "after refill");
#endif

            if (layout == null || !restoreLayout)
                yield break;

            try
            {
                layout.enabled = true;
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)boardRoot);
                Dictionary<GemView, Vector2> startingOffsets = new Dictionary<GemView, Vector2>();
                Dictionary<GemView, float> durations = new Dictionary<GemView, float>();
                List<GemView> movingGems = new List<GemView>();
                Dictionary<int, int> spawnCountsByColumn = new Dictionary<int, int>();
                Dictionary<int, int> totalSpawnCountsByColumn = new Dictionary<int, int>();
                float cellPitch = layout.cellSize.y + layout.spacing.y;
                if (cellPitch <= 0f)
                {
                    Debug.LogError("Board GridLayoutGroup must have a positive vertical cell pitch to animate gem falls.", this);
                    yield break;
                }

                foreach (GemView gem in newGems)
                {
                    totalSpawnCountsByColumn.TryGetValue(gem.Position.Col, out int count);
                    totalSpawnCountsByColumn[gem.Position.Col] = count + 1;
                }

                Dictionary<GemView, GravityMove> moveByGem = new Dictionary<GemView, GravityMove>();
                foreach (GravityMove move in gravityMoves)
                    moveByGem.Add(move.Gem, move);

                for (int row = 0; row < rows; row++)
                for (int column = 0; column < columns; column++)
                {
                    GemView gem = grid[row, column];
                    RectTransform gemRect = (RectTransform)gem.transform;
                    Vector3 destinationPosition = GetCellBoardPosition(gem.Position);
                    Vector3 startPosition = destinationPosition;
                    Vector3 startWorldPosition;
                    bool isNewGem = newGems.Contains(gem);

                    if (isNewGem)
                    {
                        Vector3 topCellPosition = GetCellBoardPosition(new BoardPos(0, column));
                        spawnCountsByColumn.TryGetValue(column, out int spawnIndex);
                        startPosition = topCellPosition + Vector3.up * cellPitch
                            * (totalSpawnCountsByColumn[column] - spawnIndex);
                        spawnCountsByColumn[column] = spawnIndex + 1;
                        startWorldPosition = boardRoot.TransformPoint(startPosition);
                    }
                    else if (moveByGem.TryGetValue(gem, out GravityMove move))
                    {
                        startPosition = GetCellBoardPosition(move.Source);
                        startWorldPosition = GetCellWorldPosition(move.Source);
                    }
                    else
                    {
                        continue;
                    }

                    Debug.Assert(Mathf.Abs(startPosition.x - destinationPosition.x) <= .01f,
                        $"Illegal horizontal gravity: Gem {gem.RuntimeId} started at x={startPosition.x} and destination x={destinationPosition.x}");

                    Vector3 destinationWorldPosition = GetCellWorldPosition(gem.Position);
                    Vector3 startOffset3 = gemRect.InverseTransformPoint(startWorldPosition)
                        - gemRect.InverseTransformPoint(destinationWorldPosition);
                    Vector2 startOffset = new Vector2(startOffset3.x, startOffset3.y);
#if UNITY_EDITOR
                    if (!isNewGem)
                    {
                        GravityMove movementRecord = moveByGem[gem];
                        lastGravityMovementRecords.Add(new GravityMovementDebugRecord(
                            gem.RuntimeId, movementRecord.Source, movementRecord.Destination,
                            startPosition, destinationPosition));
                        if (gem.RuntimeId == 19 && !loggedGem19GravityTrace)
                        {
                            RectTransform sourceCell = boardCells != null
                                ? boardCells[movementRecord.Source.Row, movementRecord.Source.Col].RectTransform
                                : null;
                            RectTransform destinationCell = boardCells != null
                                ? boardCells[movementRecord.Destination.Row, movementRecord.Destination.Col].RectTransform
                                : null;
                            Debug.Log(
                                $"Gravity trace Gem 19 | Source={movementRecord.Source.Row},{movementRecord.Source.Col} "
                                + $"Destination={movementRecord.Destination.Row},{movementRecord.Destination.Col} "
                                + $"GemParent={gemRect.parent.name} GemAnchored={gemRect.anchoredPosition} "
                                + $"GemLocal={gemRect.localPosition} "
                                + $"SourceCellParent={(sourceCell != null ? sourceCell.parent.name : "cached board slot")} "
                                + $"SourceCellAnchored={(sourceCell != null ? sourceCell.anchoredPosition.ToString() : "n/a")} "
                                + $"SourceCellLocal={(sourceCell != null ? sourceCell.localPosition.ToString() : startPosition.ToString())} "
                                + $"DestinationCellParent={(destinationCell != null ? destinationCell.parent.name : "cached board slot")} "
                                + $"DestinationCellAnchored={(destinationCell != null ? destinationCell.anchoredPosition.ToString() : "n/a")} "
                                + $"DestinationCellLocal={(destinationCell != null ? destinationCell.localPosition.ToString() : destinationPosition.ToString())} "
                                + $"BoardRootLocalStart={startPosition} BoardRootLocalDestination={destinationPosition}",
                                this);
                            loggedGem19GravityTrace = true;
                        }
                    }
#endif
                    if (startOffset.sqrMagnitude <= 0f)
                        continue;

                    startingOffsets.Add(gem, startOffset);
                    gem.SetVisualOffset(startOffset);
                    float fallDistance = startOffset.magnitude / cellPitch;
                    float fallDuration = GetGemFallDuration(fallDistance, isNewGem);
                    durations.Add(gem, fallDuration);
                    movingGems.Add(gem);
                    if (isNewGem && !loggedFirstRefillGem)
                    {
                        RectTransform viewportRect = boardRoot.parent as RectTransform;
                        Debug.Log(
                            $"First refill Gem | BoardRootWorld={boardRoot.position}, "
                            + $"GemViewportSize={(viewportRect != null ? viewportRect.rect.size : Vector2.zero)}, "
                            + $"Destination=({gem.Position.Row},{gem.Position.Col}), "
                            + $"VisualOffset={startOffset}, DestinationPosition={destinationPosition}, Duration={fallDuration:0.000}s",
                            this);
                        loggedFirstRefillGem = true;
                    }
                }

                float duration = durations.Count > 0 ? durations.Values.Max() : 0f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Mathf.Max(Time.unscaledDeltaTime, 1f / 60f);
                    foreach (GemView gem in movingGems)
                    {
                        float progress = Mathf.Clamp01(elapsed / durations[gem]);
                        float easedProgress = progress * progress;
                        gem.SetVisualOffset(Vector2.LerpUnclamped(
                            startingOffsets[gem], Vector2.zero, easedProgress));
                    }
                    yield return null;
                }

                foreach (GemView gem in movingGems)
                    gem.SetVisualOffset(Vector2.zero);
            }
            finally
            {
                layout.enabled = restoreLayout;
                if (restoreLayout)
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)boardRoot);
            }
        }

        sealed class GravityMove
        {
            public readonly GemView Gem;
            public readonly BoardPos Source;
            public readonly BoardPos Destination;

            public GravityMove(GemView gem, BoardPos source, BoardPos destination)
            {
                Gem = gem;
                Source = source;
                Destination = destination;
            }
        }

#if UNITY_EDITOR
        sealed class DevelopmentBoardSnapshot
        {
            public readonly long[,] RuntimeIds;
            public readonly GemType[,] Types;
            public readonly GemSpecialType[,] Specials;

            public DevelopmentBoardSnapshot(int snapshotRows, int snapshotColumns)
            {
                RuntimeIds = new long[snapshotRows, snapshotColumns];
                Types = new GemType[snapshotRows, snapshotColumns];
                Specials = new GemSpecialType[snapshotRows, snapshotColumns];
            }
        }

        DevelopmentBoardSnapshot CaptureDevelopmentSnapshot()
        {
            DevelopmentBoardSnapshot snapshot = new DevelopmentBoardSnapshot(rows, columns);
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                GemView gem = grid[row, column];
                if (gem == null) continue;
                snapshot.RuntimeIds[row, column] = gem.RuntimeId;
                snapshot.Types[row, column] = gem.Type;
                snapshot.Specials[row, column] = gem.SpecialType;
            }
            return snapshot;
        }

        void CompareDevelopmentSnapshots(DevelopmentBoardSnapshot before,
            DevelopmentBoardSnapshot after, HashSet<int> affectedColumns, string phase)
        {
            List<string> unexpected = new List<string>();
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                if (affectedColumns.Contains(column)) continue;
                if (before.RuntimeIds[row, column] == after.RuntimeIds[row, column]
                    && before.Types[row, column] == after.Types[row, column]
                    && before.Specials[row, column] == after.Specials[row, column])
                    continue;
                unexpected.Add($"({row},{column}) id {before.RuntimeIds[row, column]}"
                    + $"->{after.RuntimeIds[row, column]} "
                    + $"{ShortGemId(before.Types[row, column], before.Specials[row, column])}"
                    + $"->{ShortGemId(after.Types[row, column], after.Specials[row, column])}");
            }

            if (unexpected.Count > 0)
                Debug.LogError($"Board identity changed outside affected columns {phase}: "
                    + string.Join(", ", unexpected), this);
        }

        void CompareBoardSnapshots(BoardSnapshot before, BoardSnapshot after,
            HashSet<int> affectedColumns)
        {
            List<string> changes = new List<string>();
            List<string> unexpected = new List<string>();
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                BoardPos position = new BoardPos(row, column);
                GemType beforeType = before.TypeAt(position);
                GemSpecialType beforeSpecial = before.SpecialAt(position);
                GemType afterType = after.TypeAt(position);
                GemSpecialType afterSpecial = after.SpecialAt(position);
                if (beforeType == afterType && beforeSpecial == afterSpecial) continue;

                string change = $"({row},{column}) {ShortGemId(beforeType, beforeSpecial)}"
                    + $"->{ShortGemId(afterType, afterSpecial)}";
                changes.Add(change);
                if (!affectedColumns.Contains(column)) unexpected.Add(change);
            }

            if (changes.Count == 0) return;
            string message = "Board resolution snapshot:\nBefore:\n" + FormatBoardSnapshot(before)
                + "\nAfter:\n" + FormatBoardSnapshot(after)
                + "\nChanged cells: " + string.Join(", ", changes);
            if (unexpected.Count > 0)
                Debug.LogError(message + "\nUnexpected unrelated-column changes: "
                    + string.Join(", ", unexpected), this);
            else
                Debug.Log(message, this);
        }

        static string FormatBoardSnapshot(BoardSnapshot snapshot)
        {
            List<string> lines = new List<string>(snapshot.Rows);
            for (int row = 0; row < snapshot.Rows; row++)
            {
                List<string> cells = new List<string>(snapshot.Columns);
                for (int column = 0; column < snapshot.Columns; column++)
                {
                    BoardPos position = new BoardPos(row, column);
                    cells.Add(ShortGemId(snapshot.TypeAt(position), snapshot.SpecialAt(position)));
                }
                lines.Add(string.Join(" ", cells));
            }
            return string.Join("\n", lines);
        }

        static string ShortGemId(GemType type, GemSpecialType special)
        {
            string gem;
            switch (type)
            {
                case GemType.Attack: gem = "AT"; break;
                case GemType.Mana: gem = "MA"; break;
                case GemType.ExpDrain: gem = "EX"; break;
                case GemType.Health: gem = "HP"; break;
                case GemType.Rage: gem = "RA"; break;
                case GemType.Shield: gem = "SH"; break;
                case GemType.Soul: gem = "SO"; break;
                case GemType.Rainbow: gem = "RB"; break;
                case GemType.Lightning: gem = "LI"; break;
                case GemType.Bagua: gem = "BG"; break;
                default: gem = "??"; break;
            }

            string state;
            switch (special)
            {
                case GemSpecialType.Line: state = "L"; break;
                case GemSpecialType.AreaBomb: state = "B"; break;
                case GemSpecialType.Rainbow: state = "R"; break;
                default: state = string.Empty; break;
            }
            return gem + state;
        }
#endif

        float GetGemFallDuration(float distanceInCells, bool isNewGem)
        {
            float minimumDuration = Mathf.Max(gemFallMinDuration, isNewGem ? .30f : 0f);
            float maximumDuration = Mathf.Max(minimumDuration, gemFallMaxDuration);
            return Mathf.Clamp(distanceInCells * gemFallSecondsPerCell, minimumDuration, maximumDuration);
        }

        void UpdateSiblingOrder()
        {
            for(int r=0;r<rows;r++)
            for(int c=0;c<columns;c++)
            {
                int desiredIndex = r * columns + c;
                Transform gem = grid[r, c].transform;
                if (gem.GetSiblingIndex() != desiredIndex)
                    gem.SetSiblingIndex(desiredIndex);
            }
        }

        void SetInput(bool value)
        {
            if (!value) SetSelected(null);
            if (grid == null) return;
            foreach (var gem in grid) if (gem != null) gem.SetInteractable(value);
        }
    }
}
