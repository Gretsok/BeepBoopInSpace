using System.Collections;
using Game.ArchitectureTools.Manager;
using Game.Global;
using Game.MainMenu.CameraManagement;
using UnityEngine;

namespace Game.MainMenu
{
    public class MainMenuContext : AManager<MainMenuContext>
    {
        [field: SerializeField]
        public CameraManager CameraManager { get; private set; }
        [field: SerializeField]
        public MainMenuOrchestrator MainMenuOrchestrator { get; private set; }

        protected override IEnumerator Initialize()
        {
            yield return base.Initialize();
            
            var globalContext = GlobalContext.Instance;
            var settingsManager = globalContext.SettingsManager;
            var saveManager = globalContext.SaveManager;
            
            MainMenuOrchestrator.Initialize(CameraManager, settingsManager, saveManager);
        }
    }
}