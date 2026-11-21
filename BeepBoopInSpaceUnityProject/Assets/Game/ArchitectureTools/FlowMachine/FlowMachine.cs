using System;
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
                IgnoreBackStack = 2,
                BackTo = 3
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
            Run(m_defaultState);
        }

        /// <summary>
        /// Starts in <paramref name="firstState"/>, as if reached from the default state, then from
        /// <paramref name="backStates"/> in their order: going back leads through them.
        /// </summary>
        public void Run(AFlowState firstState, params AFlowState[] backStates)
        {
            if (RunningState != ERunningState.Inactive)
                return;

            if (firstState != m_defaultState)
                m_backStack.Push(m_defaultState);
            foreach (var backState in backStates)
                m_backStack.Push(backState);
            RequestState(firstState);
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

        /// <summary>
        /// Driven by another machine (a guest's game of an online room): the states requested here are ignored, the other
        /// machine's arrive through <see cref="SwitchToStateNow"/>.
        /// </summary>
        public bool IsRemotelyDriven { get; set; }

        /// <summary>
        /// The machine's last state has entered (a game that unloads): the states requested from then on, by a timer
        /// of a state left before or a message that arrives late, are ignored.
        /// </summary>
        public bool IsLocked { get; private set; }

        public void Lock()
        {
            IsLocked = true;
            m_requestedStates.Clear();
            Debug.Log($"[FM] {gameObject.name} - locked in {(CurrentState ? CurrentState.name : "no state")}.");
        }

        /// <summary>A state is about to enter, the previous one has left.</summary>
        public event Action<AFlowState> OnStateEntering;

        /// <summary>Switches at once to the state another machine entered, its states arriving in their order.</summary>
        public void SwitchToStateNow(AFlowState newState)
        {
            if (IsLocked)
            {
                Debug.Log($"[FM] {gameObject.name} - Remote state ignored, locked: {newState.gameObject}.");
                return;
            }
            m_requestedStates.Clear();
            m_backStack.Push(newState);
            Debug.Log($"[FM] {gameObject.name} - {newState.name} - About to switch state remotely.");
            SwitchToState(newState);
        }

        private Queue<SStateRequest> m_requestedStates = new();
        public void RequestState(AFlowState newState, bool ignoreBackStack = false)
        {
            if (IsLocked)
            {
                Debug.Log($"[FM] {gameObject.name} - Request state ignored, locked: {newState.gameObject}.");
                return;
            }
            if (IsRemotelyDriven)
            {
                Debug.Log($"[FM] {gameObject.name} - Request state ignored, driven remotely: {newState.gameObject}.");
                return;
            }
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
            if (IsRemotelyDriven || IsLocked)
                return;
            SStateRequest request = new SStateRequest()
            {
                RequestType = SStateRequest.ERequestType.Back
            };
            m_requestedStates.Enqueue(request);
            Debug.Log($"[FM] {gameObject.name} - Request back.");
        }

        /// <summary>
        /// Goes back to <paramref name="state"/>, however many states were reached since: going back from it leads
        /// where it did before. A state that isn't in the back stack is reached like a new one.
        /// </summary>
        public void RequestBackTo(AFlowState state)
        {
            if (IsRemotelyDriven || IsLocked)
                return;
            SStateRequest request = new SStateRequest()
            {
                State = state,
                RequestType = SStateRequest.ERequestType.BackTo
            };
            m_requestedStates.Enqueue(request);
            Debug.Log($"[FM] {gameObject.name} - Request back to : {state.gameObject}.");
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
                else if (request.RequestType == SStateRequest.ERequestType.BackTo)
                {
                    if (m_backStack.Contains(request.State))
                    {
                        while (m_backStack.Peek() != request.State)
                            m_backStack.Pop();
                    }
                    else
                        m_backStack.Push(request.State);
                    Debug.Log($"[FM] {gameObject.name} - {request.State.name} - About to switch state by going back to it.");
                    SwitchToState(request.State);
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
                OnStateEntering?.Invoke(CurrentState);
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