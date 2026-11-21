using UnityEngine;

namespace Game.MainMenu.OnlineLobby
{
    public class OnlineLobbyState : AMainMenuFlowState
    {
        [SerializeField]
        private OnlineLobbyScreen m_onlineLobbyScreen;

        protected override void HandleInitialization()
        {
            base.HandleInitialization();
            m_onlineLobbyScreen.Deactivate(true);
            m_onlineLobbyScreen.Initialize(MainMenuContext.MainMenuOrchestrator, GlobalContext.OnlineManager);
        }

        protected override void HandleEnter()
        {
            base.HandleEnter();
            // Lost between joining it and this screen: the online screen says why.
            if (!GlobalContext.OnlineManager.IsInRoom)
            {
                MainMenuContext.MainMenuOrchestrator.BackToOnlineScreen();
                return;
            }
            m_onlineLobbyScreen.Activate();
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            m_onlineLobbyScreen.Deactivate();
        }
    }
}
