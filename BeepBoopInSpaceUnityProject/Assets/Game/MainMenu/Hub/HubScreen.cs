using Game.Global.SFXManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.MainMenu.Hub
{
    public class HubScreen : AMainMenuScreen
    {
        [SerializeField]
        private AudioPlayer m_confirmAudioPlayer;

        private Button m_tournamentButton;
        private Button m_trainingButton;

        private GameModesLauncher m_gameModesLauncher;
        private MainMenuOrchestrator m_orchestrator;

        public void Initialize(GameModesLauncher gameModesLauncher, MainMenuOrchestrator mainMenuOrchestrator)
        {
            m_gameModesLauncher = gameModesLauncher;
            m_orchestrator = mainMenuOrchestrator;
        }

        protected override void HandleActivation()
        {
            m_tournamentButton = BindButton("tournament-button", HandleTournamentButtonClicked, m_confirmAudioPlayer);
            m_trainingButton = BindButton("training-button", HandleTrainingButtonClicked, m_confirmAudioPlayer);
            BindButton("settings-button", HandleSettingsButtonClicked, m_confirmAudioPlayer);
            BindButton("character-selection-button", HandleCharacterSelectionButtonClicked, m_confirmAudioPlayer);
            BindButton("quit-button", HandleQuitGameButtonClicked);

            DefaultFocus = m_tournamentButton;
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
