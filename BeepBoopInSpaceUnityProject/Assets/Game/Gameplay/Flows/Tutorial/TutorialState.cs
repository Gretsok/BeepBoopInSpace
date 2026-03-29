using System.Collections;
using System.Linq;
using Game.ArchitectureTools.FlowMachine;
using Game.Global;
using UnityEngine;

namespace Game.Gameplay.Flows.Tutorial
{
    public class TutorialState : AFlowState
    {
        [SerializeField]
        private AFlowState m_nextState;

        [SerializeField]
        private float m_waitingDurationWhenEveryoneIsReady = 3f;

        private TutorialPanel m_tutorialPanel;

        protected override void HandleEnter()
        {
            base.HandleEnter();
            m_tutorialPanel = GameplayContext.Instance.UIManager.GetPanel<TutorialPanel>();
            var players = GlobalContext.Instance.PlayerManager.Players;
            m_tutorialPanel.OnPlayerStatusUpdated += HandlePlayerStatusUpdated;
            m_tutorialPanel.InflatePlayers(players);
            m_tutorialPanel.ResetWaitingBar();
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            m_tutorialPanel.OnPlayerStatusUpdated -= HandlePlayerStatusUpdated;
        }

        private Coroutine m_waitingCoroutine;
        private void HandlePlayerStatusUpdated(TutorialPanel arg1)
        {
            m_tutorialPanel.ResetWaitingBar();
            if (arg1.PlayerStatus.Values.ToList().TrueForAll(status => status))
            {
                if (m_waitingCoroutine != null)
                    StopCoroutine(m_waitingCoroutine);
                m_waitingCoroutine = StartCoroutine(WaitingBeforeStartingRoutine());
            }
        }

        private IEnumerator WaitingBeforeStartingRoutine()
        {
            m_tutorialPanel.PlayWaitingBarFor(m_waitingDurationWhenEveryoneIsReady);
            yield return new WaitForSeconds(m_waitingDurationWhenEveryoneIsReady);
            
            if (m_tutorialPanel.PlayerStatus.Values.ToList().TrueForAll(status => status))
            {
                RequestState(m_nextState);
            }
            m_waitingCoroutine = null;
        }
    }
}