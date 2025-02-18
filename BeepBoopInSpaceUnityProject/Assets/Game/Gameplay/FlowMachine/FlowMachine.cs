using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.FlowMachine
{
    public class FlowMachine : MonoBehaviour
    {
        [SerializeField] 
        private AFlowState m_defaultState;

        private void Start()
        {
            RequestState(m_defaultState);
        }

        public AFlowState CurrentState { get; private set; }

        private Queue<AFlowState> m_requestedStates = new();
        public void RequestState(AFlowState newState)
        {
            m_requestedStates.Enqueue(newState);
        }

        private void Update()
        {
            if (m_requestedStates.Count > 0)
            {
                var state = m_requestedStates.Dequeue();
                SwitchToState(state);
            }
            
            if (CurrentState)
                CurrentState.DoUpdate();
        }

        private void SwitchToState(AFlowState newState)
        {
            if (CurrentState)
            {
                CurrentState.Leave();
                CurrentState.OnAnotherStateRequested -= HandleAnotherStateRequested;
            }
            CurrentState = newState;
            if (CurrentState)
            {
                CurrentState.OnAnotherStateRequested += HandleAnotherStateRequested;
                CurrentState.Enter();
            }
        }

        private void HandleAnotherStateRequested(AFlowState obj)
        {
            RequestState(obj);
        }
    }
}