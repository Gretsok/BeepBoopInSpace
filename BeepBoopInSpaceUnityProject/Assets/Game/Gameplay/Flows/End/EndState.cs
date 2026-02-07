using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.LoadingScreen;

namespace Game.Gameplay.Flows.End
{
    public class EndState : AFlowState
    {
        protected override void HandleEnter()
        {
            base.HandleEnter();

            var gameplayContext = GameplayContext.Instance;
            var currentLevelInfoManager = gameplayContext.CurrentLevelInfoManager;
            
            LoadingScreenManager.Instance?.ShowLoadingScreen();
            
            gameplayContext.LoadingManager.Unload(() =>
            {
                if (currentLevelInfoManager)
                {
                    currentLevelInfoManager.SetCurrentLevelDataAsset(null);
                }
            });
        }
    }
}