using UnityEngine;

namespace Game.MainMenu
{
    public class MainMenuOrchestrator : MonoBehaviour
    {
        [SerializeField]
        private HomeScreen m_homeScreen;
        [SerializeField]
        private PlayerJoiningScreen m_joiningScreen;

        private void Awake()
        {
            m_homeScreen.Deactivate(true);
            m_joiningScreen.Deactivate(true);
        }

        private void Start()
        {
            SwitchToHomeScreen();

            m_homeScreen.OnPlayRequested += HandlePlayRequestFromHomeScreen;
            m_joiningScreen.OnBack += HandleBackRequestFromJoiningScreen;
        }

        private void OnDestroy()
        {
            m_homeScreen.OnPlayRequested -= HandlePlayRequestFromHomeScreen;
            m_joiningScreen.OnBack -= HandleBackRequestFromJoiningScreen;        
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
            m_joiningScreen.Deactivate();
            m_homeScreen.Activate();
        }

        public void SwitchToJoiningScreen()
        {
            m_homeScreen.Deactivate();
            m_joiningScreen.Activate();
        }
    }
}