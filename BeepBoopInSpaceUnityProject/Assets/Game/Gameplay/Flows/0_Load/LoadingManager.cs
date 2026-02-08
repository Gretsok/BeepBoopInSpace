using System;
using System.Collections;
using System.Collections.Generic;
using Game.Characters;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core;
using Game.Gameplay.GlobalGameplayData;
using Game.Gameplay.GridSystem;
using Game.Gameplay.Levels._0_Core;
#if UNITY_EDITOR
using Game.Gameplay.Levels._0_Core.EditorUtils;
using Game.Global;
using Game.Tournament;
#endif
using Game.Gameplay.LoadingScreen;
using Game.PlayerManagement;
using Game.Training;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Flows._0_Load
{
    public class LoadingManager : MonoBehaviour
    {
#if UNITY_EDITOR
        private IEnumerator SettingGameInfosInStandaloneRoutine()
        {
            {
                var tournamentContext = TournamentContext.Instance;
                if (tournamentContext)
                    yield break;
            }
            {
                var trainingContext = TrainingContext.Instance;
                if (trainingContext)
                    yield break;
            }
            
            var currentLevelInfoManagerGO = new GameObject("CurrentLevelInfoManager_Standalone");
            var currentLevelInfoManager = currentLevelInfoManagerGO.AddComponent<CurrentLevelInfoManager>();
            currentLevelInfoManagerGO.AddComponent<CurrentLevelInfoManagerStandaloneSingleton>().Initialize();

            if (!UnityEditor.EditorPrefs.GetBool(LevelEditorPrefsConstants.OverridesGameInfosKey, false))
                yield break;
            
            var currentLevelDataAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDataAsset>(UnityEditor.EditorPrefs.GetString("BB_LDA"));
            currentLevelInfoManager.SetCurrentLevelDataAsset(currentLevelDataAsset);

            var globalContextPrefab = AssetDatabase.LoadAssetAtPath<GlobalContext>("Assets/Game/Global/GlobalContext.prefab");
            var globalContext = Instantiate(globalContextPrefab); 
            
            yield return new WaitUntil(() => GlobalContext.IsInitialized);
            
            var devices = InputSystem.devices;
            for (int i = 0; i < UnityEditor.EditorPrefs.GetInt(LevelEditorPrefsConstants.NumberOfPlayersKey, 0); ++i)
            {
                var device = InputSystem.GetDevice(devices[UnityEditor.EditorPrefs.GetInt($"{LevelEditorPrefsConstants.DeviceAssignedKey}{i}", 0)].name);
                var playerInput = globalContext.PlayerManager.AddPlayerFromDevice(device);
                var abstractPlayer = playerInput.GetComponent<AbstractPlayer>();
                var characterDataAsset = AssetDatabase.LoadAssetAtPath<CharacterDataAsset>(EditorPrefs.GetString($"{LevelEditorPrefsConstants.CharacterDataAssignedKey}{i}"));
                abstractPlayer.SetCharacterData(characterDataAsset);
            }
        }
#endif

        private AsyncOperationHandle<GameObject> m_objectiveManagerOp;
        private AsyncOperationHandle<GameObject> m_specialActionOp;
        private List<AsyncOperationHandle<GameObject>> m_additionalSystemOp = new ();
        private AsyncOperationHandle<SceneInstance> m_environmentSceneOp;
        private AsyncOperationHandle<SceneInstance> m_levelSceneOp;
        private List<AsyncOperationHandle<SceneInstance>> m_optionalScenesOp = new ();
        
        public void Load(Action onLoadingComplete = null)
        {
            StartCoroutine(LoadingRoutine(onLoadingComplete));
        }

        public static CurrentLevelInfoManager FetchCurrentLevelInfoManager()
        {
            var tournamentContext = TournamentContext.Instance;
            if (tournamentContext)
                return tournamentContext.CurrentLevelInfoManager;
            var trainingContext = TrainingContext.Instance;
            if (trainingContext)
                return trainingContext.CurrentLevelInfoManager;
            var standaloneSingleton = CurrentLevelInfoManagerStandaloneSingleton.Instance;
            if (standaloneSingleton)
                return standaloneSingleton;
            
            Debug.LogError($"Failed to fetch current level info manager");
            return null;
        }
        
        private IEnumerator LoadingRoutine(Action onLoadingComplete = null)
        {
#if UNITY_EDITOR // When starting a game mode in standalone
            yield return SettingGameInfosInStandaloneRoutine();
#endif
            
            var currentLevelInfoManager = FetchCurrentLevelInfoManager();
            var currentLevelDataAsset = currentLevelInfoManager?.CurrentLevelDataAsset;
            var charactersManager = CharactersManager.Instance;

            if (!currentLevelDataAsset)
            {
                Debug.Log($"No current level data asset, cannot load level.");
                yield break;
            }

            m_objectiveManagerOp = currentLevelDataAsset.ObjectiveManagerPrefab.InstantiateAsync();
            yield return m_objectiveManagerOp.WaitForCompletion();
            
            m_specialActionOp = currentLevelDataAsset.SpecialActionPrefab.LoadAssetAsync<GameObject>();
            yield return m_specialActionOp.WaitForCompletion();
            
            for (int i = 0; i < currentLevelDataAsset.AdditionalSystemsToInstantiate.Count; i++)
            {
                var additionalSystemOp = currentLevelDataAsset.AdditionalSystemsToInstantiate[i].InstantiateAsync();
                m_additionalSystemOp.Add(additionalSystemOp);
                yield return additionalSystemOp.WaitForCompletion();
            }
            
            charactersManager.CreateCharactersAndPlayerControllers(m_specialActionOp.Result.GetComponent<SpecialAction>());

            // Loading environment
            {
                m_environmentSceneOp =
                    Addressables.LoadSceneAsync(currentLevelDataAsset.EnvironmentScene, LoadSceneMode.Additive);
                bool isCompleted = false;
                m_environmentSceneOp.Completed += _ => isCompleted = true;
                yield return new WaitUntil(() => isCompleted);

                SceneManager.SetActiveScene(m_environmentSceneOp.Result.Scene);
                //sceneOp.Release();
            }
            
            // Loading level (mainly containing the grid and all other gameplay elements in the world)
            {
                m_levelSceneOp =
                    Addressables.LoadSceneAsync(currentLevelDataAsset.LevelScene, LoadSceneMode.Additive);
                //sceneOp.ReleaseHandleOnCompletion();
                bool isCompleted = false;
                m_levelSceneOp.Completed += _ => isCompleted = true;
                yield return new WaitUntil(() => isCompleted);
            }
            
            // Loading optional scenes
            for (int i = 0; i < currentLevelDataAsset.AdditionalScenes.Count; ++i)
            {
                var sceneOp = Addressables.LoadSceneAsync(currentLevelDataAsset.AdditionalScenes[i], LoadSceneMode.Additive);
                //sceneOp.ReleaseHandleOnCompletion();
                m_optionalScenesOp.Add(sceneOp);
                bool isCompleted = false;
                sceneOp.Completed += _ => isCompleted = true;
                yield return new WaitUntil(() => isCompleted);
            }

            GlobalGameplayDataManager.Instance.SetDataAsset(currentLevelDataAsset.GlobalGameplayDataAsset.CreateAndGetData());
            
            yield return new WaitUntil(() => GridBuilder.IsInitialized);

            yield return new WaitUntil(() => GetComponent<LoadEventsHooker>().AllRequirementsMet);
            
            LoadingScreenManager.Instance?.HideLoadingScreen();
            
            onLoadingComplete?.Invoke();
        }


        public void Unload(Action onUnloadingComplete = null)
        {
            StartCoroutine(UnloadingRoutine(onUnloadingComplete));
        }

        private IEnumerator UnloadingRoutine(Action onUnloadingComplete = null)
        {
            m_objectiveManagerOp.Release();
            m_specialActionOp.Release();
            m_additionalSystemOp.ForEach(op => op.Release());
            m_environmentSceneOp.Release();
            m_levelSceneOp.Release();
            m_optionalScenesOp.ForEach(op => op.Release());

            m_objectiveManagerOp = default;
            m_specialActionOp = default;
            m_additionalSystemOp.Clear();
            m_environmentSceneOp = default;
            m_levelSceneOp = default;
            m_optionalScenesOp.Clear();
            
            yield return null;
            onUnloadingComplete?.Invoke();
        }
    }
}
