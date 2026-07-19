using System;
using Game.Gameplay.Flows._1_SetUp;
using Game.Gameplay.Flows.Gameplay;
using UnityEngine;

namespace Game.Gameplay.Timer
{
    public class TimerManager : MonoBehaviour
    {
        [SerializeField] 
        private float m_gameDuration = 60f;

        public float TimeLeft { get; private set; } = -1f;

        private void Start()
        {
            SetUpEventsHooker.RegisterPostInitializationCallback(eventsHooker =>
            {
                eventsHooker.OnSetUpCompleted += HandleSetUpCompleted;
            });
            
            GameplayEventsHooker.RegisterPostInitializationCallback(hooker =>
            {
                hooker.OnGameplayResumed += HandleGameplayResumed;
                hooker.OnGameplayPaused += HandleGameplayPaused;
            });
        }

        private void OnDestroy()
        {
            var setUpEventsHooker = SetUpEventsHooker.Instance;
            if (setUpEventsHooker)
            {
                setUpEventsHooker.OnSetUpCompleted -= HandleSetUpCompleted; 
            }
            
            var gameplayEventsHooker = GameplayEventsHooker.Instance;
            if (gameplayEventsHooker)
            {
                gameplayEventsHooker.OnGameplayResumed -= HandleGameplayResumed;
                gameplayEventsHooker.OnGameplayPaused -= HandleGameplayPaused;
            }
        }

        private void HandleGameplayResumed()
        {
            ResumeTimer();
        }

        private void HandleGameplayPaused()
        {
            PauseTimer();
        }

        private void HandleSetUpCompleted()
        {
            SetUpEventsHooker.Instance.OnSetUpCompleted -= HandleSetUpCompleted; 
            ResetTimer();
        }

        public void ResetTimer()
        {
            TimeLeft = m_gameDuration;
        }

        public bool IsTicking { get; private set; }
        public Action<TimerManager> OnTimerResumed;
        public void ResumeTimer()
        {
            IsTicking = true;
            OnTimerResumed?.Invoke(this);
        }

        public Action<TimerManager> OnTimerPaused;
        public void PauseTimer()
        {
            IsTicking = false;
            OnTimerPaused?.Invoke(this);
        }

        public Action<TimerManager> OnTimerCompleted;
        private void Update()
        {
            if (!IsTicking)
                return;
            TimeLeft -= Time.deltaTime;

            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                OnTimerCompleted?.Invoke(this);
                PauseTimer();
            }
        }
    }
}