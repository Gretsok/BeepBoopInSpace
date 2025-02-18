using System;
using UnityEngine;

namespace Game.Gameplay.FlowMachine
{
    public class AFlowState : MonoBehaviour
    {
        public bool IsRunning { get; private set; } = false;
        
        public Action<AFlowState> OnEntered;
        public void Enter()
        {
            HandleEnter();
            IsRunning = true;
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
            IsRunning = false;
            OnLeft?.Invoke(this);
        }
        
        protected virtual void HandleLeave()
        {}

        public Action<AFlowState> OnAnotherStateRequested;
        protected void RequestState(AFlowState newState)
        {
            OnAnotherStateRequested?.Invoke(newState);
        }
    }
}