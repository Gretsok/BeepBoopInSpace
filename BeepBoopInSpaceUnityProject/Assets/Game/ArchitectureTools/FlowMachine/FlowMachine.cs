using System.Collections.Generic;
using NaughtyAttributes;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.ArchitectureTools.FlowMachine
{
    public class FlowMachine : MonoBehaviour,
        IFlowMachinePauser // It means a flow machine can pause another flow machine.
    {
        public struct SStateRequest
        {
            public enum ERequestType
            {
                AddingToBackStack = 0,
                Back = 1,
                IgnoreBackStack = 2
            }
            public AFlowState State;
            public ERequestType RequestType;
        }
        [field: ReadOnly]
        public ERunningState RunningState { get; private set; } = ERunningState.Inactive;
        [SerializeField] 
        private AFlowState m_defaultState;

        private Stack<AFlowState> m_backStack = new Stack<AFlowState>();

        [Button]
        public void Run()
        {
            if (RunningState != ERunningState.Inactive)
                return;
            
            RequestState(m_defaultState);
            RunningState = ERunningState.Running;
            Debug.Log($"[FM] {gameObject.name} - started running.");
        }

        [Button]
        public void Stop()
        {
            if (RunningState == ERunningState.Inactive)
                return;
            
            m_requestedStates.Clear();
            SwitchToState(null);
            RunningState = ERunningState.Inactive;
            Debug.Log($"[FM] {gameObject.name} - stopped running.");
        }

        public AFlowState CurrentState { get; private set; }

        private Queue<SStateRequest> m_requestedStates = new();
        public void RequestState(AFlowState newState, bool ignoreBackStack = false)
        {
            SStateRequest request = new SStateRequest()
            {
                State = newState,
                RequestType = ignoreBackStack
                    ? SStateRequest.ERequestType.IgnoreBackStack
                    : SStateRequest.ERequestType.AddingToBackStack
            };
            m_requestedStates.Enqueue(request);
            Debug.Log($"[FM] {gameObject.name} - Request state : {newState.gameObject} of type {newState.GetType()}.");
        }

        public void RequestBack()
        {
            SStateRequest request = new SStateRequest()
            {
                RequestType = SStateRequest.ERequestType.Back
            };
            m_requestedStates.Enqueue(request);
            Debug.Log($"[FM] {gameObject.name} - Request back.");
        }

        public void ClearBackStack()
        {
            m_backStack.Clear();
            Debug.Log($"[FM] {gameObject.name} - Back stack cleared.");
        }

        private void Update()
        {
            if (RunningState != ERunningState.Running) return;
            
            if (m_requestedStates.Count > 0)
            {
                var request = m_requestedStates.Dequeue();
                if (request.RequestType == SStateRequest.ERequestType.AddingToBackStack)
                {
                    Debug.Log($"[FM] {gameObject.name} - {request.State.name} - About to switch state by adding to back stack.");
                    m_backStack.Push(request.State);
                    SwitchToState(request.State);
                }
                else if (request.RequestType == SStateRequest.ERequestType.IgnoreBackStack)
                {
                    Debug.Log($"[FM] {gameObject.name} - {request.State.name} - About to switch state by ignoring back stack.");
                    SwitchToState(request.State);
                }
                else if (request.RequestType == SStateRequest.ERequestType.Back)
                {
                    var backState = m_backStack.Peek();
                    if (backState == CurrentState)
                    {
                        m_backStack.Pop();
                        backState = m_backStack.Peek();
                    }
                    Debug.Log($"[FM] {gameObject.name} - {backState.name} - About to switch state by going back.");
                    SwitchToState(backState);
                }
            }
            
            if (CurrentState)
                CurrentState.DoUpdate();
        }

        private void SwitchToState(AFlowState newState)
        {
            if (CurrentState)
            {
                Debug.Log($"[FM] {gameObject.name} - {CurrentState.name} - LEAVE ");
                CurrentState.Leave();
                CurrentState.OnAnotherStateRequested -= HandleAnotherStateRequested;
                CurrentState.OnBackRequested -= HandleBackRequested;
            }
            CurrentState = newState;
            if (CurrentState)
            {
                Debug.Log($"[FM] {gameObject.name} - {CurrentState.name} - ENTER ");
                CurrentState.OnAnotherStateRequested += HandleAnotherStateRequested;
                CurrentState.OnBackRequested += HandleBackRequested;
                CurrentState.Enter();
            }
        }

        private void HandleAnotherStateRequested(AFlowState newState, bool ignoreBackStack)
        {
            RequestState(newState, ignoreBackStack);
        }

        private void HandleBackRequested()
        {
            RequestBack();
        }
        
        private List<IFlowMachinePauser> m_pausers = new();
        public bool IsPaused => m_pausers.Count > 0;

        public void Pause(IFlowMachinePauser pauser)
        {
            Debug.Log($"[FM] {gameObject.name} - Adding a pauser.");
            var isPaused = IsPaused;
            m_pausers.Add(pauser);
            if (!isPaused && IsPaused)
            {
                Debug.Log($"[FM] {gameObject.name} - Paused.");
                HandleFlowMachinePaused();
                RunningState = ERunningState.Paused;
            }
        }

        public void Unpause(IFlowMachinePauser pauser)
        {
            Debug.Log($"[FM] {gameObject.name} - Removing a pauser.");
            var isPaused = IsPaused;
            m_pausers.Remove(pauser);
            if (isPaused && !IsPaused)
            {
                HandleFlowMachineUnpaused();
                RunningState = ERunningState.Running;
                Debug.Log($"[FM] {gameObject.name} - Unpaused.");
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