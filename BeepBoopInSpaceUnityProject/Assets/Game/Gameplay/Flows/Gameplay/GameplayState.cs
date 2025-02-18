using Game.Gameplay.CharactersManagement;
using Game.Gameplay.FlowMachine;

namespace Game.Gameplay.Flows.Gameplay
{
    public class GameplayState : AFlowState
    {
        protected override void HandleEnter()
        {
            base.HandleEnter();
            var charactersManager = CharactersManager.Instance;
            
            foreach (var pair in charactersManager.CharacterPlayerControllers)
            {
                pair.Key.Activate();
            }
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            var charactersManager = CharactersManager.Instance;

            foreach (var pair in charactersManager.CharacterPlayerControllers)
            {
                pair.Key.Activate();
            }
        }
    }
}