using System.Collections.Generic;
using Game.SFXManagement;
using UnityEngine;

namespace Game.Global.SFXManagement
{
    [RequireComponent(typeof(AudioPool))]
    public class AudioManager : MonoBehaviour
    {
        private AudioPool m_audioPool;

        public void Initialize()
        {
            m_audioPool = GetComponent<AudioPool>();
        }

        private readonly List<AudioSource> m_audioSources = new();

        public AudioSource Play2DSound(AudioPlayer audioPlayer)
        {
            var audioSource = m_audioPool.GetAudioSource();
            audioSource.resource = audioPlayer.AudioResource;
            audioSource.volume = audioPlayer.Volume;
            audioSource.Play();
            m_audioSources.Add(audioSource);
            audioSource.gameObject.name = $"{audioPlayer.AudioResource.name}_AudioSource";
            return audioSource;
        }

        private void Update()
        {
            for (int i =  m_audioSources.Count - 1; i >= 0; --i)
            {
                var audioSource = m_audioSources[i];
                if (audioSource && audioSource.isPlaying)
                    continue;
                
                m_audioSources.RemoveAt(i);
                audioSource.gameObject.name = "UnusedAudioSource";
                m_audioPool.FreeAudioSource(audioSource);
            }
        }
    }
}
