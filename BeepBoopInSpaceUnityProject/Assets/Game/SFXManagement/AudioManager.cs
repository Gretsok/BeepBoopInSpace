using System.Collections;
using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using UnityEngine;

namespace Game.SFXManagement
{
    [RequireComponent(typeof(AudioPool))]
    public class AudioManager : AManager<AudioManager>
    {
        private AudioPool m_audioPool;

        protected override IEnumerator Initialize()
        {
            m_audioPool = GetComponent<AudioPool>();
            DontDestroyOnLoad(gameObject);
            yield break;
        }

        private readonly List<AudioSource> m_audioSources = new();

        public void Play2DSound(AudioPlayer audioPlayer)
        {
            var audioSource = m_audioPool.GetAudioSource();
            audioSource.resource = audioPlayer.AudioResource;
            audioSource.volume = audioPlayer.Volume;
            audioSource.Play();
            m_audioSources.Add(audioSource);
            audioSource.gameObject.name = $"{audioPlayer.AudioResource.name}_AudioSource";
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
