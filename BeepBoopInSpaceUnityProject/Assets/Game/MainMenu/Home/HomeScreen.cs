using System;
using Game.MainMenu.ZoneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.MainMenu
{
    public class HomeScreen : AMainMenuScreen
    {
        [SerializeField] 
        private CanvasGroup m_container;

        [SerializeField] 
        private Button m_playButton;
        [SerializeField] 
        private Button m_creditsButton;
        [SerializeField] 
        private Button m_quitButton;
        [SerializeField]
        private Button m_settingsButton;
        
        private ZoneManager m_zoneManager;
        private MainMenuOrchestrator m_mainMenuOrchestrator;

        protected override void HandleActivation()
        {
            gameObject.SetActive(true);
            
            m_playButton.onClick.AddListener(HandlePlayButtonClicked);
            m_creditsButton.onClick.AddListener(HandleCreditsButtonClicked);
            m_quitButton.onClick.AddListener(HandleQuitButtonClicked);
            m_settingsButton.onClick.AddListener(HandleSettingsButtonClicked);
            
            m_zoneManager.SwitchToEntranceCamera();
        }

        private void HandlePlayButtonClicked()
        {
            m_mainMenuOrchestrator.SwitchToJoiningScreen();
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

        protected override void HandleDeactivation()
        {
            gameObject.SetActive(false);
            
            m_playButton.onClick.RemoveListener(HandlePlayButtonClicked);
            m_creditsButton.onClick.RemoveListener(HandleCreditsButtonClicked);
            m_quitButton.onClick.RemoveListener(HandleQuitButtonClicked);
        }

        public void Initialize(MainMenuOrchestrator orchestrator, ZoneManager zoneManager)
        {
            m_zoneManager = zoneManager;
            m_mainMenuOrchestrator = orchestrator;
        }
    }
}