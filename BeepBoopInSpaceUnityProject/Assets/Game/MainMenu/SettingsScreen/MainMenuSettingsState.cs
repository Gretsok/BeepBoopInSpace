using UnityEngine;
using UnityEngine.InputSystem;

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
            
            GlobalContext.NavigationAuthorityManager.SetInputRegistrationsCallbacks(RegisterMainPlayerActions, UnregisterMainPlayerAction);
        }

        private void HandleBackRequested()
        {
            MainMenuContext.FlowMachine.RequestBack();
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            GlobalContext.NavigationAuthorityManager.UnsetInputRegistrationsCallbacks(RegisterMainPlayerActions, UnregisterMainPlayerAction);
            m_mainMenuSettingsScreen.OnBackRequested -= HandleBackRequested;
            m_mainMenuSettingsScreen.Deactivate();
        }
        
        private void RegisterMainPlayerActions(InputActionAsset actionAsset)
        {
            actionAsset.FindActionMap("UI").FindAction("Cancel").started += HandleCancelStartedByMainPlayer;
        }

        private void UnregisterMainPlayerAction(InputActionAsset actionAsset)
        {
            actionAsset.FindActionMap("UI").FindAction("Cancel").started -= HandleCancelStartedByMainPlayer;
        }

        private void HandleCancelStartedByMainPlayer(InputAction.CallbackContext obj)
        {
            HandleBackRequested();
        }
    }
}