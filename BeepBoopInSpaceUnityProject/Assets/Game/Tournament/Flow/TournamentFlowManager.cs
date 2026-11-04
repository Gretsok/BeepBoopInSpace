using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Gameplay.Flows.Results;
using Game.Gameplay.Levels._0_Core;
using Game.LoadingScreen;
using Game.Online;
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

        public void InjectDependencies(TournamentResultsManager resultsManager, TournamentConfigDataAsset configDataAsset, CurrentLevelInfoManager currentLevelInfoManager)
        {
            m_resultsManager = resultsManager;
            m_configDataAsset = configDataAsset;
            m_currentLevelInfoManager = currentLevelInfoManager;
        }
        
        private readonly List<LevelDataAsset> m_levelsDone = new ();
        
        // Online guest: the level the host chose after the results (-1: the tournament is over), and whether the results
        // wait for it.
        private int? m_nextLevelFromHost;
        private bool m_isWaitingForHost;
        // From the loading of a game's scene until it's loaded: a room lost meanwhile is handled by the game itself.
        private bool m_isLoadingLevel;

        public void StartTournament()
        {
            var onlineManager = OnlineManager.Instance;
            if (Netplay.IsOnline)
            {
                onlineManager.OnRoomLost += HandleRoomLost;
                if (Netplay.IsGuest)
                    onlineManager.OnTournamentNextGame += HandleHostNextGame;
            }

            var levelDataAsset = m_configDataAsset.LevelDataAssets[0];
            m_levelsDone.Add(levelDataAsset);

            m_currentLevelInfoManager.SetCurrentLevelDataAsset(levelDataAsset);
            m_isLoadingLevel = true;
            var op = Addressables.LoadSceneAsync(levelDataAsset.GameModeScene, LoadSceneMode.Single);
            op.Completed += HandleOnGameStartedLogic;
        }

        public void Abandon()
        {
            StopListeningToTheRoom();
            // The game being unloaded mustn't lead to the tournament's results.
            m_currentLevelInfoManager.OnCurrentLevelDataAssetChanged -= HandleCurrentLevelDataAssetChanged;
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
            Debug.Log("[Tournament] Game over: its results.");

            loadingScreenManager.ShowLoadingScreen();
            var op = Addressables.LoadSceneAsync(m_tournamentResultsScene, LoadSceneMode.Single);
            yield return new WaitUntil(() => op.IsDone);
            
            TournamentResultsSceneContext.RegisterPostInitializationCallback(resultsSceneContext =>
            {
                // Online guest whose room was lost as the game ended: no host to wait for.
                if (Netplay.IsGuest && !Netplay.IsOnline)
                {
                    Debug.Log("[Tournament] Room lost between two games: back to the main menu.");
                    CloseTournament();
                    return;
                }
                resultsSceneContext.ResultsCanvas.SetUp(HandleEndOfResultTournamentScreenClosed);
                loadingScreenManager.HideLoadingScreen();
                // Online guest: the results stay until the host leaves them.
                if (Netplay.IsGuest)
                {
                    Debug.Log($"[Tournament] Results shown, the host's choice {(m_nextLevelFromHost.HasValue ? "already here" : "awaited")}.");
                    m_isWaitingForHost = true;
                    if (m_nextLevelFromHost.HasValue)
                        ContinueWithHost();
                }
            });
        }

        private void HandleEndOfResultTournamentScreenClosed()
        {
            var loadingScreenManager = LoadingScreenManager.Instance;
            loadingScreenManager.ShowLoadingScreen();
            if (m_resultsManager.IsGameComplete() || HasTooFewPlayersLeft)
            {
                CloseTournament();
            }
            else
            {
                LoadNextLevel();
            }
        }

        // Online, the others left the room: a tournament needs two players, as offline.
        private static bool HasTooFewPlayersLeft
        {
            get
            {
                if (!Netplay.IsHost || OnlineManager.Instance.Roster.Count >= OnlineManager.k_MinPlayers)
                    return false;
                Debug.Log("[Tournament] Fewer than 2 players left in the room: the tournament ends.");
                return true;
            }
        }

        private void CloseTournament()
        {
            if (Netplay.IsHost)
                OnlineManager.Instance.SendTournamentNextGame(-1);
            StopListeningToTheRoom();
            // Loading end Tournament scene
            Addressables.LoadSceneAsync(m_tournamentCloseScene, LoadSceneMode.Single);
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
            // Online, the guests play the level the host drew.
            if (Netplay.IsHost)
                OnlineManager.Instance.SendTournamentNextGame(m_configDataAsset.LevelDataAssets.IndexOf(levelDataAsset));
            LoadLevel(levelDataAsset);
        }

        private void HandleHostNextGame(int levelIndex)
        {
            Debug.Log($"[Tournament] The host chose the level {levelIndex}.");
            m_nextLevelFromHost = levelIndex;
            if (m_isWaitingForHost)
                ContinueWithHost();
        }

        private void ContinueWithHost()
        {
            m_isWaitingForHost = false;
            var levelIndex = m_nextLevelFromHost ?? -1;
            m_nextLevelFromHost = null;
            LoadingScreenManager.Instance.ShowLoadingScreen();
            if (levelIndex < 0 || levelIndex >= m_configDataAsset.LevelDataAssets.Count)
            {
                CloseTournament();
                return;
            }
            var levelDataAsset = m_configDataAsset.LevelDataAssets[levelIndex];
            if (m_levelsDone.Count >= m_configDataAsset.LevelDataAssets.Count)
                m_levelsDone.Clear();
            LoadLevel(levelDataAsset);
        }

        // The room is lost between two games (during one, the game goes back to the main menu itself).
        private void HandleRoomLost(string reason)
        {
            if (Game.Gameplay.GameplayContext.Instance || m_isLoadingLevel)
                return;
            Debug.Log("[Tournament] Room lost between two games: back to the main menu.");
            m_isWaitingForHost = false;
            LoadingScreenManager.Instance.ShowLoadingScreen();
            CloseTournament();
        }

        private void StopListeningToTheRoom()
        {
            var onlineManager = OnlineManager.Instance;
            if (!onlineManager)
                return;
            onlineManager.OnRoomLost -= HandleRoomLost;
            onlineManager.OnTournamentNextGame -= HandleHostNextGame;
        }

        private void OnDestroy()
        {
            StopListeningToTheRoom();
        }

        private void LoadLevel(LevelDataAsset levelDataAsset)
        {
            Debug.Log($"[Tournament] Next game: {levelDataAsset.name}.");
            m_levelsDone.Add(levelDataAsset);
            
            m_currentLevelInfoManager.SetCurrentLevelDataAsset(levelDataAsset);
            m_isLoadingLevel = true;
            var op = Addressables.LoadSceneAsync(levelDataAsset.GameModeScene, LoadSceneMode.Single);
            op.Completed += HandleOnGameStartedLogic;
        }

        private void HandleOnGameStartedLogic(AsyncOperationHandle<SceneInstance> obj)
        {
            m_isLoadingLevel = false;
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
