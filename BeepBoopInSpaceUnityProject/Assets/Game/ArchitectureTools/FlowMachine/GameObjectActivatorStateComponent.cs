using System.Collections.Generic;
using UnityEngine;

namespace Game.ArchitectureTools.FlowMachine
{
    [RequireComponent(typeof(AFlowState))]
    public class GameObjectActivatorStateComponent : MonoBehaviour
    {
        private AFlowState m_flowState;

        [SerializeField] private List<GameObject> m_gameObjectsToActivateDuringState = new();
        
        private void Awake()
        {
            m_flowState = GetComponent<AFlowState>();
            m_flowState.OnEntered += HandleEntered;
            m_flowState.OnLeft += HandleLeft;
            m_flowState.OnPaused += HandlePaused;
            m_flowState.OnUnpaused += HandleUnpaused;
        }

        private void HandlePaused(AFlowState obj) { }

        private void HandleUnpaused(AFlowState obj) { }

        private void HandleEntered(AFlowState obj)
        {
            m_gameObjectsToActivateDuringState?.ForEach(go =>
            {
                if (go)
                    go.SetActive(true);
            });
        }

        private void HandleLeft(AFlowState obj)
        {
            m_gameObjectsToActivateDuringState?.ForEach(go =>
            {
                if (go)
                    go.SetActive(false);
            });
        }
    }
}
