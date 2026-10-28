using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Gameplay.Timer
{
    public class TimerPanel : AUIToolkitScreen
    {
        [SerializeField]
        private TimerManager m_timerManager;

        private Label m_timerLabel;

        private void Awake()
        {
            m_timerManager.OnTimerResumed += HandleTimerResumed;
            m_timerManager.OnTimerPaused += HandleTimerPaused;
            // We must be active right before here for Awake() to be called
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (m_timerManager)
            {
                m_timerManager.OnTimerResumed -= HandleTimerResumed;
                m_timerManager.OnTimerPaused -= HandleTimerPaused;
            }
        }

        private void HandleTimerResumed(TimerManager obj)
        {
            gameObject.SetActive(true);
        }

        private void HandleTimerPaused(TimerManager obj)
        {
            gameObject.SetActive(false);
        }

        protected override void HandleShown()
        {
            m_timerLabel = Query<Label>("timer-label");
            RefreshTime();
        }

        protected override void HandleUpdate()
        {
            RefreshTime();
        }

        private void RefreshTime()
        {
            if (!m_timerManager)
                return;

            var secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, m_timerManager.TimeLeft));
            m_timerLabel.text = $"{secondsLeft / 60}:{secondsLeft % 60:00}";
        }
    }
}
