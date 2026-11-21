using System.Collections;
using System.Linq;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.Death;
using Game.Gameplay.CharactersManagement.Death.Invincibility;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.Gameplay.Flows._1_SetUp;
using Game.Gameplay.Flows.Finish;
using Game.Gameplay.Flows.Gameplay;
using Game.Gameplay.Flows.Results;
using Game.Gameplay.RoundsManagement;
using Game.Online;
using Game.Online.Gameplay;
using Game.UITools.WorldOverlay;
using UnityEngine;

namespace Game.Gameplay.GameModes.ObjectiveManagement.LastManStandingObjectiveManagement
{
    public class LastManStandingObjectiveManager : AObjectiveManager, IInvincibilityGiver
    {
        private CharactersManager m_charactersManager;

        [SerializeField]
        private float m_playTimeAfterLastDeath = 5f;
        [SerializeField, Tooltip("Time the winner of a round stays alone on the arena, under its \"+1\", before the next round: everyone sees who won it.")]
        private float m_timeBeforeNextRound = 1.5f;
        private DeathController m_roundWinner;
        [field: SerializeField]
        public int PointsObjective { get; private set; } = 3;
        private RoundsManager m_roundsManager;

        public override string ScoreCaption => "Manches gagnées";
        public override int ScoreGoal => PointsObjective;
        
        public float TimePassed { get; private set; } = 0f;
        public bool IsTicking { get; private set; } = false;
        
        protected override IEnumerator Initialize()
        {
            GameplayContext.RegisterPostInitializationCallback(context =>
            {
                m_roundsManager = context.RoundsManager;
                
                m_roundsManager.SetActive(true);
            });
            CharactersManager.RegisterPostInitializationCallback(manager =>
            {
                m_charactersManager = manager;

                m_charactersManager.OnCharactersCreated += SetUp;
            });

            SetUpEventsHooker.Instance.OnSetUpCompleted += HandleSetUpCompleted;

            var gameplayEventsHooker = GameplayEventsHooker.Instance;
            gameplayEventsHooker.OnGameplayResumed += HandleGameplayResumed;
            gameplayEventsHooker.OnGameplayPaused += HandleGameplayPaused;
            
            ResultsManager.RegisterPostInitializationCallback(manager => manager.SetScoreCalculationMethod(EScoreCalculationMethod.HigherScoreIsBest));
            
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
            SetUpEventsHooker.Instance.OnSetUpCompleted -= HandleSetUpCompleted;
            
            
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
            // Deaths outside of gameplay (e.g. meteorites falling on the results screen) must not end a round.
            // Online guest: the host ends the rounds. A round already won stays won.
            if (!IsTicking || !Netplay.HasAuthority || m_roundWinner)
                return;

            var alivePlayersCount = m_charactersManager.CharacterPawns.Count(pawn => pawn.ReferencesHolder.DeathController.IsAlive);
            if (alivePlayersCount == 1)
            {
                TriggerEndOfRound();
            }
        }

        private void TriggerEndOfRound()
        {
            var winner = m_charactersManager.CharacterPawns.First(pawn => pawn.ReferencesHolder.DeathController.IsAlive).ReferencesHolder;
            // Nothing takes the round from its winner while everyone looks at it.
            m_roundWinner = winner.DeathController;
            m_roundWinner.RegisterInvincibilityGiver(this);
            winner.ScoringController.IncreaseScore();
            GameplaySync.NotifyRoundWon(winner);
            PlayRoundWonFeedback(winner);

            if (winner.ScoringController.Score >= PointsObjective)
                StartCoroutine(TriggerEndOfGame());
            else
                StartCoroutine(TriggerNewRoundAfterDelay());
        }

        private IEnumerator TriggerNewRoundAfterDelay()
        {
            yield return new WaitForSeconds(m_timeBeforeNextRound);
            m_roundWinner.UnregisterInvincibilityGiver(this);
            m_roundWinner = null;
            // The timer may have ended the game in the meantime.
            if (!IsTicking)
                yield break;
            m_roundsManager.TriggerNewRound();
        }

        /// <summary>Online guest: the host's robot won the round.</summary>
        public void ReplicateRoundWon(CharacterReferencesHolder winner)
        {
            PlayRoundWonFeedback(winner);
        }

        // A "+1" of the winner's color over it, like a point of Points Rush.
        private static void PlayRoundWonFeedback(CharacterReferencesHolder winner)
        {
            var cell = winner.GridWalker.CurrentCell;
            var position = cell ? cell.transform.position : winner.ModelSource.position;
            GameplayContext.Instance.UIManager.GetPanel<WorldOverlayPanel>()
                .PlayFloatingText("+1", winner.CharacterDataAsset.CharacterColor, position);
        }

        private IEnumerator TriggerEndOfGame()
        {
            yield return new WaitForSeconds(m_playTimeAfterLastDeath);
            // The timer may have ended the game in the meantime.
            if (!IsTicking)
                yield break;
            GameplayContext.Instance.FlowMachine.RequestState(FinishStateGrabber.Instance.FinishState);
        }

        private void Update()
        {
            if (IsTicking)
                TimePassed += Time.deltaTime;
        }
    }
}
