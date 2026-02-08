using System.Collections;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.Flows._0_Load;
using Game.Gameplay.Levels._0_Core;
using Game.Gameplay.PauseMenu;
using UnityEngine;

namespace Game.Gameplay
{
    public class GameplayContext : AManager<GameplayContext>
    {
        [field: SerializeField]
        public LoadingManager LoadingManager { get; private set; }
        [field: SerializeField]
        public ArchitectureTools.FlowMachine.FlowMachine FlowMachine { get; private set; }
        [field: SerializeField]
        public PauseMenuManager PauseMenuManager { get; private set; }
        
        public CurrentLevelInfoManager CurrentLevelInfoManager { get; private set; }

        private IEnumerator Start()
        {
            FlowMachine.Run();
            PauseMenuManager.Initialize(FlowMachine);
            
            yield return new WaitUntil(() => LoadingManager.HasLoadedLevel);
            CurrentLevelInfoManager = LoadingManager.FetchCurrentLevelInfoManager();
        }
    }
}
