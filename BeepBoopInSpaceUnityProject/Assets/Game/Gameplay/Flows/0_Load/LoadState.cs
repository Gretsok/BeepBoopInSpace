using Game.Gameplay.FlowMachine;
using UnityEngine;

namespace Game.Gameplay.Flows._0_Load
{
    public class LoadState : AFlowState
    {
        [SerializeField] 
        private AFlowState m_nextState;
        protected override void HandleEnter()
        {
            base.HandleEnter();
            
            GameplayContext.Instance.LoadingManager.Load(() => { RequestState(m_nextState); });
        }
    }
}