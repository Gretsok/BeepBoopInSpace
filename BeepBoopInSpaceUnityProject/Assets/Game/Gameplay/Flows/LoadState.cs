using System.Collections;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.FlowMachine;
using Game.Gameplay.Levels._0_Core;
using Game.Gameplay.LoadingScreen;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Flows
{
    public class LoadState : AFlowState
    {
        [SerializeField] 
        private AFlowState m_nextState;
        [SerializeField]
        private string m_environmentSceneName = "Environment";
        protected override void HandleEnter()
        {
            base.HandleEnter();
            StartCoroutine(LoadingRoutine());
        }

        private IEnumerator LoadingRoutine()
        {
            var currentLevelDataAsset = CurrentLevelInfoManager.Instance.CurrentLevelDataAsset;
            
            
#if UNITY_EDITOR // When starting a game mode in standalone
            if (!currentLevelDataAsset)
                currentLevelDataAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDataAsset>(UnityEditor.EditorPrefs.GetString("BB_LDA"));
#endif
            
            yield return null;
            var charactersManager = CharactersManager.Instance;
            charactersManager.CreateCharactersAndPlayerControllers();
            
            var op = SceneManager.LoadSceneAsync(m_environmentSceneName, LoadSceneMode.Additive);
            yield return op;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(m_environmentSceneName));
            LoadingScreenManager.Instance?.HideLoadingScreen();
            RequestState(m_nextState);
        }
    }
}