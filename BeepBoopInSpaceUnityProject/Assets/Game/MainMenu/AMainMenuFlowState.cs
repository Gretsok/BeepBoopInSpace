using Game.ArchitectureTools.FlowMachine;
using Game.Global;

namespace Game.MainMenu
{
    public class AMainMenuFlowState : AFlowState
    {
        public GlobalContext GlobalContext { get; private set; }
        public MainMenuContext MainMenuContext { get; private set; }
        
        public void Initialize(GlobalContext globalContext, MainMenuContext mainMenuContext)
        {
            GlobalContext = globalContext;
            MainMenuContext = mainMenuContext;
            HandleInitialization();
        }
        
        protected virtual void HandleInitialization()
        {}
    }
}