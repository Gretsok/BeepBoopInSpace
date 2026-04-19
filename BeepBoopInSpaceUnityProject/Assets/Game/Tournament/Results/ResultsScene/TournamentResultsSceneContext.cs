using System.Collections;
using Game.ArchitectureTools.Manager;
using Game.Global;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Tournament.Results.ResultsScene
{
    public class TournamentResultsSceneContext : AManager<TournamentResultsSceneContext>
    {
        [field: SerializeField]
        public TournamentResultsCanvas ResultsCanvas { get; private set; }

        protected override IEnumerator Initialize()
        {
            yield return base.Initialize();
            
            var globalContext = GlobalContext.Instance;
            globalContext.NavigationAuthorityManager.SetInputRegistrationsCallbacks(RegisterMainPlayerActions, UnregisterMainPlayerAction);
        }
        
        private void RegisterMainPlayerActions(InputActionAsset actionAsset)
        {
            actionAsset.FindActionMap("UI").FindAction("Submit").started += HandleSubmitStartedByMainPlayer;
        }

        private void UnregisterMainPlayerAction(InputActionAsset actionAsset)
        {
            actionAsset.FindActionMap("UI").FindAction("Submit").started -= HandleSubmitStartedByMainPlayer;
        }
        
        private void HandleSubmitStartedByMainPlayer(InputAction.CallbackContext obj)
        {
            ResultsCanvas.ManualClickButton();
            
            var globalContext = GlobalContext.Instance;
            globalContext.NavigationAuthorityManager.UnsetInputRegistrationsCallbacks(RegisterMainPlayerActions, UnregisterMainPlayerAction);
        }
    }
}
