using System;
using Game.Gameplay.ConfigurationsManagement;
using Game.Gameplay.Flows._1_SetUp;
using UnityEngine;

namespace Game.Gameplay.Timer
{
    public class TimerManager : MonoBehaviour
    {
        private ConfigurationsManager m_configurationsManager;
        [SerializeField] 
        private float m_gameDuration = 60f;

        public float TimeLeft { get; private set; } = -1f;

        private void Start()
        {
            m_configurationsManager = ConfigurationsManager.Instance;
            m_configurationsManager.OnResumeRunning += HandleResumeRunning;
            m_configurationsManager.OnPauseRunning += HandlePauseRunning;
            
            SetUpEventsHooker.RegisterPostInitializationCallback(eventsHooker =>
            {
                eventsHooker.OnSetUpCompleted += HandleSetUpCompleted;
            });
        }

        private void HandleSetUpCompleted()
        {
            ResetTimer();
        }

        private void OnDestroy()
        {
            if (m_configurationsManager)
            {
                m_configurationsManager.OnResumeRunning -= HandleResumeRunning;
                m_configurationsManager.OnPauseRunning -= HandlePauseRunning;
            }
        }

        private void HandleResumeRunning(ConfigurationsManager obj)
        {
            ResumeTimer();
        }

        private void HandlePauseRunning(ConfigurationsManager obj)
        {
            PauseTimer();
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