using UnityEngine;

namespace Game.MainMenu.Credits
{
    public class CreditsState : AMainMenuFlowState
    {
        [SerializeField]
        private CreditsScreen m_creditsScreen;

        protected override void HandleInitialization()
        {
            base.HandleInitialization();
            m_creditsScreen.Deactivate(true);
            m_creditsScreen.Initialize(MainMenuContext.ZoneManager);
        }

        protected override void HandleEnter()
        {
            base.HandleEnter();
            m_creditsScreen.OnBackRequested += HandleBackPerformed;
            m_creditsScreen.Activate();
        }

        private void HandleBackPerformed()
        {
            MainMenuContext.MainMenuOrchestrator.SwitchToHomeScreen();
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            m_creditsScreen.OnBackRequested -= HandleBackPerformed;
            m_creditsScreen.Deactivate();
        }
    }
}
