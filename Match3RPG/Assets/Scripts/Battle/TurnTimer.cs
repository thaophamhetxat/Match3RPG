using System;
using Match3RPG.UI;
using UnityEngine;

namespace Match3RPG.Battle
{
    public sealed class TurnTimer : MonoBehaviour
    {
        [SerializeField] TurnIndicator indicator;
        public event Action<float> TimeChanged;
        public event Action TimedOut;
        public float RemainingTime { get; private set; }
        public bool IsRunning { get; private set; }
        bool timeoutReported;

        public void Configure(TurnIndicator turnIndicator)
        {
            indicator = turnIndicator;
            if (indicator == null)
                Debug.LogError("TurnTimer requires a TurnIndicator reference.", this);
            RefreshIndicator();
        }

        public void StartTimer(float duration)
        {
            RemainingTime = Mathf.Max(0f, duration);
            timeoutReported = false;
            IsRunning = true;
            TimeChanged?.Invoke(RemainingTime);
            RefreshIndicator();
            Debug.Log($"TURN TIMER START | Time={RemainingTime:0}");
        }

        public void Pause()
        {
            IsRunning = false;
        }

        public void Resume()
        {
            if (RemainingTime > 0f && !timeoutReported) IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
            timeoutReported = true;
        }

        void Update()
        {
            AdvanceTime(Time.unscaledDeltaTime);
        }

        public void AdvanceTime(float elapsedSeconds)
        {
            if (!IsRunning || elapsedSeconds <= 0f) return;
            RemainingTime = Mathf.Max(0f, RemainingTime - elapsedSeconds);
            TimeChanged?.Invoke(RemainingTime);
            RefreshIndicator();
            if (RemainingTime > 0f || timeoutReported) return;
            timeoutReported = true;
            IsRunning = false;
            TimedOut?.Invoke();
        }

        void RefreshIndicator()
        {
            if (IsRunning && indicator != null) indicator.SetTimer(RemainingTime);
        }
    }
}