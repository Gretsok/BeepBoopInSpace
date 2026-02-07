using Game.Gameplay.LoadingScreen;
using Game.Global.PlayerManagement;
using Game.Global.Save;
using Game.Global.Settings;
using Game.MainMenu.Credits;
using Game.MainMenu.PlayerJoining;
using Game.MainMenu.SettingsScreen;
using Game.MainMenu.ZoneManagement;
using Game.PlayerManagement;
using UnityEngine;

namespace Game.MainMenu
{
    public class MainMenuOrchestrator : MonoBehaviour
    {
        [SerializeField]
        private HomeScreen m_homeScreen;
        [SerializeField]
        private PlayerJoiningScreen m_joiningScreen;
        [SerializeField]
        private CreditsScreen m_creditsScreen;
        [SerializeField]
        private MainMenuSettingsScreen m_settingsScreen;

        private void Awake()
        {
            m_homeScreen.Deactivate(true);
            m_joiningScreen.Deactivate(true);
            m_creditsScreen.Deactivate(true);
            m_settingsScreen.Deactivate(true);
        }

        public void Initialize(ZoneManager zoneManager, PlayerManager playerManager, SettingsManager settingsManager, SaveManager saveManager)
        {
            m_homeScreen.Initialize(this, zoneManager);
            m_joiningScreen.Initialize(zoneManager, playerManager);
            m_creditsScreen.Initialize(zoneManager);
            m_settingsScreen.Initialize(zoneManager, settingsManager, saveManager);
            
            SwitchToHomeScreen();

            m_joiningScreen.OnBack += HandleBackRequestFromJoiningScreen;
            m_creditsScreen.OnBackRequested += HandleBackRequestedFromCreditsScreen;
            m_settingsScreen.OnBackRequested += HandleBackRequestFromSettingsScreen;
            
            LoadingScreenManager.Instance.HideLoadingScreen();
        }


        private void OnDestroy()
        {
            m_joiningScreen.OnBack -= HandleBackRequestFromJoiningScreen;       
            m_creditsScreen.OnBackRequested -= HandleBackRequestedFromCreditsScreen;
            m_settingsScreen.OnBackRequested -= HandleBackRequestFromSettingsScreen;
        }
        
        private void HandleBackRequestedFromCreditsScreen()
        {
            if (!m_creditsScreen.IsActivated)
                return;
            
            SwitchToHomeScreen();
        }

        
        private void HandleBackRequestFromJoiningScreen()
        {
            if (!m_joiningScreen.IsActivated)
                return;
            
            SwitchToHomeScreen();
        }

        private void HandleBackRequestFromSettingsScreen()
        {
            if (!m_settingsScreen.IsActivated)
                return;
            
            SwitchToHomeScreen();
        }

        public void SwitchToHomeScreen()
        {
            m_creditsScreen.Deactivate();
            m_joiningScreen.Deactivate();
            m_settingsScreen.Deactivate();
            m_homeScreen.Activate();
        }

        public void SwitchToJoiningScreen()
        {
            m_creditsScreen.Deactivate();
            m_homeScreen.Deactivate();
            m_settingsScreen.Deactivate();
            m_joiningScreen.Activate();
        }

        public void SwitchToCreditsScreen()
        {
            m_joiningScreen.Deactivate();
            m_homeScreen.Deactivate();
            m_settingsScreen.Deactivate();
            m_creditsScreen.Activate();
        }

        public void SwitchToSettingsScreen()
        {
            m_joiningScreen.Deactivate();
            m_homeScreen.Deactivate();
            m_creditsScreen.Deactivate();
            m_settingsScreen.Activate();
            
        }
    }
}