using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Gameplay.Flows.Results;
using Game.Gameplay.Levels._0_Core;
using Game.Gameplay.LoadingScreen;
using Game.Tournament.Config;
using Game.Tournament.Results;
using Game.Tournament.Results.ResultsScene;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Game.Tournament.Flow
{
    public class TournamentFlowManager : MonoBehaviour
    {
        [SerializeField]
        private AssetReference m_tournamentCloseScene;

        [SerializeField]
        private AssetReference m_tournamentResultsScene;
        
        private TournamentResultsManager m_resultsManager;
        private TournamentConfigDataAsset m_configDataAsset;
        
        private CurrentLevelInfoManager m_currentLevelInfoManager;

        public void InjectDependencies(TournamentResultsManager resultsManager, TournamentConfigDataAsset configDataAsset)
        {
            m_resultsManager = resultsManager;
            m_configDataAsset = configDataAsset;
            
            m_currentLevelInfoManager = CurrentLevelInfoManager.Instance;
        }
        
        private readonly List<LevelDataAsset> m_levelsDone = new ();
        
        public void StartTournament()
        {
            var levelDataAsset = m_configDataAsset.LevelDataAssets[0];
            m_levelsDone.Add(levelDataAsset);

            m_currentLevelInfoManager.SetCurrentLevelDataAsset(levelDataAsset);
            var op = Addressables.LoadSceneAsync(levelDataAsset.GameModeScene, LoadSceneMode.Single);
            op.Completed += HandleOnGameStartedLogic;
        }

        private void HandleCurrentLevelDataAssetChanged(CurrentLevelInfoManager arg1, LevelDataAsset arg2)
        {
            if (arg2)
                return;
            m_currentLevelInfoManager.OnCurrentLevelDataAssetChanged -= HandleCurrentLevelDataAssetChanged;

            // Handling end of game
            StartCoroutine(HandleEndOfGame());
        }

        private IEnumerator HandleEndOfGame()
        {
            var loadingScreenManager = LoadingScreenManager.Instance;
            
            loadingScreenManager.ShowLoadingScreen();
            var op = Addressables.LoadSceneAsync(m_tournamentResultsScene, LoadSceneMode.Single);
            yield return new WaitUntil(() => op.IsDone);
            
            TournamentResultsSceneContext.RegisterPostInitializationCallback(resultsSceneContext =>
            {
                resultsSceneContext.ResultsCanvas.SetUp(HandleEndOfResultTournamentScreenClosed);
                loadingScreenManager.HideLoadingScreen();
            });
        }

        private void HandleEndOfResultTournamentScreenClosed()
        {
            var loadingScreenManager = LoadingScreenManager.Instance;
            loadingScreenManager.ShowLoadingScreen();
            if (m_resultsManager.IsGameComplete())
            {
                // Loading end Tournament scene
                var op = Addressables.LoadSceneAsync(m_tournamentCloseScene, LoadSceneMode.Single);
            }
            else
            {
                LoadNextLevel();
            }
        }

        private void LoadNextLevel()
        {
            List<LevelDataAsset> levels = m_configDataAsset.LevelDataAssets.Except(m_levelsDone).ToList();

            if (levels.Count == 0)
            {
                m_levelsDone.Clear();
                levels = m_configDataAsset.LevelDataAssets.ToList();
            }
            
            var levelDataAsset = levels[Random.Range(0, levels.Count)];
            m_levelsDone.Add(levelDataAsset);
            
            m_currentLevelInfoManager.SetCurrentLevelDataAsset(levelDataAsset);
            var op = Addressables.LoadSceneAsync(levelDataAsset.GameModeScene, LoadSceneMode.Single);
            op.Completed += HandleOnGameStartedLogic;
        }

        private void HandleOnGameStartedLogic(AsyncOperationHandle<SceneInstance> obj)
        {
            m_currentLevelInfoManager.OnCurrentLevelDataAssetChanged += HandleCurrentLevelDataAssetChanged;
            
            ResultsManager.RegisterPostInitializationCallback(resultsManager =>
            {
                resultsManager.OnGameResultProcessed += (manager, gameResult) =>
                {
                    m_resultsManager.RegisterGameResult(gameResult);
                };
            });
        }
    }
}
