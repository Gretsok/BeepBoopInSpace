using Game.Gameplay.FlowMachine;
using Game.Gameplay.LoadingScreen;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Flows
{
    public class LoadingMenuState : AFlowState
    {
        protected override void HandleEnter()
        {
            base.HandleEnter();

            LoadingScreenManager.Instance?.ShowLoadingScreen();
            SceneManager.LoadSceneAsync("MainMenu");
        }
    }
}