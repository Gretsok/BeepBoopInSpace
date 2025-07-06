using System.Collections.Generic;
using Game.Gameplay.CameraManagement;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.FlowMachine;
using Game.Gameplay.GameModes.PointsRush.ObjectiveManagement;
using Game.Gameplay.GridSystem;
using Game.Gameplay.Timer;
using UnityEngine;

namespace Game.Gameplay.Flows.NewRoundAnnouncement
{
    public class SetUpNewRoundState : AFlowState
    {
        [SerializeField]
        private AFlowState m_nextState;
        [SerializeField] 
        private List<Cell> m_spawnPointsCells;
        protected override void HandleEnter()
        {
            base.HandleEnter();
            
            var charactersManager = CharactersManager.Instance;

            for (int i = 0; i < charactersManager.CharacterPawns.Count; i++)
            {
                var character = charactersManager.CharacterPawns[i];
                
                character.TeleportToCell(m_spawnPointsCells[i]);
            }

            PointsRushObjectiveManager.Instance.SetUp();
            
            TimerManager.Instance.ResetTimer();
            
            CameraManager.Instance.SwitchToGameplayCamera();
            
            RequestState(m_nextState);
        }
    }
}