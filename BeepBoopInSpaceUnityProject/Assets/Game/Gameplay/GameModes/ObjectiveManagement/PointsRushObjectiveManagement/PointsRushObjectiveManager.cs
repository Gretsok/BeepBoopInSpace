using System.Collections;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.Movement;
using Game.Gameplay.Flows._1_SetUp;
using Game.Gameplay.GridSystem;
using Game.Gameplay.GridSystem.GenericComponents;
using Game.Gameplay.Timer;
using Game.Global.SFXManagement;
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
        
        public Cell CurrentObjectiveCell { get; private set; }
        
        private GridBuilder m_gridBuilder;
        private CharactersManager m_charactersManager;
        protected override IEnumerator Initialize()
        {
            GridBuilder.RegisterPostInitializationCallback(manager => m_gridBuilder = manager);
            CharactersManager.RegisterPostInitializationCallback(manager =>
            {
                m_charactersManager = manager;
            });

            SetUpEventsHooker.RegisterPostInitializationCallback(setUpManager => setUpManager.OnTimeToSetUpDependencies += SetUp);
            
            yield break;
        }

        private void HandlePawnMove(CharacterMovementController characterMovementController, Cell cell)
        {
            if (cell == CurrentObjectiveCell)
            {
                characterMovementController.ReferencesHolder.ScoringController.IncreaseScore();
                UpdateObjective();
            }
        }

        private void SetUp()
        {
            foreach (var pawn in m_charactersManager.CharacterPawns)
            {
                pawn.ReferencesHolder.MovementController.OnPositionUpdated += HandlePawnMove;
            }
            UpdateObjective();
        }
        
        [Button]
        private void UpdateObjective()
        {
            if (m_currentObjectiveIndication != null)
                Destroy(m_currentObjectiveIndication);

            do
            {
                CurrentObjectiveCell = m_gridBuilder.GetRandomAvailableWalkableCell((cell) => !cell.GetComponent<KillingCellComponent>());
            } while (!CurrentObjectiveCell || DoesAPawnStandOnCell(CurrentObjectiveCell));
            m_objectiveCollectedAudioPlayer.Play();
            m_currentObjectiveIndication = Instantiate(m_objectiveIndicationPrefab, CurrentObjectiveCell.transform);
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