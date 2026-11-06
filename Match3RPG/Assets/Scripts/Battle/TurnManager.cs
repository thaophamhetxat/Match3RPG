using System.Collections;
using Match3RPG.Board;
using Match3RPG.Core;
using Match3RPG.UI;
using UnityEngine;

namespace Match3RPG.Battle
{
    public sealed class TurnManager : MonoBehaviour
    {
        const float PlayerDecisionSeconds = 10f;
        [SerializeField] BoardManager board;
        [SerializeField] BattleController battle;
        [SerializeField] BossAI bossAI;
        [SerializeField] TurnIndicator turnIndicator;
        [SerializeField] TurnTimer turnTimer;
        [SerializeField, Min(1)] int maximumStoredTurns = 5;
        [SerializeField, Min(0f)] float bossThinkingDelay = .8f;
        [SerializeField, Min(0f)] float bossTurnDisplayDelay = .15f;
        [SerializeField, Min(0f)] float statusMessageDuration = .65f;

        Coroutine activeTurn;
        Coroutine extraTurnMessageRoutine;
        readonly BoardShuffleService shuffleService = new BoardShuffleService();
        int playerTurnsRemaining;
        int bossTurnsRemaining;
        bool extraTurnMessage;
        bool pausedForStatusPopup;
        public BattleTurnState State { get; private set; } = BattleTurnState.BattleStart;
        public BattleActor CurrentActor { get; private set; } = BattleActor.Player;
        public int PlayerTurnsRemaining => playerTurnsRemaining;
        public int BossTurnsRemaining => bossTurnsRemaining;
        public float RemainingTime => turnTimer != null ? turnTimer.RemainingTime : 0f;
        public BattleController Battle => battle;
        public TurnTimer Timer => turnTimer;
        public bool IsPausedForStatusPopup => pausedForStatusPopup;
        int StoredTurnLimit => Mathf.Clamp(maximumStoredTurns, 1, 5);

        public static BattleActor? GetFasterActor(float heroSpeed, float bossSpeed)
        {
            if (heroSpeed > bossSpeed)
                return BattleActor.Player;
            if (bossSpeed > heroSpeed)
                return BattleActor.Boss;
            return null;
        }

        public bool PauseForStatusPopup()
        {
            if (State == BattleTurnState.Victory || State == BattleTurnState.Defeat)
                return false;
            pausedForStatusPopup = true;
            turnTimer?.Pause();
            board?.SetInputEnabled(false);
            return true;
        }

        public void ResumeFromStatusPopup()
        {
            if (!pausedForStatusPopup)
                return;
            pausedForStatusPopup = false;
            if (State == BattleTurnState.PlayerTurn && board != null && !board.IsResolving)
            {
                turnTimer?.Resume();
                board.SetInputEnabled(true);
            }
            else
                board?.SetInputEnabled(false);
        }

        public void StopForBattleExit()
        {
            pausedForStatusPopup = true;
            turnTimer?.Stop();
            if (activeTurn != null) StopCoroutine(activeTurn);
            if (extraTurnMessageRoutine != null) StopCoroutine(extraTurnMessageRoutine);
            activeTurn = null;
            extraTurnMessageRoutine = null;
            board?.SetInputEnabled(false);
        }

        public void Configure(BoardManager boardManager, BattleController battleController, BossAI ai, TurnIndicator indicatorView)
        {
            if (board != null) board.PlayerMoveRequested -= OnPlayerMoveRequested;
            board = boardManager;
            battle = battleController;
            bossAI = ai;
            turnIndicator = indicatorView;
            if (board == null) Debug.LogError("TurnManager requires a BoardManager reference.", this);
            if (battle == null) Debug.LogError("TurnManager requires a BattleController reference.", this);
            if (bossAI == null) Debug.LogError("TurnManager requires a BossAI reference.", this);
            if (turnIndicator == null) Debug.LogError("TurnManager requires a TurnIndicator reference.", this);
            if (turnTimer == null) turnTimer = GetComponent<TurnTimer>();
            if (turnTimer == null) turnTimer = gameObject.AddComponent<TurnTimer>();
            turnTimer.Configure(turnIndicator);
            turnTimer.TimedOut -= OnTimerExpired;
            turnTimer.TimedOut += OnTimerExpired;
            turnTimer.TimeChanged -= OnTimeChanged;
            turnTimer.TimeChanged += OnTimeChanged;
            BindBoard();
            SetState(BattleTurnState.BattleStart);
        }

        void OnEnable()
        {
            BindBoard();
            if (turnTimer != null)
            {
                turnTimer.TimedOut -= OnTimerExpired;
                turnTimer.TimedOut += OnTimerExpired;
                turnTimer.TimeChanged -= OnTimeChanged;
                turnTimer.TimeChanged += OnTimeChanged;
            }
        }

        void OnDisable()
        {
            if (board != null) board.PlayerMoveRequested -= OnPlayerMoveRequested;
            if (turnTimer != null)
            {
                turnTimer.TimedOut -= OnTimerExpired;
                turnTimer.TimeChanged -= OnTimeChanged;
                turnTimer.Stop();
            }
            if (activeTurn != null) StopCoroutine(activeTurn);
            if (extraTurnMessageRoutine != null) StopCoroutine(extraTurnMessageRoutine);
            activeTurn = null;
            extraTurnMessageRoutine = null;
        }

        void Start()
        {
            board.EnsureBoardReady();
            activeTurn = StartCoroutine(StartPlayerPhase(true, false, PlayerDecisionSeconds));
        }

        void BindBoard()
        {
            if (!isActiveAndEnabled || board == null) return;
            board.PlayerMoveRequested -= OnPlayerMoveRequested;
            board.PlayerMoveRequested += OnPlayerMoveRequested;
        }

        void OnPlayerMoveRequested(MoveResult move)
        {
            if (State != BattleTurnState.PlayerTurn || activeTurn != null || move == null || !AllowsPlayerInput(State)) return;
            turnTimer.Stop();
            SetState(BattleTurnState.PlayerResolving);
            activeTurn = StartCoroutine(ResolvePlayerAction(move));
        }

        IEnumerator ResolvePlayerAction(MoveResult move)
        {
            MoveResult resolved = null;
            yield return board.ResolveMove(move, result => resolved = result);
            yield return WaitWhileStatusPopupClosed();
            if (resolved == null)
            {
                BeginPlayerDecision(PlayerDecisionSeconds, false);
                yield break;
            }

            if (!resolved.WasValid)
            {
                playerTurnsRemaining = ConsumeTurn(playerTurnsRemaining, 0, StoredTurnLimit);
                RefreshIndicator();
                SetState(BattleTurnState.PlayerInvalidMove);
                turnIndicator.ShowMessage("INVALID MOVE!");
                yield return WaitForStatusDuration(statusMessageDuration);

                SetState(BattleTurnState.BossCounterAttack);
                turnIndicator.ShowMessage("BOSS COUNTER ATTACK!");
                yield return battle.ApplyInvalidMovePenalty();
                yield return WaitWhileStatusPopupClosed();
                if (CheckBattleEnd()) yield break;

                float counterattackNextDecisionTime = PlayerDecisionSeconds;
                yield return EnsureBoardPlayable(BattleActor.Player, counterattackNextDecisionTime,
                    time => counterattackNextDecisionTime = time);
                ContinueAfterPlayerAction(false, counterattackNextDecisionTime);
                yield break;
            }

            playerTurnsRemaining = ConsumeTurn(playerTurnsRemaining, resolved.ExtraTurnsGranted, StoredTurnLimit);
            RefreshIndicator();
            if (resolved.ExtraTurnsGranted > 0)
                Debug.Log($"MATCH EXTRA TURN REWARD x{resolved.ExtraTurnsGranted} | Turns={playerTurnsRemaining}");
            yield return battle.ResolveMove(BattleActor.Player, resolved);
            yield return WaitWhileStatusPopupClosed();
            if (CheckBattleEnd()) yield break;

            float nextDecisionTime = PlayerDecisionSeconds;
            yield return EnsureBoardPlayable(BattleActor.Player, nextDecisionTime, time => nextDecisionTime = time);
            ContinueAfterPlayerAction(resolved.ExtraTurnsGranted > 0, nextDecisionTime);
        }

        void ContinueAfterPlayerAction(bool earnedExtraTurn, float savedTime)
        {
            BattleTurnState nextState = GetStateAfterPlayerAction(playerTurnsRemaining);
            if (nextState == BattleTurnState.PlayerTurn)
            {
                BeginPlayerDecision(earnedExtraTurn ? PlayerDecisionSeconds : savedTime, earnedExtraTurn);
                return;
            }
            extraTurnMessage = false;
            activeTurn = StartCoroutine(StartBossPhase(true));
        }

        void OnTimerExpired()
        {
            if (State != BattleTurnState.PlayerTurn || activeTurn != null) return;
            SetState(BattleTurnState.PlayerTimedOut);
            playerTurnsRemaining = ConsumeTurn(playerTurnsRemaining, 0, StoredTurnLimit);
            if (turnIndicator != null) turnIndicator.ShowMessage("TIME OUT!", "TURN LOST");
            activeTurn = StartCoroutine(HandleTimeout());
        }

        IEnumerator HandleTimeout()
        {
            yield return WaitForStatusDuration(statusMessageDuration);
            if (CheckBattleEnd()) yield break;

            SetState(BattleTurnState.BossCounterAttack);
            turnIndicator?.ShowMessage("BOSS COUNTER ATTACK!");
            yield return battle.ApplyInvalidMovePenalty();
            yield return WaitWhileStatusPopupClosed();
            if (CheckBattleEnd()) yield break;

            float nextDecisionTime = PlayerDecisionSeconds;
            yield return EnsureBoardPlayable(BattleActor.Player, nextDecisionTime, time => nextDecisionTime = time);
            ContinueAfterPlayerAction(false, nextDecisionTime);
        }

        IEnumerator StartPlayerPhase(bool newPhase, bool earnedExtraTurn, float savedTime)
        {
            yield return null;
            yield return WaitWhileStatusPopupClosed();
            if (CheckBattleEnd()) yield break;
            if (newPhase) playerTurnsRemaining = 1;
            CurrentActor = BattleActor.Player;
            SetState(BattleTurnState.BattleStart);
            float restoredTime = savedTime;
            yield return EnsureBoardPlayable(BattleActor.Player, restoredTime, time => restoredTime = time);
            BeginPlayerDecision(restoredTime, earnedExtraTurn);
        }

        void BeginPlayerDecision(float time, bool earnedExtraTurn)
        {
            if (battle.IsGameOver)
            {
                CheckBattleEnd();
                return;
            }
            battle.AdvancePlayerTurn();
            extraTurnMessage = earnedExtraTurn;
            SetState(BattleTurnState.PlayerTurn);
            turnTimer.StartTimer(Mathf.Max(0f, time));
            Debug.Log($"PLAYER TURN | Turns={playerTurnsRemaining} | Time={turnTimer.RemainingTime:0}");
            RefreshIndicator();
            if (extraTurnMessage)
            {
                if (extraTurnMessageRoutine != null) StopCoroutine(extraTurnMessageRoutine);
                extraTurnMessageRoutine = StartCoroutine(ClearExtraTurnMessage());
            }
            activeTurn = null;
        }

        IEnumerator ClearExtraTurnMessage()
        {
            yield return WaitForStatusDuration(.8f);
            extraTurnMessage = false;
            extraTurnMessageRoutine = null;
            if (State == BattleTurnState.PlayerTurn) RefreshIndicator();
        }

        IEnumerator StartBossPhase(bool newPhase)
        {
            yield return null;
            yield return WaitWhileStatusPopupClosed();
            if (CheckBattleEnd()) yield break;
            if (newPhase) bossTurnsRemaining = 1;
            CurrentActor = BattleActor.Boss;
            yield return EnsureBoardPlayable(BattleActor.Boss, 0f, null);

            while (bossTurnsRemaining > 0 && !battle.IsGameOver)
            {
                SetState(BattleTurnState.BossThinking);
                yield return WaitForStatusDuration(bossThinkingDelay);
                if (!board.HasAnyLegalMove())
                {
                    yield return EnsureBoardPlayable(BattleActor.Boss, 0f, null);
                    SetState(BattleTurnState.BossThinking);
                    yield return WaitForStatusDuration(bossThinkingDelay);
                }

                MoveResult move = bossAI.ChooseMove(board, battle.BossHealthRatio, battle.BossRageRatio);
                if (move == null)
                {
                    yield return EnsureBoardPlayable(BattleActor.Boss, 0f, null);
                    continue;
                }

                SetState(BattleTurnState.BossTurn);
                yield return WaitForStatusDuration(bossTurnDisplayDelay);
                SetState(BattleTurnState.BossResolving);
                MoveResult resolved = null;
                yield return board.ResolveMove(move, result => resolved = result);
                yield return WaitWhileStatusPopupClosed();
                if (resolved == null || !resolved.WasValid)
                {
                    Debug.LogWarning("BossAI selected a move that the board did not resolve.", this);
                    yield return EnsureBoardPlayable(BattleActor.Boss, 0f, null);
                    continue;
                }

                bossTurnsRemaining = ConsumeTurn(bossTurnsRemaining, resolved.ExtraTurnsGranted, StoredTurnLimit);
                RefreshIndicator();
                yield return battle.ResolveMove(BattleActor.Boss, resolved);
                yield return WaitWhileStatusPopupClosed();
                if (CheckBattleEnd()) yield break;
                yield return EnsureBoardPlayable(BattleActor.Boss, 0f, null);
            }

            if (CheckBattleEnd()) yield break;
            activeTurn = StartCoroutine(StartPlayerPhase(true, false, PlayerDecisionSeconds));
        }

        IEnumerator EnsureBoardPlayable(BattleActor actor, float savedTime, System.Action<float> timeRestored)
        {
            if (board.HasAnyLegalMove()) yield break;
            SetState(BattleTurnState.BoardShuffle);
            turnIndicator.ShowMessage("NO MOVES", "SHUFFLING...");
            bool shuffleComplete = false;
            yield return shuffleService.Shuffle(board, result => shuffleComplete = result);
            yield return WaitWhileStatusPopupClosed();
            if (!shuffleComplete)
            {
                Debug.LogError("Board shuffle failed to produce a playable board.", board);
                yield break;
            }
            turnIndicator.ShowMessage("BOARD READY");
            yield return WaitForStatusDuration(statusMessageDuration);
            if (actor == BattleActor.Player)
            {
                savedTime = RestorePlayerTimeAfterShuffle(savedTime);
                timeRestored?.Invoke(savedTime);
            }
            if (turnTimer != null) turnTimer.Pause();
        }

        bool CheckBattleEnd()
        {
            if (!battle.IsGameOver) return false;
            playerTurnsRemaining = 0;
            bossTurnsRemaining = 0;
            extraTurnMessage = false;
            turnTimer.Stop();
            SetState(battle.PlayerWon ? BattleTurnState.Victory : BattleTurnState.Defeat);
            activeTurn = null;
            return true;
        }

        void OnTimeChanged(float remaining)
        {
            if (State == BattleTurnState.PlayerTurn) RefreshIndicator();
        }

        void RefreshIndicator()
        {
            if (turnIndicator != null)
                turnIndicator.Show(State, playerTurnsRemaining, bossTurnsRemaining, RemainingTime, extraTurnMessage);
        }

        public static int ConsumeTurn(int currentTurns, int extraTurns, int maximumTurns)
        {
            return Mathf.Clamp(currentTurns - 1 + Mathf.Max(0, extraTurns), 0, maximumTurns);
        }

        public static BattleTurnState GetStateAfterPlayerAction(int turnsRemaining)
        {
            return turnsRemaining > 0 ? BattleTurnState.PlayerTurn : BattleTurnState.BossThinking;
        }

        public static float RestorePlayerTimeAfterShuffle(float remainingTime)
        {
            return Mathf.Max(remainingTime, 5f);
        }

        public static int ClampStoredTurns(int turns, int maximumTurns)
        {
            return Mathf.Clamp(turns, 0, maximumTurns);
        }

        public static BattleTurnState GetStateAfterMove(BattleActor actor, bool grantsExtraTurn)
        {
            if (actor == BattleActor.Player) return grantsExtraTurn ? BattleTurnState.PlayerTurn : BattleTurnState.BossThinking;
            return grantsExtraTurn ? BattleTurnState.BossTurn : BattleTurnState.PlayerTurn;
        }

        public static bool AllowsPlayerInput(BattleTurnState state) => state == BattleTurnState.PlayerTurn;

    #if UNITY_EDITOR
        [ContextMenu("Run deterministic turn battle checks")]
        void RunDeterministicChecks() => TurnBattleDebugChecks.RunAll(this);

        public void ConfigureDebugPlayerTurn(int playerTurns, float seconds)
        {
            playerTurnsRemaining = playerTurns;
            bossTurnsRemaining = 1;
            SetState(BattleTurnState.PlayerTurn);
            turnTimer.StartTimer(seconds);
        }

        public void TriggerDebugTimeout() => OnTimerExpired();
    #endif

        void SetState(BattleTurnState state)
        {
            State = state;
            if ((pausedForStatusPopup || state != BattleTurnState.PlayerTurn) && turnTimer != null)
                turnTimer.Pause();
            if (board != null) board.SetInputEnabled(!pausedForStatusPopup && AllowsPlayerInput(state));
            if (turnIndicator != null)
                turnIndicator.Show(state, playerTurnsRemaining, bossTurnsRemaining, RemainingTime, extraTurnMessage);
        }

        IEnumerator WaitWhileStatusPopupClosed()
        {
            while (pausedForStatusPopup)
            {
                while (board != null && board.IsResolving)
                    yield return null;
                if (!pausedForStatusPopup)
                    yield break;
                yield return null;
            }
        }

        IEnumerator WaitForStatusDuration(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                yield return WaitWhileStatusPopupClosed();
                float frameTime = Time.unscaledDeltaTime;
                yield return null;
                if (!pausedForStatusPopup)
                    elapsed += frameTime;
            }
        }
    }
}