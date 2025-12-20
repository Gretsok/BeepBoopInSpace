using System.Collections;
using System.Linq;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.Death;
using Game.Gameplay.Flows._1_SetUp;
using Game.Gameplay.Flows.Finish;
using Game.Gameplay.Flows.Gameplay;
using Game.Gameplay.Flows.Results;
using Game.Gameplay.Timer;
using UnityEngine;

namespace Game.Gameplay.GameModes.ObjectiveManagement.LastManStandingObjectiveManagement
{
    public class LastManStandingObjectiveManager : AObjectiveManager
    {
        private CharactersManager m_charactersManager;

        [SerializeField]
        private float m_playTimeAfterLastDeath = 5f;
        
        public float TimePassed { get; private set; } = 0f;
        public bool IsTicking { get; private set; } = false;
        
        protected override IEnumerator Initialize()
        {
            CharactersManager.RegisterPostInitializationCallback(manager =>
            {
                m_charactersManager = manager;

                m_charactersManager.OnCharactersCreated += SetUp;
            });

            SetUpEventsHooker.Instance.OnSetUpCompleted += HandleSetUpCompleted;

            var gameplayEventsHooker = GameplayEventsHooker.Instance;
            gameplayEventsHooker.OnGameplayResumed += HandleGameplayResumed;
            gameplayEventsHooker.OnGameplayPaused += HandleGameplayPaused;
            
            ResultsManager.RegisterPostInitializationCallback(manager => manager.SetScoreCalculationMethod(ResultsManager.EScoreCalculationMethod.HigherScoreIsBest));

            Destroy(TimerManager.Instance.gameObject);
            
            yield break;
        }

        private void HandleGameplayResumed()
        {
            IsTicking = true;
        }

        private void HandleGameplayPaused()
        {
            IsTicking = false;
        }
        
        private void HandleSetUpCompleted()
        {
            
        }

        private void SetUp(CharactersManager obj)
        {
            m_charactersManager.OnCharactersCreated -= SetUp;
            
            foreach (var pawn in m_charactersManager.CharacterPawns)
            {
                pawn.ReferencesHolder.DeathController.CanResurrect = false;
                pawn.ReferencesHolder.DeathController.OnDeath += HandlePlayedDeath;
            }
        }
        
        private void HandlePlayedDeath(DeathController obj)
        {
            obj.CharacterReferencesHolder.ScoringController.SetScore((int)(TimePassed * 10f));

            var alivePlayersCount = m_charactersManager.CharacterPawns.Count(pawn => pawn.ReferencesHolder.DeathController.IsAlive);
            if (alivePlayersCount == 1)
            {
                StartCoroutine(TriggerEndOfGame());
            }
        }

        private IEnumerator TriggerEndOfGame()
        {
            yield return new WaitForSeconds(m_playTimeAfterLastDeath);
            FlowMachine.FlowMachine.Instance.RequestState(FinishStateGrabber.Instance.FinishState);
            m_charactersManager.CharacterPawns.First(pawn => pawn.ReferencesHolder.DeathController.IsAlive)
                .ReferencesHolder.ScoringController.SetScore((int)(TimePassed * 10f));
        }

        private void Update()
        {
            if (IsTicking)
                TimePassed += Time.deltaTime;
        }
    }
}
