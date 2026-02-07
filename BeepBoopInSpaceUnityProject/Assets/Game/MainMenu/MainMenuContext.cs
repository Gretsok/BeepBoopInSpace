using System.Collections;
using Game.ArchitectureTools.FlowMachine;
using Game.ArchitectureTools.Manager;
using Game.Global;
using Game.MainMenu.ZoneManagement;
using UnityEngine;

namespace Game.MainMenu
{
    public class MainMenuContext : AManager<MainMenuContext>
    {
        [field: SerializeField]
        public ZoneManager ZoneManager { get; private set; }
        [field: SerializeField]
        public MainMenuOrchestrator MainMenuOrchestrator { get; private set; }
        [field: SerializeField]
        public FlowMachine FlowMachine { get; private set; }

        protected override IEnumerator Initialize()
        {
            yield return new WaitUntil(() => GlobalContext.IsInitialized);
            MainMenuOrchestrator.Initialize(GlobalContext.Instance, this);
            FlowMachine.Run();
        }
    }
}