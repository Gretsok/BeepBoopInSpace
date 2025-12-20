using Game.Gameplay.FlowMachine;
using Game.Gameplay.Levels._0_Core;
using Game.Gameplay.LoadingScreen;

namespace Game.Gameplay.Flows.End
{
    public class EndState : AFlowState
    {
        protected override void HandleEnter()
        {
            base.HandleEnter();

            var currentLevelInfoManager = CurrentLevelInfoManager.Instance;
            
            LoadingScreenManager.Instance?.ShowLoadingScreen();
            
            GameplayContext.Instance.LoadingManager.Unload(() =>
            {
                if (currentLevelInfoManager)
                {
                    currentLevelInfoManager.SetCurrentLevelDataAsset(null);
                }
            });
        }
    }
}