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
            
            
            foreach (var pair in charactersManager.CharacterPlayerControllersAssociation)
            {
                pair.Key.Activate();
                pair.Value.ReferencesHolder.ActionsController.SetConfiguration(m_configurationsManager.CurrentConfiguration);
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
    }
}