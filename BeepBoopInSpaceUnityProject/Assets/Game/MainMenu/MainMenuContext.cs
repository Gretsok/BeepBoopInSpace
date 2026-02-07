using System.Collections;
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

        protected override IEnumerator Initialize()
        {
            yield return base.Initialize();
            
            var globalContext = GlobalContext.Instance;
            var playerManager = globalContext.PlayerManager;
            var settingsManager = globalContext.SettingsManager;
            var saveManager = globalContext.SaveManager;
            
            MainMenuOrchestrator.Initialize(ZoneManager, playerManager, settingsManager, saveManager);
        }
    }
}