using System;
using System.Collections;
using Game.ArchitectureTools.FlowMachine;
using Game.Global.SFXManagement;
using Game.SFXManagement;
using UnityEngine;

namespace Game.Gameplay.ConfigurationsManagement
{
    public class NewConfigurationAnnouncementState : AFlowState
    {
        [SerializeField] 
        private float m_duration = 2f;
        [SerializeField]
        private AFlowState m_nextState = null;

        [SerializeField]
        private AudioPlayer m_pauseAudioPlayer;
        [SerializeField]
        private AudioPlayer m_resumeAudioPlayer;
        
        protected override void HandleEnter()
        {
            base.HandleEnter();
            m_pauseAudioPlayer.Play();

            StartCoroutine(WaitAndDo(m_duration, () => RequestState(m_nextState)));
        }

        private static IEnumerator WaitAndDo(float duration, Action action)
        {
            yield return new WaitForSeconds(duration);
            action?.Invoke();
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            
            m_resumeAudioPlayer.Play();
        }
    }
}