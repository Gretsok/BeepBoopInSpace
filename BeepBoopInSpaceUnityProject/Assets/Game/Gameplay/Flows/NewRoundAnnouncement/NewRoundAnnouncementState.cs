using System.Collections;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.FlowMachine;
using UnityEngine;

namespace Game.Gameplay.Flows.NewRoundAnnouncement
{
    public class NewRoundAnnouncementState : AFlowState
    {
        [SerializeField]
        private AFlowState m_nextState;

        [SerializeField] 
        private float m_showDurationPerCharacter = 3f;
        
        protected override void HandleEnter()
        {
            base.HandleEnter();

            StartCoroutine(IntroductionRoutine());
        }

        private IEnumerator IntroductionRoutine()
        {
            var charactersManager = CharactersManager.Instance;
            var introductionManager = IntroductionManager.Instance;

            for (int i = 0; i < charactersManager.CharacterPawns.Count; i++)
            {
                var characterPawn = charactersManager.CharacterPawns[i];
                introductionManager.InflateCharacterPawn(characterPawn);
                yield return new WaitForSeconds(m_showDurationPerCharacter);
            }

            introductionManager.Stop();
            RequestState(m_nextState);
        }
    }
}