using System;
using System.Collections;
using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.Death.Invincibility;
using Game.Global.SFXManagement;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.Gameplay.Announcement
{
    public class AnnouncementState : AFlowState,
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
            m_pauseAudioPlayer.Play();

            StartCoroutine(WaitAndDo(m_duration, () => RequestState(m_nextState)));

            var charactersManager = CharactersManager.Instance;
            foreach (var characterPawn in charactersManager.CharacterPawns)
            {
                characterPawn.ReferencesHolder.DeathController.RegisterInvincibilityGiver(this);
            }
        }

        override protected void HandleLeave()
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

        private static IEnumerator WaitAndDo(float duration, Action action)
        {
            yield return new WaitForSeconds(duration);
            action?.Invoke();
        }
    }
}