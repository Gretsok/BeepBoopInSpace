using Game.SFXManagement;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement
{
    public class CharacterSFXsHandler : MonoBehaviour
    {
        [SerializeField]
        private AudioPlayer m_moveAudioPlayer;
        [SerializeField]
        private AudioPlayer m_turnAudioPlayer;
        [SerializeField]
        private AudioPlayer m_cannotMoveAudioPlayer;

        [SerializeField]
        private AudioPlayer m_explosionAudioPlayer;
        [SerializeField]
        private AudioPlayer m_spawnAudioPlayer;

        public void PlayMoveAudio()
        {
            m_moveAudioPlayer.Play();
        }

        public void PlayTurnAudio()
        {
            m_turnAudioPlayer.Play();
        }

        public void PlayCannotMoveAudio()
        {
            m_cannotMoveAudioPlayer.Play();
        }

        public void PlayExplosionAudio()
        {
            m_explosionAudioPlayer.Play();
        }

        public void PlaySpawnAudio()
        {
            m_spawnAudioPlayer.Play();
        }
    }
}