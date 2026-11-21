using UnityEngine;

namespace Game.MainMenu.LocalOrOnline
{
    public class LocalOrOnlineState : AMainMenuFlowState
    {
        [SerializeField]
        private LocalOrOnlineScreen m_localOrOnlineScreen;

        protected override void HandleInitialization()
        {
            base.HandleInitialization();
            m_localOrOnlineScreen.Deactivate(true);
            m_localOrOnlineScreen.Initialize(MainMenuContext.MainMenuOrchestrator, GlobalContext.PlayerManager);
        }

        protected override void HandleEnter()
        {
            base.HandleEnter();
            m_localOrOnlineScreen.Activate();
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            m_localOrOnlineScreen.Deactivate();
        }
    }
}
