using System.Collections.Generic;
using UnityEngine;

namespace Game.ArchitectureTools.FlowMachine
{
    public class PanelsActivatorStateComponent : AStateComponent
    {
        [SerializeField]
        private List<Panel> m_panels = new List<Panel>();

        protected override void HandleEntered(AFlowState obj)
        {
            foreach (var panel in m_panels)
            {
                if (panel)
                    panel.gameObject.SetActive(true);
            }
        }

        protected override void HandleLeft(AFlowState obj)
        {
            foreach (var panel in m_panels)
            {
                if (panel)
                    panel.gameObject.SetActive(false);
            }
        }
    }
}
