using Game.ArchitectureTools.Manager;

namespace Game.Gameplay.GlobalGameplayData
{
    public class GlobalGameplayDataManager : AManager<GlobalGameplayDataManager>
    {
        public GlobalGameplayData Data { get; private set; }

        public void SetDataAsset(GlobalGameplayData globalGameplayData)
        {
            Data = globalGameplayData;
        }
    }
}
