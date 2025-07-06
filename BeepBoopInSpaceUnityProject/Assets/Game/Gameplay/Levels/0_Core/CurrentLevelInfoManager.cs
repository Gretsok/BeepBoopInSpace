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
    }
}
