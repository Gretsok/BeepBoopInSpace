using System.Collections.Generic;
using Game.Gameplay.Levels._0_Core;
using UnityEngine;

namespace Game.Tournament.Config
{
    [CreateAssetMenu(menuName = "Game/Tournament/Config/TournamentConfigDataAsset", fileName = "TournamentConfigDataAsset")]
    public class TournamentConfigDataAsset : ScriptableObject
    {
        [field: SerializeField]
        public int ScoreToReach { get; private set; } = 3;
        [field: SerializeField]
        public List<LevelDataAsset> LevelDataAssets { get; private set; }
    }
}
