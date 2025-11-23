using System;
using System.Collections;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.FlowMachine;
using UnityEngine;

namespace Game.Gameplay.Flows.Gameplay
{
    [RequireComponent(typeof(GameplayState))]
    public class GameplayEventsHooker : AManager<GameplayEventsHooker>
    {
        public event Action OnGameplayResumed;
        public event Action OnGameplayPaused;
        
        private GameplayState m_gameplayState;

        protected override IEnumerator Initialize()
        {
            m_gameplayState = GetComponent<GameplayState>();
            m_gameplayState.OnEntered += HandleStateEntered;
            m_gameplayState.OnLeft += HandleStateLeft;
            yield break;
        }

        private void HandleStateEntered(AFlowState obj)
        {
            OnGameplayResumed?.Invoke();
        }

        private void HandleStateLeft(AFlowState obj)
        {
            OnGameplayPaused?.Invoke();
        }
    }
}
