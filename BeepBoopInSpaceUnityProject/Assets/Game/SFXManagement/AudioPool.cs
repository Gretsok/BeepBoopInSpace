using System.Collections.Generic;
using UnityEngine;

namespace Game.SFXManagement
{
    public class AudioPool : MonoBehaviour
    {
        [SerializeField]
        private AudioSource m_audioSourcePrefab;
        
        private readonly List<AudioSource> m_availableSources = new List<AudioSource>();
        private readonly List<AudioSource> m_runningSources = new List<AudioSource>();

        public AudioSource GetAudioSource()
        {
            AudioSource newAudioSource;
            if (m_availableSources.Count == 0)
                newAudioSource = Instantiate<AudioSource>(m_audioSourcePrefab, transform);
            else
                newAudioSource = m_availableSources[0];
            m_runningSources.Add(newAudioSource);
            newAudioSource.gameObject.SetActive(true);
            return newAudioSource;
        }

        public void FreeAudioSource(AudioSource audioSource)
        {
            m_runningSources.Remove(audioSource);
            audioSource.clip = null;
            audioSource.pitch = 1f;
            audioSource.volume = 1f;
            m_availableSources.Add(audioSource);
            audioSource.gameObject.SetActive(false);
        }
    }
}