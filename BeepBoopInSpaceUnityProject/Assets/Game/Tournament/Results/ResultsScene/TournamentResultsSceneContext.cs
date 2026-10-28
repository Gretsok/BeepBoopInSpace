using Game.ArchitectureTools.Manager;
using UnityEngine;

namespace Game.Tournament.Results.ResultsScene
{
    public class TournamentResultsSceneContext : AManager<TournamentResultsSceneContext>
    {
        [field: SerializeField]
        public TournamentResultsCanvas ResultsCanvas { get; private set; }
    }
}
