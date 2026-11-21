using Game.Global.SFXManagement;
using Game.MainMenu.ZoneManagement;
using UnityEngine;

namespace Game.MainMenu
{
    public class HomeScreen : AMainMenuScreen
    {
        [SerializeField]
        private AudioPlayer m_playAudioPlayer;

        private ZoneManager m_zoneManager;
        private MainMenuOrchestrator m_mainMenuOrchestrator;

        public void Initialize(MainMenuOrchestrator orchestrator, ZoneManager zoneManager)
        {
            m_zoneManager = zoneManager;
            m_mainMenuOrchestrator = orchestrator;
        }

        protected override void HandleActivation()
        {
            DefaultFocus = BindButton("play-button", HandlePlayButtonClicked, m_playAudioPlayer);
            BindButton("credits-button", HandleCreditsButtonClicked, m_playAudioPlayer);
            BindButton("settings-button", HandleSettingsButtonClicked);
            BindButton("quit-button", HandleQuitButtonClicked);

            m_zoneManager.SwitchToEntranceCamera();
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
