using System;
using System.Collections;
using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.Death.Invincibility;
using Game.Global.SFXManagement;
using UnityEngine;

namespace Game.Gameplay.RoundsManagement
{
    public class RoundAnnouncementState : AFlowState,
        IInvincibilityGiver
    {
        [SerializeField] 
        private float m_duration = 2f;
        [SerializeField]
        private float m_invincibilityDurationAfterEndOfAnnouncement = 2f;
        [SerializeField]
        private AFlowState m_nextState = null;

        [SerializeField]
        private AudioPlayer m_pauseAudioPlayer;
        [SerializeField]
        private AudioPlayer m_resumeAudioPlayer;
        
        protected override void HandleEnter()
        {
            base.HandleEnter();
            var gameplayContext = GameplayContext.Instance;
            RoundsManager roundsManager = gameplayContext.RoundsManager;
            var panel = gameplayContext.UIManager.GetPanel<NewRoundPanel>();
            if (roundsManager.IsActive)
                panel.SetRoundText(roundsManager.RoundIndex + 1);
            m_pauseAudioPlayer.Play();

            var charactersManager = CharactersManager.Instance;
            foreach (var characterPawn in charactersManager.CharacterPawns)
            {
                characterPawn.ReferencesHolder.DeathController.RegisterInvincibilityGiver(this);
            }

            StartCoroutine(WaitAndDo(m_duration, () => RequestState(m_nextState)));
        }

        private static IEnumerator WaitAndDo(float duration, Action action)
        {
            yield return new WaitForSeconds(duration);
            action?.Invoke();
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            
            EndInvincibilityAfterAnnouncement();

            m_resumeAudioPlayer.Play();
        }

        private void EndInvincibilityAfterAnnouncement()
        {
            StartCoroutine(WaitAndDo(m_invincibilityDurationAfterEndOfAnnouncement, () =>
            {
                var charactersManager = CharactersManager.Instance;
                foreach (var characterPawn in charactersManager.CharacterPawns)
                {
                    characterPawn.ReferencesHolder.DeathController.UnregisterInvincibilityGiver(this);
                }
            }));
        }
    }
}
