using System.Collections;
using Game.ArchitectureTools.FlowMachine;
using Game.ArchitectureTools.Manager;
using Game.Global;
using Game.MainMenu.CharacterManagement;
using UnityEngine;

namespace Game.MainMenu
{
    public class MainMenuContext : AManager<MainMenuContext>
    {
        [field: SerializeField]
        public MainMenuOrchestrator MainMenuOrchestrator { get; private set; }
        [field: SerializeField]
        public FlowMachine FlowMachine { get; private set; }
        [field: SerializeField]
        public GameModesLauncher GameModesLauncher { get; private set; }

        protected override IEnumerator Initialize()
        {
            // Back from a game this may run in the scene's first Awake: the orchestrator needs the bank awake.
            yield return new WaitUntil(() => GlobalContext.Instance && GlobalContext.Instance.IsInitialized && CharacterBankManager.Instance);
            MainMenuOrchestrator.Initialize(GlobalContext.Instance, this);
            MainMenuOrchestrator.Run();
        }
    }
}