using System.Collections;
using Game.Characters;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core;
using Game.Gameplay.FlowMachine;
using Game.Gameplay.GlobalGameplayData;
using Game.Gameplay.GridSystem;
using Game.Gameplay.Levels._0_Core;
using Game.Gameplay.Levels._0_Core.EditorUtils;
using Game.Gameplay.LoadingScreen;
using Game.PlayerManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Flows._0_Load
{
    [RequireComponent(typeof(LoadEventsHooker))]
    public class LoadState : AFlowState
    {
        [SerializeField] 
        private AFlowState m_nextState;
        protected override void HandleEnter()
        {
            base.HandleEnter();
            StartCoroutine(LoadingRoutine());
        }
        
#if UNITY_EDITOR
        private IEnumerator SettingGameInfosInStandaloneRoutine()
        {
            var currentLevelDataAsset = CurrentLevelInfoManager.Instance?.CurrentLevelDataAsset;

            if (currentLevelDataAsset)
                yield break;
            
            if (!UnityEditor.EditorPrefs.GetBool(LevelEditorPrefsConstants.OverridesGameInfosKey, false))
                yield break;
            
            if (!CurrentLevelInfoManager.Instance)
            {
                var currentLevelInfoManager = new GameObject("CurrentLevelInfoManager").AddComponent<CurrentLevelInfoManager>();
                yield return new WaitUntil(() => currentLevelInfoManager.IsInitialized);
                
                currentLevelDataAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDataAsset>(UnityEditor.EditorPrefs.GetString("BB_LDA"));
                currentLevelInfoManager.Setup(currentLevelDataAsset);
            }

            var playerManagerPrefab = AssetDatabase.LoadAssetAtPath<PlayerManager>("Assets/Game/PlayerManagement/PlayerManager.prefab");
            
            var playerManager = Instantiate(playerManagerPrefab); 
            
            yield return new WaitUntil(() => playerManager.IsInitialized);
            
            var devices = InputSystem.devices;
            for (int i = 0; i < UnityEditor.EditorPrefs.GetInt(LevelEditorPrefsConstants.NumberOfPlayersKey, 0); ++i)
            {
                var device = InputSystem.GetDevice(devices[UnityEditor.EditorPrefs.GetInt($"{LevelEditorPrefsConstants.DeviceAssignedKey}{i}", 0)].name);
                var playerInput = PlayerManager.Instance.AddPlayerFromDevice(device);
                var abstractPlayer = playerInput.GetComponent<AbstractPlayer>();
                var characterDataAsset = AssetDatabase.LoadAssetAtPath<CharacterDataAsset>(EditorPrefs.GetString($"{LevelEditorPrefsConstants.CharacterDataAssignedKey}{i}"));
                abstractPlayer.SetCharacterData(characterDataAsset);
            }
        }
#endif

        private IEnumerator LoadingRoutine()
        {
#if UNITY_EDITOR // When starting a game mode in standalone
            yield return SettingGameInfosInStandaloneRoutine();
#endif
            
            var currentLevelDataAsset = CurrentLevelInfoManager.Instance?.CurrentLevelDataAsset;
            var charactersManager = CharactersManager.Instance;

            if (!currentLevelDataAsset)
            {
                Debug.Log($"No current level data asset, cannot load level.");
                yield break;
            }

            var objectiveManagerOp = currentLevelDataAsset.ObjectiveManagerPrefab.InstantiateAsync();
            yield return objectiveManagerOp.WaitForCompletion();
            
            var specialActionOp = currentLevelDataAsset.SpecialActionPrefab.LoadAssetAsync<GameObject>();
            yield return specialActionOp.WaitForCompletion();
            
            for (int i = 0; i < currentLevelDataAsset.AdditionalSystemsToInstantiate.Count; i++)
            {

                var additionalSystemOp = currentLevelDataAsset.AdditionalSystemsToInstantiate[i].InstantiateAsync();
                yield return additionalSystemOp.WaitForCompletion();
            }
            
            charactersManager.CreateCharactersAndPlayerControllers(specialActionOp.Result.GetComponent<SpecialAction>());



            // Loading environment
            {
                var sceneOp =
                    Addressables.LoadSceneAsync(currentLevelDataAsset.EnvironmentScene, LoadSceneMode.Additive);
                bool isCompleted = false;
                sceneOp.Completed += _ => isCompleted = true;
                yield return new WaitUntil(() => isCompleted);

                SceneManager.SetActiveScene(sceneOp.Result.Scene);
            }
            
            // Loading level (mainly containing the grid and all other gameplay elements in the world)
            {
                var sceneOp =
                    Addressables.LoadSceneAsync(currentLevelDataAsset.LevelScene, LoadSceneMode.Additive);
                bool isCompleted = false;
                sceneOp.Completed += _ => isCompleted = true;
                yield return new WaitUntil(() => isCompleted);
            }
            
            // Loading optional scenes
            for (int i = 0; i < currentLevelDataAsset.AdditionalScenes.Count; ++i)
            {
                var sceneOp = Addressables.LoadSceneAsync(currentLevelDataAsset.AdditionalScenes[i], LoadSceneMode.Additive);
                bool isCompleted = false;
                sceneOp.Completed += _ => isCompleted = true;
                yield return new WaitUntil(() => isCompleted);
            }

            GlobalGameplayDataManager.Instance.SetDataAsset(currentLevelDataAsset.GlobalGameplayDataAsset.CreateAndGetData());
            
            yield return new WaitUntil(() => GridBuilder.Instance.IsInitialized);

            yield return new WaitUntil(() => GetComponent<LoadEventsHooker>().AllRequirementsMet);
            
            LoadingScreenManager.Instance?.HideLoadingScreen();
            
            RequestState(m_nextState);
        }
    }
}