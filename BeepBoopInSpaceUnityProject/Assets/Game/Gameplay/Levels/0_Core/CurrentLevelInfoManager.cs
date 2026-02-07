using System;
using UnityEngine;

namespace Game.Gameplay.Levels._0_Core
{
    public class CurrentLevelInfoManager : MonoBehaviour
    {
        public LevelDataAsset CurrentLevelDataAsset { get; private set; }

        public event Action<CurrentLevelInfoManager, LevelDataAsset> OnCurrentLevelDataAssetChanged;
        
        public void SetCurrentLevelDataAsset(LevelDataAsset levelDataAsset)
        {
            CurrentLevelDataAsset = levelDataAsset;
            OnCurrentLevelDataAssetChanged?.Invoke(this, CurrentLevelDataAsset);
        }
    }
}
