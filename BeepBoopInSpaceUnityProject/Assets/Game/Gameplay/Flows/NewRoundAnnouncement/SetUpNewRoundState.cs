using Game.Gameplay.FlowMachine;
using UnityEngine;

namespace Game.Gameplay.Flows.NewRoundAnnouncement
{
    public class SetUpNewRoundState : AFlowState
    {
        [SerializeField]
        private AFlowState m_nextState;
        
        protected override void HandleEnter()
        {
            base.HandleEnter();
            
            
            
            RequestState(m_nextState);
        }
    }
}