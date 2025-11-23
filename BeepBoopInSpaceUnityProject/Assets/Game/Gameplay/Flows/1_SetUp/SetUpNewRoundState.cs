using System.Collections.Generic;
using Game.Gameplay.CameraManagement;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.FlowMachine;
using Game.Gameplay.Flows._1_SetUp;
using Game.Gameplay.GridSystem;
using Game.Gameplay.Timer;
using UnityEngine;

namespace Game.Gameplay.Flows.NewRoundAnnouncement
{
    [RequireComponent(typeof(SetUpEventsHooker))]
    public class SetUpNewRoundState : AFlowState
    {
        [SerializeField]
        private AFlowState m_nextState;
        [SerializeField] 
        private List<Vector2Int> m_spawnPointsCells;

        protected override void HandleEnter()
        {
            base.HandleEnter();
            
            var gridBuilder = GridBuilder.Instance;
            
            var charactersManager = CharactersManager.Instance;

            for (int i = 0; i < charactersManager.CharacterPawns.Count; i++)
            {
                var character = charactersManager.CharacterPawns[i];
                
                character.ReferencesHolder.MovementController.TeleportToCell(gridBuilder.GetCellAt(m_spawnPointsCells[i]));
            }

            GetComponent<SetUpEventsHooker>().NotifySetUp();
            
            TimerManager.Instance.ResetTimer();
            
            CameraManager.Instance.SwitchToGameplayCamera();
            
            RequestState(m_nextState);
        }
    }
}