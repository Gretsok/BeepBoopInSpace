using UnityEngine;

namespace Game.MainMenu.SettingsScreen
{
    public class MainMenuSettingsState : AMainMenuFlowState
    {
        [SerializeField]
        private MainMenuSettingsScreen m_mainMenuSettingsScreen;

        protected override void HandleInitialization()
        {
            base.HandleInitialization();
            m_mainMenuSettingsScreen.Deactivate(true);
            m_mainMenuSettingsScreen.Initialize(MainMenuContext.ZoneManager, GlobalContext.SettingsManager, GlobalContext.SaveManager);
        }
        
        protected override void HandleEnter()
        {
            base.HandleEnter();
            m_mainMenuSettingsScreen.OnBackRequested += HandleBackRequested;
            m_mainMenuSettingsScreen.Activate();
        }

        private void HandleBackRequested()
        {
            MainMenuContext.MainMenuOrchestrator.SwitchToHomeScreen();
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            m_mainMenuSettingsScreen.OnBackRequested -= HandleBackRequested;
            m_mainMenuSettingsScreen.Deactivate();
        }
    }
}