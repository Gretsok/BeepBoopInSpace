using UnityEngine;

namespace Game.MainMenu.Hub
{
    public class HubState : AMainMenuFlowState
    {
        [SerializeField]
        private HubScreen m_hubScreen;

        protected override void HandleInitialization()
        {
            base.HandleInitialization();
            m_hubScreen.Deactivate(true);
            m_hubScreen.Initialize(MainMenuContext.GameModesLauncher, MainMenuContext.MainMenuOrchestrator);
        }

        protected override void HandleEnter()
        {
            base.HandleEnter();
            MainMenuContext.ZoneManager.SwitchToHubCamera();
            m_hubScreen.Activate();
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            m_hubScreen.Deactivate();
        }
    }
}
