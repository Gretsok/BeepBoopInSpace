using System.Collections;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.Levels._0_Core;
using Game.Tournament.Config;
using Game.Tournament.Flow;
using Game.Tournament.Results;
using UnityEngine;

namespace Game.Tournament
{
    public class TournamentContext : AManager<TournamentContext>
    {
        [field: Header("Data")]
        [field: SerializeField]
        public TournamentConfigDataAsset ConfigDataAsset { get; private set; }
        
        [field: Header("Managers")]
        [field: SerializeField]
        public TournamentFlowManager FlowManager { get; private set; }
        [field: SerializeField]
        public TournamentResultsManager ResultsManager { get; private set; }
        [field: SerializeField]
        public CurrentLevelInfoManager CurrentLevelInfoManager { get; private set; }
        
        protected override IEnumerator Initialize()
        {
            DontDestroyOnLoad(gameObject);
            ResultsManager.InjectDependencies(ConfigDataAsset);
            FlowManager.InjectDependencies(ResultsManager, ConfigDataAsset, CurrentLevelInfoManager);
            
            yield break;
        }
    }
}
