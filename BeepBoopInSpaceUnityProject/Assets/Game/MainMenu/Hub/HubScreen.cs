using UnityEngine;
using UnityEngine.UI;

namespace Game.MainMenu.Hub
{
    public class HubScreen : AMainMenuScreen
    {
        [SerializeField]
        private Button m_tournamentButton;

        [SerializeField]
        private Button m_trainingButton;

        [SerializeField]
        private Button m_settingsButton;

        [SerializeField]
        private Button m_characterSelectionButton;

        [SerializeField]
        private Button m_quitGameButton;

        private GameModesLauncher m_gameModesLauncher;
        private MainMenuOrchestrator m_orchestrator;
        
        public void Initialize(GameModesLauncher gameModesLauncher, MainMenuOrchestrator mainMenuOrchestrator)
        {
            m_gameModesLauncher = gameModesLauncher;
            m_orchestrator = mainMenuOrchestrator;
        }

        protected override void HandleActivation()
        {
            base.HandleActivation();
            m_tournamentButton.onClick.RemoveListener(HandleTournamentButtonClicked);
            m_trainingButton.onClick.RemoveListener(HandleTrainingButtonClicked);
            m_settingsButton.onClick.RemoveListener(HandleSettingsButtonClicked);
            m_characterSelectionButton.onClick.RemoveListener(HandleCharacterSelectionButtonClicked);
            m_quitGameButton.onClick.RemoveListener(HandleQuitGameButtonClicked);
            
            
            m_tournamentButton.onClick.AddListener(HandleTournamentButtonClicked);
            m_trainingButton.onClick.AddListener(HandleTrainingButtonClicked);
            m_settingsButton.onClick.AddListener(HandleSettingsButtonClicked);
            m_characterSelectionButton.onClick.AddListener(HandleCharacterSelectionButtonClicked);
            m_quitGameButton.onClick.AddListener(HandleQuitGameButtonClicked);
        }

        protected override void HandleDeactivation()
        {
            base.HandleDeactivation();
            
            m_tournamentButton.onClick.RemoveListener(HandleTournamentButtonClicked);
            m_trainingButton.onClick.RemoveListener(HandleTrainingButtonClicked);
            m_settingsButton.onClick.RemoveListener(HandleSettingsButtonClicked);
            m_characterSelectionButton.onClick.RemoveListener(HandleCharacterSelectionButtonClicked);
            m_quitGameButton.onClick.RemoveListener(HandleQuitGameButtonClicked);
        }

        private void HandleTournamentButtonClicked()
        {
            m_gameModesLauncher.LaunchTournament();
        }

        private void HandleTrainingButtonClicked()
        {
            m_orchestrator.SwitchToLevelSelectionScreen();
        }

        private void HandleSettingsButtonClicked()
        {
            m_orchestrator.SwitchToSettingsScreen();
        }

        private void HandleCharacterSelectionButtonClicked()
        {
            m_orchestrator.SwitchToCharacterSelectionScreen();
        }

        private void HandleQuitGameButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
