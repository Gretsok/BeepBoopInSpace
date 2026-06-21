using Game.Gameplay.Flows.Gameplay;
using UnityEngine;

namespace Game.Gameplay.ConfigurationsManagement
{
    public class PeriodicConfigurationChangesManager : MonoBehaviour
    {
        private ConfigurationsManager m_configurationsManager;
        
        [SerializeField] private float m_changePeriod = 10f;
        public bool IsRunning { get; private set; }
        public float TimePassedSinceLastChange { get; private set; }

        private void Start()
        {
            ConfigurationsManager.RegisterPostInitializationCallback(manager =>
            {
                m_configurationsManager = manager;
            });
            GameplayEventsHooker.RegisterPostInitializationCallback(hooker =>
            {
                hooker.OnGameplayResumed += HandleGameplayResumed;
                hooker.OnGameplayPaused += HandleGameplayPaused;
            });
        }

        private void OnDestroy()
        {
            var gameplayEventsHooker = GameplayEventsHooker.Instance;
            if (gameplayEventsHooker)
            {
                gameplayEventsHooker.OnGameplayResumed -= HandleGameplayResumed;
                gameplayEventsHooker.OnGameplayPaused -= HandleGameplayPaused;
            }
        }

        private void HandleGameplayResumed()
        {
            ResumeRunning();
        }

        private void HandleGameplayPaused()
        {
            PauseRunning();
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
            {
                TimePassedSinceLastChange = 0;
                m_configurationsManager.PerformChange();
            }
        }
    }
}
