using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.ConfigurationsManagement;

namespace Game.Gameplay.Flows.Gameplay
{
    public class GameplayState : AFlowState
    {
        private ConfigurationsManager m_configurationsManager;
        protected override void HandleEnter()
        {
            base.HandleEnter();
            var charactersManager = CharactersManager.Instance;
            m_configurationsManager = ConfigurationsManager.Instance;
            var controlMode = GameplayContext.Instance.ControlMode;

            foreach (var pair in charactersManager.CharacterPlayerControllersAssociation)
            {
                pair.Key.Activate();
                pair.Value.ReferencesHolder.ActionsController.SetConfiguration(m_configurationsManager.CurrentConfiguration, controlMode);
            }
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();

            var charactersManager = CharactersManager.Instance;

            foreach (var pair in charactersManager.CharacterPlayerControllersAssociation)
            {
                pair.Key.Deactivate();
            }
        }

        // The pause menu is played with the same buttons as the robots: they mustn't move behind it.
        protected override void HandleFlowMachinePaused()
        {
            base.HandleFlowMachinePaused();
            foreach (var pair in CharactersManager.Instance.CharacterPlayerControllersAssociation)
                pair.Key.Deactivate();
        }

        protected override void HandleFlowMachineUnpaused()
        {
            base.HandleFlowMachineUnpaused();
            foreach (var pair in CharactersManager.Instance.CharacterPlayerControllersAssociation)
                pair.Key.Activate();
        }
    }
}