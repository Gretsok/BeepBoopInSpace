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
        private ArchitectureTools.FlowMachine.FlowMachine m_flowMachine;

        [SerializeField] private AFlowState m_configurationChangeAnnouncementState;
        [SerializeField] private float m_changePeriod = 10f;
        public float TimePassedSinceLastChange { get; private set; }

        public bool IsRunning { get; private set; } = false;

        protected override IEnumerator Initialize()
        {
            PerformChange(true);
            yield return null;
        }

        public void PauseRunning()
        {
            IsRunning = false;
        }

        public void ResumeRunning()
        {
            IsRunning = true;
        }

        private void Update()
        {
            if (!IsRunning)
                return;
            TimePassedSinceLastChange += Time.deltaTime;

            if (TimePassedSinceLastChange >= m_changePeriod)
                PerformChange();
        }

        private void PerformChange(bool skipState = false)
        {
            TimePassedSinceLastChange = 0f;
            
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