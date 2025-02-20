using System;
using Game.ArchitectureTools.Manager;
using UnityEngine;

namespace Game.Gameplay.Timer
{
    public class TimerManager : AManager<TimerManager>
    {
        [SerializeField] 
        private float m_gameDuration = 60f;

        public float TimeLeft { get; private set; } = -1f;
        
        public void ResetTimer()
        {
            TimeLeft = m_gameDuration;
        }

        public bool IsTicking { get; private set; }
        public void ResumeTimer()
        {
            IsTicking = true;
        }

        public void PauseTimer()
        {
            IsTicking = false;
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
                ResumeTimer();
            }
        }
    }
}