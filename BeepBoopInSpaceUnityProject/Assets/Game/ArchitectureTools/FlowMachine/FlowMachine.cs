using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Game.ArchitectureTools.FlowMachine
{
    public class FlowMachine : MonoBehaviour,
        IFlowMachinePauser // It means a flow machine can pause another flow machine.
    {
        [field: ReadOnly]
        public ERunningState RunningState { get; private set; } = ERunningState.Inactive;
        [SerializeField] 
        private AFlowState m_defaultState;

        [Button]
        public void Run()
        {
            if (RunningState != ERunningState.Inactive)
                return;
            
            RequestState(m_defaultState);
            RunningState = ERunningState.Running;
        }

        [Button]
        public void Stop()
        {
            if (RunningState == ERunningState.Inactive)
                return;
            
            m_requestedStates.Clear();
            SwitchToState(null);
            RunningState = ERunningState.Inactive;
        }

        public AFlowState CurrentState { get; private set; }

        private Queue<AFlowState> m_requestedStates = new();
        public void RequestState(AFlowState newState)
        {
            m_requestedStates.Enqueue(newState);
        }

        private void Update()
        {
            if (RunningState != ERunningState.Running) return;
            
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
                Debug.Log($"About to leave {CurrentState.name}");
                CurrentState.Leave();
                CurrentState.OnAnotherStateRequested -= HandleAnotherStateRequested;
            }
            CurrentState = newState;
            if (CurrentState)
            {
                Debug.Log($"About to enter {CurrentState.name}");
                CurrentState.OnAnotherStateRequested += HandleAnotherStateRequested;
                CurrentState.Enter();
            }
        }

        private void HandleAnotherStateRequested(AFlowState obj)
        {
            RequestState(obj);
        }
        
        private List<IFlowMachinePauser> m_pausers = new();
        public bool IsPaused => m_pausers.Count > 0;

        public void Pause(IFlowMachinePauser pauser)
        {
            var isPaused = IsPaused;
            m_pausers.Add(pauser);
            if (!isPaused && IsPaused)
            {
                HandleFlowMachinePaused();
                RunningState = ERunningState.Paused;
            }
        }

        public void Unpause(IFlowMachinePauser pauser)
        {
            var isPaused = IsPaused;
            m_pausers.Remove(pauser);
            if (isPaused && !IsPaused)
            {
                HandleFlowMachineUnpaused();
                RunningState = ERunningState.Running;
            }
        }

        private void HandleFlowMachinePaused()
        {
            if (CurrentState)
                CurrentState.NotifyFlowMachinePaused();
        }

        private void HandleFlowMachineUnpaused()
        {
            if (CurrentState)
                CurrentState.NotifyFlowMachineUnpaused();
        }
    }
}