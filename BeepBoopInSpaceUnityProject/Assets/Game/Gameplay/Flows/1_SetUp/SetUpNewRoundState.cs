using System.Collections.Generic;
using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.CameraManagement;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.Flows._1_SetUp;
using Game.Gameplay.GridSystem;
using Game.Gameplay.Levels._0_Core;
using Game.Gameplay.Timer;
using UnityEngine;

namespace Game.Gameplay.Flows.NewRoundAnnouncement
{
    [RequireComponent(typeof(SetUpEventsHooker))]
    public class SetUpNewRoundState : AFlowState
    {
        [SerializeField]
        private AFlowState m_nextState;

        protected override void HandleEnter()
        {
            base.HandleEnter();
            
            var gridBuilder = GridBuilder.Instance;
            
            var charactersManager = CharactersManager.Instance;

            var levelManager = LevelManager.Instance;
            
            for (int i = 0; i < charactersManager.CharacterPawns.Count; i++)
            {
                var character = charactersManager.CharacterPawns[i];
                
                character.ReferencesHolder.MovementController.TeleportToCell(levelManager.StartingCells[i]);
            }

            var eventsHooker = GetComponent<SetUpEventsHooker>();
            eventsHooker.NotifySetUp();
            
            TimerManager.Instance.ResetTimer();
            
            CameraManager.Instance.SwitchToGameplayCamera();
            
            RequestState(m_nextState);
            
            eventsHooker.NotifySetUpCompleted();
        }
    }
}