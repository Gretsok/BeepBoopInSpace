using UnityEngine;

namespace Game.ArchitectureTools.FlowMachine
{
    [RequireComponent(typeof(AFlowState))]
    public abstract class AStateComponent : MonoBehaviour
    {
        private AFlowState m_flowState;
        
        private void Awake()
        {
            m_flowState = GetComponent<AFlowState>();
            m_flowState.OnEntered += HandleEntered;
            m_flowState.OnLeft += HandleLeft;
            m_flowState.OnPaused += HandlePaused;
            m_flowState.OnUnpaused += HandleUnpaused;
        }

        protected virtual void HandlePaused(AFlowState obj) { }

        protected virtual void HandleUnpaused(AFlowState obj) { }
        

        protected virtual void HandleEntered(AFlowState obj)
        {
        }

        protected virtual void HandleLeft(AFlowState obj)
        {
        }
    }
}
