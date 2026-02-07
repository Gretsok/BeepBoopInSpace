using System;
using System.Collections;
using Game.Gameplay.FlowMachine;
using Game.Gameplay.MusicsManagement;
using Game.Global.SFXManagement;
using Game.SFXManagement;
using UnityEngine;

namespace Game.Gameplay.Flows.Finish
{
    public class FinishState : AFlowState
    {
        [SerializeField]
        private AFlowState m_nextState;
        [SerializeField]
        private float m_stateDuration = 4f;

        [SerializeField]
        private AudioPlayer m_endAudioPlayer;
        
        protected override void HandleEnter()
        {
            base.HandleEnter();
            
            m_endAudioPlayer.Play();
            MusicsManager.Instance.StopPlayingGameplayMusics();
            
            StartCoroutine(WaitAndDo(m_stateDuration, () => RequestState(m_nextState)));
        }

        private static IEnumerator WaitAndDo(float duration, Action action)
        {
            yield return new WaitForSeconds(duration);
            action?.Invoke();
        }
    }
}