using System.Collections;
using System.Collections.Generic;
using Game.Gameplay.FlowMachine;
using Game.Gameplay.GridSystem;
using UnityEngine;

namespace Game.Gameplay.Flows
{
    public class LoadState : AFlowState
    {
        [SerializeField] 
        private AFlowState m_nextState;

        [SerializeField] 
        private List<Cell> m_spawnPointsCells;
        
        protected override void HandleEnter()
        {
            base.HandleEnter();
            StartCoroutine(LoadingRoutine());
        }

        private IEnumerator LoadingRoutine()
        {
            yield return null;

            RequestState(m_nextState);
        }
    }
}