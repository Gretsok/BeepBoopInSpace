using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Gameplay.Flows._0_Load;
using Game.Gameplay.Flows.Gameplay;
using Game.Gameplay.GridSystem;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.GridSystem.GenericComponents;
using Game.Online;
using Game.Online.Gameplay;
using UnityEngine;

namespace Game.Gameplay.GameModes.GameplayModifiers.Meteorites
{
    public class MeteoritesModifier : AGameplayModifier
    {
        [SerializeField]
        private MeteoriteSystem m_meteoriteSystemPrefab;
        private List<MeteoriteSystem> m_instantiatedMeteoriteSystems = new List<MeteoriteSystem>();

        private GridBuilder m_gridBuilder;
        
        [SerializeField]
        private int m_prewarmedMeteoritesAmount = 10;
        
        [SerializeField]
        private float m_startSpawnCooldown = 1f;
        [SerializeField]
        private float m_cooldownReducingSpeed = 0.05f;
        [SerializeField]
        private float m_minimalCooldown = 0.05f;
        public float SpawnCooldown => Mathf.Max(m_minimalCooldown, m_startSpawnCooldown - m_cooldownReducingSpeed * TimePassedSpawning);
        [SerializeField]
        private float m_startDropTime = 2f;
        [SerializeField]
        private float m_dropTimeReducingSpeed = 0.00f;
        [SerializeField]
        private float m_minimalDropTime = 0.1f;
        public float DropTime => Mathf.Max(m_minimalDropTime, m_startDropTime - m_dropTimeReducingSpeed * TimePassedSpawning);

        [SerializeField, Tooltip("No new meteorite for this long each time the game resumes (start, new buttons, new round): the players get to grips with their buttons first. The intensity doesn't grow meanwhile.")]
        private float m_quietTimeAfterResume = 2f;
        private float m_quietTimeLeft;

        public bool IsSpawning { get; private set; }
        public float TimePassedSpawning { get; private set; }
        
        private bool IsMeteoritesManagerInitialized()
        {
            return IsInitialized;
        }

        protected override IEnumerator HandleInitializationRoutine()
        {
            yield return new WaitUntil(() => LoadEventsHooker.Instance);
            LoadEventsHooker.Instance.AddLoadingRequirement(IsMeteoritesManagerInitialized);

            yield return new WaitUntil(() => GameplayEventsHooker.Instance);
            var gameplayEventsHooker = GameplayEventsHooker.Instance;
            gameplayEventsHooker.OnGameplayResumed += ActivateMeteoritesSpawning;
            gameplayEventsHooker.OnGameplayPaused += DeactivateMeteoritesSpawning;
            
            for (int i = 0; i < m_prewarmedMeteoritesAmount; i++)
            {
                var meteoriteSystem = Instantiate(m_meteoriteSystemPrefab, transform);
                meteoriteSystem.ResetMeteorite();
                m_instantiatedMeteoriteSystems.Add(meteoriteSystem);
            }
            
            TimePassedSpawning = 0f;
            
            GridBuilder.RegisterPostInitializationCallback(builder => m_gridBuilder = builder);
        }

        private void ActivateMeteoritesSpawning()
        {
            m_lastSpawnTime = float.MinValue;
            m_quietTimeLeft = m_quietTimeAfterResume;
            IsSpawning = true;
        }

        private void DeactivateMeteoritesSpawning()
        {
            IsSpawning = false;
        }

        private float m_lastSpawnTime;
        private void Update()
        {
            // Online guest: the host drops the meteorites (ReplicateMeteorite).
            if (!IsSpawning || !Netplay.HasAuthority)
                return;

            if (m_quietTimeLeft > 0f)
            {
                m_quietTimeLeft -= Time.deltaTime;
                return;
            }

            TimePassedSpawning += Time.deltaTime;
            
            if (m_lastSpawnTime + SpawnCooldown < TimePassedSpawning)
            {
                SpawnMeteorite();
            }
        }

        private void SpawnMeteorite()
        {
            m_lastSpawnTime = TimePassedSpawning;

            var target = m_gridBuilder.GetRandomWalkableCell(cell => !cell.TryGetComponent<KillingCellComponent>(out _));
            if (!target)
                return;
            var meteoriteSystem = PrepareAndGetReadyMeteoriteSystem();
            meteoriteSystem.Drop(target, DropTime);
            GameplaySync.NotifyMeteorite(target, DropTime);
            Debug.Log($"Spawn meteorite at {m_lastSpawnTime}", meteoriteSystem.gameObject);
        }

        /// <summary>Online guest: the host dropped a meteorite. It only kills on the host.</summary>
        public void ReplicateMeteorite(Cell target, float dropTime)
        {
            PrepareAndGetReadyMeteoriteSystem().Drop(target, dropTime);
        }

        /// <summary>A meteorite is falling on the cell.</summary>
        public bool IsCellTargeted(Cell cell)
        {
            return m_instantiatedMeteoriteSystems.Exists(meteorite => meteorite.Target == cell);
        }

        private MeteoriteSystem PrepareAndGetReadyMeteoriteSystem()
        {
            var meteoriteSystem = m_instantiatedMeteoriteSystems.FirstOrDefault(meteorite => meteorite.ReadyToBeUsed);

            if (!meteoriteSystem)
            {
                meteoriteSystem = Instantiate(m_meteoriteSystemPrefab, transform);
                meteoriteSystem.ResetMeteorite();
                m_instantiatedMeteoriteSystems.Add(meteoriteSystem);
            }
            
            return meteoriteSystem;
        }
    }
}
