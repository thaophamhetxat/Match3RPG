using Match3RPG.Battle;
using TMPro;
using UnityEngine;

namespace Match3RPG.UI
{
    public sealed class TurnIndicator : MonoBehaviour
    {
        [SerializeField] TMP_Text stateText;
        [SerializeField] TMP_Text turnsText;
        [SerializeField] TMP_Text timerText;
        Color timerNormalColor = Color.white;
        Color timerWarningColor = Color.yellow;
        Color timerDangerColor = Color.red;
        float lastTimerRemainingTime;
        bool timerColorIsActive;

        public void Configure(TMP_Text state, TMP_Text turns, TMP_Text timer)
        {
            stateText = state;
            turnsText = turns;
            timerText = timer;
            if (stateText == null || turnsText == null || timerText == null)
                Debug.LogError("TurnIndicator requires StateText, TurnsText, and TimerText references.", this);
            Show(BattleTurnState.BattleStart, 0, 0, 0f, false);
        }

        public void Show(BattleTurnState state, int playerTurns, int bossTurns, float remainingTime, bool extraTurn)
        {
            if (stateText != null) stateText.text = string.Empty;
            if (turnsText != null) turnsText.text = FormatTurnCount(playerTurns);

            bool bossTurn = state == BattleTurnState.BossTurn
                || state == BattleTurnState.BossThinking
                || state == BattleTurnState.BossResolving;
            bool showTimer = state == BattleTurnState.PlayerTurn
                || state == BattleTurnState.PlayerResolving
                || state == BattleTurnState.PlayerTimedOut;
            if (timerText != null)
                timerText.text = bossTurn || !showTimer ? "--" : FormatTime(remainingTime);
            if (showTimer)
                UpdateTimerColor(remainingTime);
            else
            {
                timerColorIsActive = false;
                if (timerText != null)
                    timerText.color = timerNormalColor;
            }
        }

        public void SetTimer(float remainingTime)
        {
            if (timerText == null) return;
            timerText.text = FormatTime(remainingTime);
            UpdateTimerColor(remainingTime);
        }

        public void ConfigureTimerColors(Color normalColor, Color warningColor,
            Color dangerColor)
        {
            timerNormalColor = normalColor;
            timerWarningColor = warningColor;
            timerDangerColor = dangerColor;
            if (timerColorIsActive)
                UpdateTimerColor(lastTimerRemainingTime);
            else if (timerText != null)
                timerText.color = timerNormalColor;
        }

        public void ShowMessage(string firstLine, string secondLine = null)
        {
            if (stateText != null) stateText.text = string.Empty;
        }

        public static string FormatTurnCount(int turns)
        {
            return $"x{Mathf.Clamp(turns, 0, 5)}";
        }

        public static string FormatTime(float remainingTime) =>
            Mathf.CeilToInt(Mathf.Max(0f, remainingTime)).ToString();

        void UpdateTimerColor(float remainingTime)
        {
            if (timerText == null) return;
            lastTimerRemainingTime = remainingTime;
            timerColorIsActive = true;
            timerText.color = remainingTime <= 3f ? timerDangerColor
                : remainingTime <= 5f ? timerWarningColor : timerNormalColor;
        }
    }
}