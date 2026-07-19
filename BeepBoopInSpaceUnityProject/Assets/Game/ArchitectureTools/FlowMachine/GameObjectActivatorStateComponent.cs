using System.Collections.Generic;
using UnityEngine;

namespace Game.ArchitectureTools.FlowMachine
{
    public class GameObjectActivatorStateComponent : AStateComponent
    {
        [SerializeField] private List<GameObject> m_gameObjectsToActivateDuringState = new();
        

        protected override void HandleEntered(AFlowState obj)
        {
            m_gameObjectsToActivateDuringState?.ForEach(go =>
            {
                if (go)
                    go.SetActive(true);
            });
        }

        protected override void HandleLeft(AFlowState obj)
        {
            m_gameObjectsToActivateDuringState?.ForEach(go =>
            {
                if (go)
                    go.SetActive(false);
            });
        }
    }
}
