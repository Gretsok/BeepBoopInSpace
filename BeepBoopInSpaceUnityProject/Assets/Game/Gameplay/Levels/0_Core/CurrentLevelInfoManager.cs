using System;
using System.Collections;
using Game.ArchitectureTools.Manager;

namespace Game.Gameplay.Levels._0_Core
{
    public class CurrentLevelInfoManager : AManager<CurrentLevelInfoManager>
    {
        public LevelDataAsset CurrentLevelDataAsset { get; private set; }

        public event Action<CurrentLevelInfoManager, LevelDataAsset> OnCurrentLevelDataAssetChanged;
        
        public void SetCurrentLevelDataAsset(LevelDataAsset levelDataAsset)
        {
            CurrentLevelDataAsset = levelDataAsset;
            OnCurrentLevelDataAssetChanged?.Invoke(this, CurrentLevelDataAsset);
        }

        protected override IEnumerator Initialize()
        {
            DontDestroyOnLoad(this.gameObject);
            yield return base.Initialize();
        }
    }
}
