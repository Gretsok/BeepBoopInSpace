using System;
using System.Collections;
using System.Collections.Generic;
using Game.ArchitectureTools.FlowMachine;
using Game.ArchitectureTools.Manager;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Gameplay.ConfigurationsManagement
{
    public class ConfigurationsManager : AManager<ConfigurationsManager>
    {
        [SerializeField]
        private FlowMachine m_flowMachine;

        [SerializeField] private AFlowState m_configurationChangeAnnouncementState;

        protected override IEnumerator Initialize()
        {
            PerformChange(true);
            yield return null;
        }

        public void PerformChange(bool skipState = false)
        {
            CurrentConfiguration.Clear();

            int maxIndex = 5;
            for (int i = 0; i < 6; i++)
            {
                int newIndex;
                do
                {
                    newIndex = Random.Range(0, maxIndex + 1);
                } while (CurrentConfiguration.Contains(newIndex));
                CurrentConfiguration.Add(newIndex);
            }

            if (!skipState)
            {
                Debug.Log($"Request new configuration announcement");
                m_flowMachine.RequestState(m_configurationChangeAnnouncementState);
            }
        }
        
        public List<int> CurrentConfiguration { get; private set; } = new List<int>();
    }
}