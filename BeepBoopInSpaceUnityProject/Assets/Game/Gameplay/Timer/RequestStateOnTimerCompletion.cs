using Game.Gameplay.Flows.Finish;
using UnityEngine;

namespace Game.Gameplay.Timer
{
    public class RequestStateOnTimerCompletion : MonoBehaviour
    {
        private TimerManager m_timerManager;

        private void Awake()
        {
            m_timerManager = GetComponent<TimerManager>();
            m_timerManager.OnTimerCompleted += HandleTimerCompleted;
        }

        private void HandleTimerCompleted(TimerManager obj)
        {
            FlowMachine.FlowMachine.Instance.RequestState(FinishStateGrabber.Instance.FinishState);
        }
    }
}