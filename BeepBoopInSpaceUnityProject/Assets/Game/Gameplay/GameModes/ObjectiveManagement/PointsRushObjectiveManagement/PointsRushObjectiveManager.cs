using System.Collections;
using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.Movement;
using Game.Gameplay.ConfigurationsManagement;
using Game.Gameplay.Flows._1_SetUp;
using Game.Gameplay.GridSystem;
using Game.Gameplay.GridSystem.GenericComponents;
using Game.Gameplay.Timer;
using Game.Global.SFXManagement;
using Game.Online;
using Game.Online.Gameplay;
using Game.UITools.WorldOverlay;
using NaughtyAttributes;
using UnityEngine;

namespace Game.Gameplay.GameModes.ObjectiveManagement.PointsRushObjectiveManagement
{
    public class PointsRushObjectiveManager : AObjectiveManager
    {
        [SerializeField]
        private AudioPlayer m_objectiveCollectedAudioPlayer;
        [SerializeField] private GameObject m_objectiveIndicationPrefab;
        private GameObject m_currentObjectiveIndication;
        [SerializeField]
        private TimerManager m_timerManager;
        [SerializeField]
        private PeriodicConfigurationChangesManager m_periodicConfigurationChangesManager;
        
        public Cell CurrentObjectiveCell { get; private set; }

        public override string ScoreCaption => "Points";
        
        private GridBuilder m_gridBuilder;
        private CharactersManager m_charactersManager;
        private GameplayContext m_gameplayContext;
        protected override IEnumerator Initialize()
        {
            GridBuilder.RegisterPostInitializationCallback(manager => m_gridBuilder = manager);
            CharactersManager.RegisterPostInitializationCallback(manager =>
            {
                m_charactersManager = manager;
            });
            GameplayContext.RegisterPostInitializationCallback(manager =>
            {
                m_gameplayContext = manager;
            });

            SetUpEventsHooker.RegisterPostInitializationCallback(setUpManager => setUpManager.OnTimeToSetUpDependencies += SetUp);
            
            yield break;
        }

        private void HandlePawnMove(CharacterMovementController characterMovementController, Cell cell)
        {
            // Online guest: the host gives the points (ReplicatePointCollected).
            if (cell == CurrentObjectiveCell && Netplay.HasAuthority)
            {
                characterMovementController.ReferencesHolder.ScoringController.IncreaseScore();
                GameplaySync.NotifyPointCollected(characterMovementController, cell);

                PlayPointFeedback(characterMovementController, cell);

                UpdateObjective();
            }
        }

        private void PlayPointFeedback(CharacterMovementController characterMovementController, Cell cell)
        {
            var worldOverlayPanel = m_gameplayContext.UIManager.GetPanel<WorldOverlayPanel>();
            worldOverlayPanel.PlayFloatingText("+1", characterMovementController.ReferencesHolder.CharacterDataAsset.CharacterColor, cell.transform.position);
        }

        private void SetUp()
        {
            SetUpEventsHooker.Instance.OnTimeToSetUpDependencies -= SetUp;
            
            foreach (var pawn in m_charactersManager.CharacterPawns)
            {
                pawn.ReferencesHolder.MovementController.OnPositionUpdated += HandlePawnMove;
            }
            // Online guest: the host picks the cells (ReplicateObjective).
            if (Netplay.HasAuthority)
                UpdateObjective();
        }
        
        [Button]
        private void UpdateObjective()
        {
            if (m_currentObjectiveIndication != null)
                Destroy(m_currentObjectiveIndication);

            CurrentObjectiveCell = m_gridBuilder.GetRandomAvailableWalkableCell(cell =>
                !cell.GetComponent<KillingCellComponent>() && !DoesAPawnStandOnCell(cell));
            GameplaySync.NotifyObjective(CurrentObjectiveCell);
            ShowObjective();
        }

        private void ShowObjective()
        {
            if (!CurrentObjectiveCell)
                return;
            m_objectiveCollectedAudioPlayer.Play();
            m_currentObjectiveIndication = Instantiate(m_objectiveIndicationPrefab, CurrentObjectiveCell.transform);
        }

        /// <summary>Online guest: the host picked the cell of the next point (none if null).</summary>
        public void ReplicateObjective(Cell cell)
        {
            if (m_currentObjectiveIndication != null)
                Destroy(m_currentObjectiveIndication);
            CurrentObjectiveCell = cell;
            ShowObjective();
        }

        /// <summary>Online guest: a robot took the point of the cell.</summary>
        public void ReplicatePointCollected(CharacterMovementController characterMovementController, Cell cell)
        {
            PlayPointFeedback(characterMovementController, cell);
        }

        private bool DoesAPawnStandOnCell(Cell cell)
        {
            foreach (var pawn in m_charactersManager.CharacterPawns)
            {
                if (pawn.ReferencesHolder.GridWalker.CurrentCell == cell)
                    return true;
            }

            return false;
        }
    }
}