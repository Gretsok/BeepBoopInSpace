using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Levels._0_Core
{
    [CreateAssetMenu(fileName = "name - LevelListDataAsset", menuName = "Game/Gameplay/Levels/Level List Data Asset")]
    public class LevelsListDataAsset : ScriptableObject
    {
        [SerializeField]
        private List<LevelDataAsset> m_levelDataList = new();
        public IReadOnlyList<LevelDataAsset> LevelDataList => m_levelDataList;
    }
}
