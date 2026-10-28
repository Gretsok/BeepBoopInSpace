using Game.Global.SFXManagement;
using UnityEngine;

namespace Game.MainMenu.Home
{
    public class HomeScreen : AMainMenuScreen
    {
        [SerializeField]
        private AudioPlayer m_playAudioPlayer;

        private MainMenuOrchestrator m_mainMenuOrchestrator;

        public void Initialize(MainMenuOrchestrator orchestrator)
        {
            m_mainMenuOrchestrator = orchestrator;
        }

        protected override void HandleActivation()
        {
            DefaultFocus = BindButton("play-button", HandlePlayButtonClicked, m_playAudioPlayer);
            BindButton("credits-button", HandleCreditsButtonClicked, m_playAudioPlayer);
            BindButton("settings-button", HandleSettingsButtonClicked);
            BindButton("quit-button", HandleQuitButtonClicked);

        }

        private void HandlePlayButtonClicked()
        {
            m_mainMenuOrchestrator.SwitchToCharacterSelectionScreen();
        }

        private void HandleCreditsButtonClicked()
        {
            m_mainMenuOrchestrator.SwitchToCreditsScreen();
        }

        private void HandleQuitButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        private void HandleSettingsButtonClicked()
        {
            m_mainMenuOrchestrator.SwitchToSettingsScreen();
        }
    }
}
