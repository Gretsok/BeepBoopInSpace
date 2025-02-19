using System;
using System.Collections;
using Game.Gameplay.FlowMachine;
using UnityEngine;

namespace Game.Gameplay.ConfigurationsManagement
{
    public class NewConfigurationAnnouncementState : AFlowState
    {
        [SerializeField] 
        private float m_duration = 2f;
        [SerializeField]
        private AFlowState m_nextState = null;

        protected override void HandleEnter()
        {
            base.HandleEnter();
            

            StartCoroutine(WaitAndDo(m_duration, () => RequestState(m_nextState)));
        }

        private static IEnumerator WaitAndDo(float duration, Action action)
        {
            yield return new WaitForSeconds(duration);
            action?.Invoke();
        }
    }
}