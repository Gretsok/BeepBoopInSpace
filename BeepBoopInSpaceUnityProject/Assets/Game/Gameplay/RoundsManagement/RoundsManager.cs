using Game.ArchitectureTools.FlowMachine;
using UnityEngine;

namespace Game.Gameplay.RoundsManagement
{
    public class RoundsManager : MonoBehaviour
    {
        [SerializeField]
        private FlowMachine m_flowMachine;  
        [SerializeField]
        private AFlowState m_newRoundState;

        public void TriggerNewRound()
        {
            m_flowMachine.RequestState(m_newRoundState);
        }
    }
}
