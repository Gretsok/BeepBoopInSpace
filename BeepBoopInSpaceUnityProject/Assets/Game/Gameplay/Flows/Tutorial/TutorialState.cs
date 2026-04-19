using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.GameModes.GameplayModifiers.Meteorites;
using Game.Gameplay.Levels._0_Core;
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

        private readonly List<TutorialPlayerController> m_tutorialPlayerControllers = new();

        protected override void HandleEnter()
        {
            base.HandleEnter();
            var gameplayContext = GameplayContext.Instance;
            m_tutorialPanel = gameplayContext.UIManager.GetPanel<TutorialPanel>();
            var players = GlobalContext.Instance.PlayerManager.Players;
            
            m_tutorialPlayerControllers.Clear();
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                var tutorialPlayerController = gameObject.AddComponent<TutorialPlayerController>();
                tutorialPlayerController.Initialize(player);
                m_tutorialPlayerControllers.Add(tutorialPlayerController);

                tutorialPlayerController.OnPlayerStatusToggleRequested += HandlePlayerStatusToggleRequested;
            }
            
            m_tutorialPanel.OnPlayerStatusUpdated += HandlePlayerStatusUpdated;
            m_tutorialPanel.InflatePlayers(players);
            m_tutorialPanel.InflateGameData(
                gameplayContext.ObjectiveManager.ObjectiveLabelKey, 
                gameplayContext.SpecialActionPrefab.SpecialActionLabelKey, 
                gameplayContext.HasGameplayModifier<MeteoritesModifier>());
            m_tutorialPanel.ResetWaitingBar();
        }

        private void HandlePlayerStatusToggleRequested(TutorialPlayerController obj)
        {
            m_tutorialPanel.TogglePlayerStatus(obj.Player);
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();

            for (int i = m_tutorialPlayerControllers.Count - 1; i >= 0; i--)
            {
                var player = m_tutorialPlayerControllers[i];
                Destroy(player);
            }
            
            m_tutorialPlayerControllers.Clear();
            
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