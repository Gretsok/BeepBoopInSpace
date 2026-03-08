using System.Collections.Generic;
using Game.ArchitectureTools.Manager;

namespace Game.Gameplay.Flows._0_Load
{
    public class LoadEventsHooker : AManager<LoadEventsHooker>
    {
        public delegate bool DLoadingRequirement();
        
        private readonly List<DLoadingRequirement> m_loadingRequirements = new();

        public void AddLoadingRequirement(DLoadingRequirement loadingRequirement)
        {
            m_loadingRequirements.Add(loadingRequirement);
        }

        public bool AllRequirementsMet => m_loadingRequirements.TrueForAll(r => r.Invoke());
    }
}
