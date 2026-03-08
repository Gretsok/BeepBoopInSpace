using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.LoadingScreen;
using Game.Tournament;
using Game.Training;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Flows.End
{
    public class EndState : AFlowState
    {
        [SerializeField]
        private AssetReference m_mainMenuScene;
        
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

            if (!TournamentContext.Instance)
            {
                Addressables.LoadSceneAsync(m_mainMenuScene);
            }
        }
    }
}