using System;
using System.Collections;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.FlowMachine;
using Game.Gameplay.MusicsManagement;
using Game.Global.SFXManagement;
using Game.SFXManagement;
using UnityEngine;

namespace Game.Gameplay.Flows.NewRoundAnnouncement
{
    public class NewRoundAnnouncementState : AFlowState
    {
        [SerializeField]
        private AFlowState m_nextState;

        [SerializeField]
        private float m_waitDurationOnStart = 1f;
        [SerializeField] 
        private float m_showDurationPerCharacter = 3f;
        
        [SerializeField]
        private AudioPlayer m_audioPlayer1;
        [SerializeField]
        private AudioPlayer m_audioPlayer2;
        
        [SerializeField]
        private AudioPlayer m_hologrammeAudioPlayer;
        
        
        protected override void HandleEnter()
        {
            base.HandleEnter();

            StartCoroutine(IntroductionRoutine());
        }

        private IEnumerator IntroductionRoutine()
        {
            yield return new WaitForSeconds(m_waitDurationOnStart);
            
            var charactersManager = CharactersManager.Instance;
            var introductionManager = IntroductionManager.Instance;

            for (int i = 0; i < charactersManager.CharacterPawns.Count; i++)
            {
                var characterPawn = charactersManager.CharacterPawns[i];
                introductionManager.InflateCharacterPawn(characterPawn);
                
                m_hologrammeAudioPlayer.Play();
                
                yield return new WaitForSeconds(m_showDurationPerCharacter/2);
                if (i == charactersManager.CharacterPawns.Count - 1)
                    m_audioPlayer1.Play();
                yield return new WaitForSeconds(m_showDurationPerCharacter/2);
            }

            var audioSource = m_audioPlayer2.PlayWithAudioSourceReturned();
            StartCoroutine(WaitForAudioSourceToEnd(audioSource,() => MusicsManager.Instance.StartPlayingGameplayMusics()));
            introductionManager.Stop();
            RequestState(m_nextState);
        }

        private IEnumerator WaitForAudioSourceToEnd(AudioSource source, Action onComplete)
        {
            while (source.isPlaying)
                yield return null;
            onComplete?.Invoke();
        }
    }
}