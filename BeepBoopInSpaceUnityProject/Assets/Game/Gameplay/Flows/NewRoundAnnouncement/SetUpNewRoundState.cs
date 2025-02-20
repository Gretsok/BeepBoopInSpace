using System.Collections.Generic;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.FlowMachine;
using Game.Gameplay.GridSystem;
using Game.Gameplay.ObjectiveManagement;
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

            ObjectiveManager.Instance.SetUp();
            
            TimerManager.Instance.ResetTimer();
            
            RequestState(m_nextState);
        }
    }
}