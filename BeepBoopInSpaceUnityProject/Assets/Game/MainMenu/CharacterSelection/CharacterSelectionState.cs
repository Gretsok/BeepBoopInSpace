using UnityEngine;

namespace Game.MainMenu.CharacterSelection
{
    public class CharacterSelectionState : AMainMenuFlowState
    {
        [SerializeField]
        private CharacterSelectionScreen m_characterSelectionScreen;

        protected override void HandleInitialization()
        {
            base.HandleInitialization();
            m_characterSelectionScreen.Deactivate(true);
            m_characterSelectionScreen.Initialize(MainMenuContext.ZoneManager, GlobalContext.PlayerManager, MainMenuContext.MainMenuOrchestrator);
        }

        protected override void HandleEnter()
        {
            base.HandleEnter();
            m_characterSelectionScreen.OnBack += HandleBackPerformed;
            m_characterSelectionScreen.Activate();
        }

        private void HandleBackPerformed()
        {
            MainMenuContext.MainMenuOrchestrator.SwitchToHomeScreen();
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            m_characterSelectionScreen.OnBack -= HandleBackPerformed;
            m_characterSelectionScreen.Deactivate();
        }
    }
}