using System;
using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using UnityEngine;

namespace Game.Gameplay.MusicsManagement
{
    public class MusicsManager : AManager<MusicsManager>
    {
        [SerializeField]
        private List<AudioClip> m_gameplayClips = new();
        [SerializeField]
        private AudioSource m_musicAudioSource;

        [SerializeField]
        private AudioClip m_resultsMusic;
        private int m_currentIndex = 0;
        private bool m_hasClipStarted;

        public bool IsPlayingGameplayMusics { get; private set; }

        public void StartPlayingGameplayMusics()
        {
            IsPlayingResultsMusic = false;
            IsPlayingGameplayMusics = m_gameplayClips.Count > 0;
            if (!IsPlayingGameplayMusics)
                return;

            m_currentIndex = 0;
            PlayGameplayClip();
        }

        public void StopPlayingGameplayMusics()
        {
            // Otherwise Update would start the next clip at once, over the end of the game and its results.
            IsPlayingGameplayMusics = false;
            IsPlayingResultsMusic = false;
            m_musicAudioSource.Stop();
            m_musicAudioSource.clip = null;
        }

        private void PlayGameplayClip()
        {
            m_hasClipStarted = false;
            m_musicAudioSource.clip = m_gameplayClips[m_currentIndex];
            m_musicAudioSource.loop = false;
            m_musicAudioSource.Play();
        }


        public bool IsPlayingResultsMusic { get; private set; }
        public void StartPlayingResultsMusic()
        {
            IsPlayingGameplayMusics = false;
            IsPlayingResultsMusic = true;
            
            m_musicAudioSource.clip = m_resultsMusic;
            m_musicAudioSource.loop = true;
            m_musicAudioSource.Play();
        }

        // A clip played to its end gives way to the next one. The clips last less than a game with its tutorial and
        // announcements: they start over. Not before the clip has started playing (a clip still loading, no audio device).
        private void Update()
        {
            if (!IsPlayingGameplayMusics)
                return;
            if (m_musicAudioSource.isPlaying)
            {
                m_hasClipStarted = true;
                return;
            }
            if (!m_hasClipStarted)
                return;
            m_currentIndex = (m_currentIndex + 1) % m_gameplayClips.Count;
            PlayGameplayClip();
        }
    }
}