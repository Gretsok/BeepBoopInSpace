using UnityEngine;

namespace Game.MainMenu.Home
{
    public class HomeState : AMainMenuFlowState
    {
        [SerializeField]
        private HomeScreen m_homeScreen;

        protected override void HandleInitialization()
        {
            base.HandleInitialization();
            
            m_homeScreen.Deactivate(true);
            m_homeScreen.Initialize(MainMenuContext.MainMenuOrchestrator, MainMenuContext.ZoneManager);
        }

        protected override void HandleEnter()
        {
            base.HandleEnter();
            m_homeScreen.Activate();
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            m_homeScreen.Deactivate();
        }
    }
}
