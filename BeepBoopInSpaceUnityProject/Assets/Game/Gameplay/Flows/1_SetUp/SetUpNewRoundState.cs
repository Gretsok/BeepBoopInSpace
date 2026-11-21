using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.CameraManagement;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.Levels._0_Core;
using UnityEngine;

namespace Game.Gameplay.Flows._1_SetUp
{
    public class SetUpNewRoundState : AFlowState
    {
        [SerializeField]
        private AFlowState m_nextState;

        protected override void HandleEnter()
        {
            base.HandleEnter();
            
            var charactersManager = CharactersManager.Instance;

            var levelManager = LevelManager.Instance;
            
            // Everyone leaves their cell before anyone is placed: a character still standing on another one's
            // starting cell would make that teleport fail, leaving the other character without a cell.
            for (int i = 0; i < charactersManager.CharacterPawns.Count; i++)
            {
                var character = charactersManager.CharacterPawns[i];

                var deathController = character.ReferencesHolder.DeathController;
                deathController.Resurrect();
                character.ReferencesHolder.MovementController.DistachFromCurrentCell();
            }

            for (int i = 0; i < charactersManager.CharacterPawns.Count; i++)
            {
                var character = charactersManager.CharacterPawns[i];

                var startingCellData = levelManager.StartingCellsData[i];
                var movementController = character.ReferencesHolder.MovementController;
                movementController.TeleportToCell(startingCellData.Cell);
                movementController.ChangeDirection(startingCellData.Direction);
            }

            var eventsHooker = GetComponent<SetUpEventsHooker>();
            eventsHooker?.NotifySetUp();
            
            CameraManager.Instance.SwitchToGameplayCamera();
            
            RequestState(m_nextState);
            
            eventsHooker?.NotifySetUpCompleted();
        }
    }
}