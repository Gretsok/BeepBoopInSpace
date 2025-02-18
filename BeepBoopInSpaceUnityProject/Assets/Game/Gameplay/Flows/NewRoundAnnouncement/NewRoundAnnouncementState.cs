using System;
using System.Collections;
using System.Collections.Generic;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.FlowMachine;
using Game.Gameplay.GridSystem;
using UnityEngine;

namespace Game.Gameplay.Flows.NewRoundAnnouncement
{
    public class NewRoundAnnouncementState : AFlowState
    {
        [SerializeField]
        private AFlowState m_nextState;

        [SerializeField] 
        private float m_stateDuration = 4f;
        [SerializeField] 
        private List<Cell> m_spawnPointsCells;
        protected override void HandleEnter()
        {
            base.HandleEnter();
            
            var charactersManager = CharactersManager.Instance;
            charactersManager.CreateCharactersAndPlayerControllers();

            for (int i = 0; i < charactersManager.CharacterPawns.Count; i++)
            {
                var character = charactersManager.CharacterPawns[i];
                
                character.TeleportToCell(m_spawnPointsCells[i]);
            }
            
            StartCoroutine(WaitAndDo(m_stateDuration, () => RequestState(m_nextState)));
        }

        private static IEnumerator WaitAndDo(float duration, Action action)
        {
            yield return new WaitForSeconds(duration);
            action?.Invoke();
        }
    }
}