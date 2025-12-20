using Game.Gameplay.CharactersManagement;
using Game.Gameplay.ConfigurationsManagement;
using Game.Gameplay.FlowMachine;
using Game.Gameplay.Timer;

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
            
            TimerManager.Instance.ResumeTimer();
            
            
            foreach (var pair in charactersManager.CharacterPlayerControllersAssociation)
            {
                pair.Key.Activate();
                pair.Value.ReferencesHolder.ActionsController.SetConfiguration(m_configurationsManager.CurrentConfiguration);
            }
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            
            m_configurationsManager.PauseRunning();
            
            var charactersManager = CharactersManager.Instance;

            TimerManager.Instance.PauseTimer();
            
            foreach (var pair in charactersManager.CharacterPlayerControllersAssociation)
            {
                pair.Key.Deactivate();
            }
        }
    }
}