using UnityEngine;

namespace Game.MainMenu.Online
{
    public class OnlineState : AMainMenuFlowState
    {
        [SerializeField]
        private OnlineScreen m_onlineScreen;

        protected override void HandleInitialization()
        {
            base.HandleInitialization();
            m_onlineScreen.Deactivate(true);
            m_onlineScreen.Initialize(MainMenuContext.MainMenuOrchestrator, GlobalContext.OnlineManager);
        }

        protected override void HandleEnter()
        {
            base.HandleEnter();
            m_onlineScreen.Activate();
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            m_onlineScreen.Deactivate();
        }
    }
}
