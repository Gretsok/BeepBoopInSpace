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
        
        public bool IsPlayingGameplayMusics { get; private set; }

        public void StartPlayingGameplayMusics()
        {
            IsPlayingResultsMusic = false;
            IsPlayingGameplayMusics = true;
            
            m_currentIndex = 0;
            m_musicAudioSource.clip = m_gameplayClips[m_currentIndex];
            m_musicAudioSource.loop = false;
            m_musicAudioSource.Play();
        }

        public void StopPlayingGameplayMusics()
        {
            IsPlayingResultsMusic = false;
            m_musicAudioSource.Stop();
            m_musicAudioSource.clip = null;
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
        private void Update()
        {
            if (IsPlayingGameplayMusics)
            {
                if (!m_musicAudioSource.isPlaying)
                {
                    ++m_currentIndex;
                    if (m_currentIndex >= m_gameplayClips.Count)
                    {
                        IsPlayingGameplayMusics = false;
                    }
                    else
                    {
                        m_musicAudioSource.clip = m_gameplayClips[m_currentIndex];
                        m_musicAudioSource.Play();
                    }
                }
            }
        }
    }
}