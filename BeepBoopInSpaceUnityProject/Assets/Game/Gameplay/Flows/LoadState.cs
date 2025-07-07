using System.Collections;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.FlowMachine;
using Game.Gameplay.GridSystem;
using Game.Gameplay.Levels._0_Core;
using Game.Gameplay.LoadingScreen;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

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

        private IEnumerator LoadingRoutine()
        {
            var currentLevelDataAsset = CurrentLevelInfoManager.Instance?.CurrentLevelDataAsset;
            
            
#if UNITY_EDITOR // When starting a game mode in standalone
            if (!currentLevelDataAsset)
                currentLevelDataAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDataAsset>(UnityEditor.EditorPrefs.GetString("BB_LDA"));
#endif
            
            yield return null;
            var charactersManager = CharactersManager.Instance;
            charactersManager.CreateCharactersAndPlayerControllers();

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