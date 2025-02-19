using System.Collections;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.FlowMachine;
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
            yield return null;
            var charactersManager = CharactersManager.Instance;
            charactersManager.CreateCharactersAndPlayerControllers();
            
            var op = SceneManager.LoadSceneAsync(m_environmentSceneName, LoadSceneMode.Additive);
            yield return op;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(m_environmentSceneName));
            
            RequestState(m_nextState);
        }
    }
}