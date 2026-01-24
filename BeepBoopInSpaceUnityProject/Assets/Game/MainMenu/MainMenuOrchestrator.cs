using Game.Gameplay.LoadingScreen;
using Game.MainMenu.CameraManagement;
using Game.MainMenu.Credits;
using Game.MainMenu.PlayerJoining;
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

        private void Awake()
        {
            m_homeScreen.Deactivate(true);
            m_joiningScreen.Deactivate(true);
            m_creditsScreen.Deactivate(true);
        }

        public void Initialize(CameraManager cameraManager)
        {
            m_homeScreen.Initialize(cameraManager);
            m_joiningScreen.Initialize(cameraManager);
            m_creditsScreen.Initialize(cameraManager);
            
            SwitchToHomeScreen();

            m_homeScreen.OnPlayRequested += HandlePlayRequestFromHomeScreen;
            m_homeScreen.OnCreditsRequested += HandleCreditsRequested;
            m_joiningScreen.OnBack += HandleBackRequestFromJoiningScreen;
            m_creditsScreen.OnBackRequested += HandleBackRequestedFromCreditsScreen;
            
            LoadingScreenManager.Instance.HideLoadingScreen();
        }


        private void OnDestroy()
        {
            m_homeScreen.OnPlayRequested -= HandlePlayRequestFromHomeScreen;
            m_homeScreen.OnCreditsRequested -= HandleCreditsRequested;
            m_joiningScreen.OnBack -= HandleBackRequestFromJoiningScreen;       
            m_creditsScreen.OnBackRequested -= HandleBackRequestedFromCreditsScreen;
        }
        
        private void HandleCreditsRequested()
        {
            SwitchToCreditsScreen();
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

        private void HandlePlayRequestFromHomeScreen()
        {
            if (!m_homeScreen.IsActivated)
                return;
            
            SwitchToJoiningScreen();
        }

        public void SwitchToHomeScreen()
        {
            m_creditsScreen.Deactivate();
            m_joiningScreen.Deactivate();
            m_homeScreen.Activate();
        }

        public void SwitchToJoiningScreen()
        {
            m_creditsScreen.Deactivate();
            m_homeScreen.Deactivate();
            m_joiningScreen.Activate();
        }

        public void SwitchToCreditsScreen()
        {
            m_joiningScreen.Deactivate();
            m_homeScreen.Deactivate();
            m_creditsScreen.Activate();
        }
    }
}