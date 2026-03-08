using System;
using UnityEngine;

namespace Game.ArchitectureTools.FlowMachine
{
    public enum ERunningState
    {
        Inactive = 0,
        Running = 1,
        Paused = 2
    }
    public class AFlowState : MonoBehaviour
    {
        public ERunningState RunningState { get; private set; } = ERunningState.Inactive;
        
        public Action<AFlowState> OnEntered;
        public void Enter()
        {
            HandleEnter();
            RunningState = ERunningState.Running;
            OnEntered?.Invoke(this);
        }
        
        protected virtual void HandleEnter()
        {}

        public void DoUpdate()
        {
            HandleDoUpdate();
        }
        
        protected virtual void HandleDoUpdate()
        {}

        public Action<AFlowState> OnLeft;
        public void Leave()
        {
            HandleLeave();
            RunningState = ERunningState.Inactive;
            OnLeft?.Invoke(this);
        }
        
        protected virtual void HandleLeave()
        {}

        public Action<AFlowState> OnPaused;
        public void NotifyFlowMachinePaused()
        {
            if (RunningState != ERunningState.Running)
            {
                Debug.LogError($"Only running state can be paused: Current running state is {RunningState}", gameObject);
                return;
            }
            RunningState = ERunningState.Paused;
            HandleFlowMachinePaused();
        }
        
        protected virtual void HandleFlowMachinePaused()
        {}
        
        public Action<AFlowState> OnUnpaused;
        public void NotifyFlowMachineUnpaused()
        {
            if (RunningState != ERunningState.Paused)
            {
                Debug.LogError($"Only paused state can be unpaused: Current running state is {RunningState}", gameObject);
                return;
            }
            RunningState = ERunningState.Running;
            HandleFlowMachineUnpaused();
        }
        
        protected virtual void HandleFlowMachineUnpaused()
        {}
        
        public Action<AFlowState> OnAnotherStateRequested;
        protected void RequestState(AFlowState newState)
        {
            OnAnotherStateRequested?.Invoke(newState);
        }
    }
}