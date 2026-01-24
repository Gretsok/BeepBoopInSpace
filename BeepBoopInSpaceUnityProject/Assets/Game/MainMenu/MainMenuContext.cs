using System.Collections;
using Game.ArchitectureTools.Manager;
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
            
            MainMenuOrchestrator.Initialize(CameraManager);
        }
    }
}