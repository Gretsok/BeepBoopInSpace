using System.Collections;
using Game.ArchitectureTools.Manager;

namespace Game.Gameplay.Levels._0_Core
{
    public class CurrentLevelInfoManager : AManager<CurrentLevelInfoManager>
    {
        public LevelDataAsset CurrentLevelDataAsset { get; private set; }

        public void Setup(LevelDataAsset levelDataAsset)
        {
            CurrentLevelDataAsset = levelDataAsset;
        }

        protected override IEnumerator Initialize()
        {
            DontDestroyOnLoad(this.gameObject);
            yield return base.Initialize();
        }
    }
}
