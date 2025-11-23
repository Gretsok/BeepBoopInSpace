using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.Flows._0_Load;
using Game.Gameplay.Flows.Gameplay;
using Game.Gameplay.GridSystem;
using UnityEngine;

namespace Game.Gameplay.GameModes.Meteorites
{
    public class MeteoritesManager : AManager<MeteoritesManager>
    {
        [SerializeField]
        private MeteoriteSystem m_meteoriteSystemPrefab;
        private List<MeteoriteSystem> m_instantiatedMeteoriteSystems = new List<MeteoriteSystem>();

        private GridBuilder m_gridBuilder;
        
        [SerializeField]
        private int m_prewarmedMeteoritesAmount = 10;
        
        [SerializeField]
        private float m_testSpawnCooldown = 1f;
        public float SpawnCooldown => m_testSpawnCooldown;
        [SerializeField]
        private float m_testDropTime = 2f;
        public float DropTime => m_testDropTime;

        public bool IsSpawning { get; private set; }
        public float TimePassedSpawning { get; private set; }

        private bool IsMeteoritesManagerInitialized()
        {
            return IsInitialized;
        }
        
        protected override IEnumerator Initialize()
        {
            yield return new WaitUntil(() => LoadEventsHooker.Instance);
            LoadEventsHooker.Instance.AddLoadingRequirement(IsMeteoritesManagerInitialized);

            yield return new WaitUntil(() => GameplayEventsHooker.Instance);
            var gameplayEventsHooker = GameplayEventsHooker.Instance;
            gameplayEventsHooker.OnGameplayResumed += ActivateMeteoritesSpawning;
            gameplayEventsHooker.OnGameplayPaused += DeactivateMeteoritesSpawning;
            
            for (int i = 0; i < m_prewarmedMeteoritesAmount; i++)
            {
                var meteoriteSystem = Instantiate(m_meteoriteSystemPrefab);
                meteoriteSystem.ResetMeteorite();
                m_instantiatedMeteoriteSystems.Add(meteoriteSystem);
            }
            
            GridBuilder.RegisterPostInitializationCallback(builder => m_gridBuilder = builder);
        }

        private void ActivateMeteoritesSpawning()
        {
            m_lastSpawnTime = float.MinValue;
            TimePassedSpawning = 0f;
            IsSpawning = true;
        }

        private void DeactivateMeteoritesSpawning()
        {
            IsSpawning = false;
        }

        private float m_lastSpawnTime;
        private void Update()
        {
            if (!IsSpawning)
                return;

            TimePassedSpawning += Time.deltaTime;
            
            if (m_lastSpawnTime + SpawnCooldown < TimePassedSpawning)
            {
                SpawnMeteorite();
            }
        }

        private void SpawnMeteorite()
        {
            m_lastSpawnTime = TimePassedSpawning;
            
            var meteoriteSystem = PrepareAndGetReadyMeteoriteSystem();
            meteoriteSystem.Drop(m_gridBuilder.GetRandomWalkableCell(), DropTime);
            Debug.Log($"Spawn meteorite at {m_lastSpawnTime}", meteoriteSystem.gameObject);
        }

        private MeteoriteSystem PrepareAndGetReadyMeteoriteSystem()
        {
            var meteoriteSystem = m_instantiatedMeteoriteSystems.FirstOrDefault(meteorite => meteorite.ReadyToBeUsed);

            if (!meteoriteSystem)
            {
                meteoriteSystem = Instantiate(m_meteoriteSystemPrefab);
                meteoriteSystem.ResetMeteorite();
                m_instantiatedMeteoriteSystems.Add(meteoriteSystem);
            }
            
            return meteoriteSystem;
        }
    }
}
