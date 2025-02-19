using Game.Gameplay.CharactersManagement;
using Game.Gameplay.ConfigurationsManagement;
using Game.Gameplay.FlowMachine;

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
            
            m_configurationsManager.ResumeRunning();
            
            
            foreach (var pair in charactersManager.CharacterPlayerControllers)
            {
                pair.Key.Activate();
                pair.Value.SetConfiguration(m_configurationsManager.CurrentConfiguration);
            }
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            
            m_configurationsManager.PauseRunning();
            
            var charactersManager = CharactersManager.Instance;

            foreach (var pair in charactersManager.CharacterPlayerControllers)
            {
                pair.Key.Deactivate();
            }
        }
    }
}