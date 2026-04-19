using TMPro;
using UnityEngine;

namespace Game.Gameplay.Timer
{
    public class TimerPanel : MonoBehaviour
    {
        [SerializeField] 
        private TMP_Text m_timerText;

        [SerializeField] 
        private TimerManager m_timerManager;

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

        private void Update()
        {
            if (!m_timerManager) 
                return;

            m_timerText.text = $"{(int)(m_timerManager.TimeLeft / 60)}:{(int)(m_timerManager.TimeLeft % 60)}";
        }
    }
}