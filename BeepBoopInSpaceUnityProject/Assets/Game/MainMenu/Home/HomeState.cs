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
            m_homeScreen.Initialize(MainMenuContext.MainMenuOrchestrator);
        }

        protected override void HandleEnter()
        {
            base.HandleEnter();
            // Back on the title screen, the players' session is over: they join again in the character selection.
            GlobalContext.PlayerManager.RemoveAllPlayers();
            m_homeScreen.Activate();
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            m_homeScreen.Deactivate();
        }
    }
}
