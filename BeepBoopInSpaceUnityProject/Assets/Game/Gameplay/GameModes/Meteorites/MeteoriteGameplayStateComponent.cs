using Game.Gameplay.FlowMachine;
using Game.Gameplay.Flows.Gameplay;
using UnityEngine;

namespace Game.Gameplay.GameModes.Meteorites
{
    public class MeteoriteGameplayStateComponent : MonoBehaviour
    {
        private GameplayState m_gameplayState;
        private MeteoritesManager m_meteoritesManager;
        private void Awake()
        {
            m_gameplayState = GetComponent<GameplayState>();
            m_gameplayState.OnEntered += HandleEntered;
            m_gameplayState.OnLeft += HandleLeft;
            
            MeteoritesManager.RegisterPostInitializationCallback(manager => m_meteoritesManager = manager);
        }

        private void HandleEntered(AFlowState obj)
        {
            m_meteoritesManager.ActivateMeteoritesSpawning();
        }

        private void HandleLeft(AFlowState obj)
        {
            m_meteoritesManager.DeactivateMeteoritesSpawning();
        }
    }
}
