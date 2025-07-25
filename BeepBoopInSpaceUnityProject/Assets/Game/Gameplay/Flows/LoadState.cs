using System.Collections;
using Game.Characters;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core;
using Game.Gameplay.FlowMachine;
using Game.Gameplay.GridSystem;
using Game.Gameplay.Levels._0_Core;
using Game.Gameplay.LoadingScreen;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Game.PlayerManagement;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using Game.Gameplay.Levels._0_Core.EditorUtils;
using UnityEditor;
#endif

namespace Game.Gameplay.Flows
{
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

            var specialActionOp = currentLevelDataAsset.SpecialActionPrefab.LoadAssetAsync<GameObject>();
            yield return specialActionOp.WaitForCompletion();
            
            charactersManager.CreateCharactersAndPlayerControllers(specialActionOp.Result.GetComponent<SpecialAction>());

            for (int i = 0; i < currentLevelDataAsset.AdditionalScenes.Count; ++i)
            {
                var sceneOp = Addressables.LoadSceneAsync(currentLevelDataAsset.AdditionalScenes[i], LoadSceneMode.Additive);
                bool isCompleted = false;
                sceneOp.Completed += _ => isCompleted = true;
                yield return new WaitUntil(() => isCompleted);
                if (i == currentLevelDataAsset.AdditionalSceneIndexToActivate)
                {
                    SceneManager.SetActiveScene(sceneOp.Result.Scene);
                }
            }

            var gridDataAssetOp = currentLevelDataAsset.GridDataAsset.LoadAssetAsync();
            yield return gridDataAssetOp.WaitForCompletion();
            
            
            var dictionaryDataAssetOp = currentLevelDataAsset.CellsDictionaryDataAsset.LoadAssetAsync();
            yield return dictionaryDataAssetOp.WaitForCompletion();
            
            var gridBuilder = GridBuilder.Instance;
            
            gridBuilder.SetData(gridDataAssetOp.Result, dictionaryDataAssetOp.Result);
            gridBuilder.BuildGridFromGridDataAsset();
            
            LoadingScreenManager.Instance?.HideLoadingScreen();
            RequestState(m_nextState);
        }
    }
}