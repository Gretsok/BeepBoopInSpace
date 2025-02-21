using System;
using System.Collections;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.FlowMachine;
using UnityEngine;

namespace Game.Gameplay.Flows.NewRoundAnnouncement
{
    public class NewRoundAnnouncementState : AFlowState
    {
        [SerializeField]
        private AFlowState m_nextState;

        [SerializeField] 
        private float m_stateDuration = 4f;

        protected override void HandleEnter()
        {
            base.HandleEnter();
            

            StartCoroutine(WaitAndDo(m_stateDuration, () => RequestState(m_nextState)));
        }

        private static IEnumerator WaitAndDo(float duration, Action action)
        {
            yield return new WaitForSeconds(duration);
            action?.Invoke();
        }
    }
}