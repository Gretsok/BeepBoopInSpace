using Game.Gameplay.FlowMachine;
using UnityEngine;

namespace Game.Gameplay.Timer
{
    public class RequestStateOnTimerCompletion : MonoBehaviour
    {
        private TimerManager m_timerManager;
        [SerializeField] 
        private AFlowState m_stateToRequest;
        private void Awake()
        {
            m_timerManager = GetComponent<TimerManager>();
            m_timerManager.OnTimerCompleted += HandleTimerCompleted;
        }

        private void HandleTimerCompleted(TimerManager obj)
        {
            FlowMachine.FlowMachine.Instance.RequestState(m_stateToRequest);
        }
    }
}