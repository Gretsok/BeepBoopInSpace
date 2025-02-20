using System;
using TMPro;
using UnityEngine;

namespace Game.Gameplay.Timer
{
    public class TimerPanel : MonoBehaviour
    {
        [SerializeField] 
        private TMP_Text m_timerText;

        private TimerManager m_timerManager;
        private void Start()
        {
            TimerManager.RegisterPostInitializationCallback(manager => m_timerManager = manager);
        }

        private void Update()
        {
            if (!m_timerManager) 
                return;

            m_timerText.text = $"{(int)(m_timerManager.TimeLeft / 60)}:{(int)(m_timerManager.TimeLeft % 60)}";
        }
    }
}