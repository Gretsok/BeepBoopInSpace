using Game.Gameplay.FlowMachine;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Flows
{
    public class LoadingMenuState : AFlowState
    {
        protected override void HandleEnter()
        {
            base.HandleEnter();

            SceneManager.LoadSceneAsync("MainMenu");
        }
    }
}