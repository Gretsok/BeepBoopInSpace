using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.ArchitectureTools.FlowMachine;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.CharactersManagement.Actions;
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
            // Only the control mode's buttons are used: with 4 of them, the same configuration would often come back.
            var gameplayContext = GameplayContext.Instance;
            var buttonsCount = gameplayContext ? gameplayContext.ControlMode.ButtonsCount() : ECharacterControlMode.Advanced.ButtonsCount();
            var previousButtons = CurrentConfiguration.FindAll(button => button < buttonsCount);
            do
            {
                Shuffle();
            } while (CurrentConfiguration.FindAll(button => button < buttonsCount).SequenceEqual(previousButtons));

            if (!skipState)
            {
                Debug.Log($"Request new configuration announcement");
                m_flowMachine.RequestState(m_configurationChangeAnnouncementState);
            }
        }

        private void Shuffle()
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
        }

        public List<int> CurrentConfiguration { get; private set; } = new List<int>();
    }
}