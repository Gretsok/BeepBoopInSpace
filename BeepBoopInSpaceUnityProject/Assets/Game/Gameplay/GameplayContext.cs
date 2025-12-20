using Game.ArchitectureTools.Manager;
using Game.Gameplay.Flows._0_Load;
using UnityEngine;

namespace Game.Gameplay
{
    public class GameplayContext : AManager<GameplayContext>
    {
        [field: SerializeField]
        public LoadingManager LoadingManager { get; private set; }
        [field: SerializeField]
        public FlowMachine.FlowMachine FlowMachine { get; private set; }
    }
}
