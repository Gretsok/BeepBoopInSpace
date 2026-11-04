using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.ConfigurationsManagement;
using Game.Online.Gameplay;
using UnityEngine;

namespace Game.Gameplay.RoundsManagement
{
    public class RoundsManager : MonoBehaviour
    {
        [SerializeField]
        private FlowMachine m_flowMachine;  
        [SerializeField]
        private AFlowState m_newRoundState;

        public bool IsActive { get; private set; } = false;
        public int RoundIndex { get; private set; } = 0;

        public void SetActive(bool value)
        {
            IsActive = value;
        }
        
        public void ResetRoundIndex()
        {
            RoundIndex = 0;
        }
        
        /// <summary>Online guest: the host started a round.</summary>
        public void SetRoundIndex(int roundIndex)
        {
            RoundIndex = roundIndex;
        }

        public void TriggerNewRound()
        {
            ++RoundIndex;
            GameplaySync.NotifyRound(RoundIndex);
            ConfigurationsManager.Instance.PerformChange(true);
            m_flowMachine.RequestState(m_newRoundState);
        }
    }
}
