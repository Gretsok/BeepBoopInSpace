using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.CameraManagement;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.Levels._0_Core;
using UnityEngine;

namespace Game.Gameplay.Flows._1_SetUp
{
    [RequireComponent(typeof(SetUpEventsHooker))]
    public class SetUpNewRoundState : AFlowState
    {
        [SerializeField]
        private AFlowState m_nextState;

        protected override void HandleEnter()
        {
            base.HandleEnter();
            
            var charactersManager = CharactersManager.Instance;

            var levelManager = LevelManager.Instance;
            
            for (int i = 0; i < charactersManager.CharacterPawns.Count; i++)
            {
                var character = charactersManager.CharacterPawns[i];

                var startingCellData = levelManager.StartingCellsData[i];
                var movementController = character.ReferencesHolder.MovementController;
                movementController.TeleportToCell(startingCellData.Cell);
                movementController.ChangeDirection(startingCellData.Direction);
            }

            var eventsHooker = GetComponent<SetUpEventsHooker>();
            eventsHooker.NotifySetUp();
            
            CameraManager.Instance.SwitchToGameplayCamera();
            
            RequestState(m_nextState);
            
            eventsHooker.NotifySetUpCompleted();
        }
    }
}