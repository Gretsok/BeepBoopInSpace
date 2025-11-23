using System;
using Game.ArchitectureTools.Manager;

namespace Game.Gameplay.Flows._1_SetUp
{
    public class SetUpEventsHooker : AManager<SetUpEventsHooker>
    {
        public event Action OnTimeToSetUpDependencies;

        public void NotifySetUp()
        {
            OnTimeToSetUpDependencies?.Invoke();
        }
    }
}
