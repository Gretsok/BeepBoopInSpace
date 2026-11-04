using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.CameraManagement;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.Levels._0_Core;
using Game.Online;
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
            
            // Online guest: the host places the robots, their moves arrive with its messages.
            if (Netplay.HasAuthority)
                PlaceCharacters(CharactersManager.Instance, LevelManager.Instance);

            var eventsHooker = GetComponent<SetUpEventsHooker>();
            eventsHooker?.NotifySetUp();

            CameraManager.Instance.SwitchToGameplayCamera();

            RequestState(m_nextState);

            eventsHooker?.NotifySetUpCompleted();
        }

        private static void PlaceCharacters(CharactersManager charactersManager, LevelManager levelManager)
        {            
            // Everyone leaves their cell before anyone is placed: a character still standing on another one's
            // starting cell would make that teleport fail, leaving the other character without a cell.
            for (int i = 0; i < charactersManager.CharacterPawns.Count; i++)
            {
                var character = charactersManager.CharacterPawns[i];

                var deathController = character.ReferencesHolder.DeathController;
                // A robot whose player left the online room stays dead, off the grid.
                if (!character.HasLeft)
                    deathController.Resurrect();
                character.ReferencesHolder.MovementController.DistachFromCurrentCell();
            }

            for (int i = 0; i < charactersManager.CharacterPawns.Count; i++)
            {
                var character = charactersManager.CharacterPawns[i];
                if (character.HasLeft)
                    continue;

                var startingCellData = levelManager.StartingCellsData[i];
                var movementController = character.ReferencesHolder.MovementController;
                movementController.TeleportToCell(startingCellData.Cell);
                movementController.ChangeDirection(startingCellData.Direction);
            }
        }
    }
}