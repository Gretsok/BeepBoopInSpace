using Game.SFXManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Global.SFXManagement
{
    public class AudioPlayer : MonoBehaviour
    {
        [field: SerializeField]
        public AudioResource AudioResource { get; private set; }

        [field: SerializeField]
        public float Volume { get; private set; } = 1f;

        private AudioManager m_audioManager;
        private void Start()
        {
            GlobalContext.RegisterPostInitializationCallback(context =>
            {
                m_audioManager = context.AudioManager;
            });
        }

        public void Play()
        {
            if (!m_audioManager)
            {
                Debug.LogError("No AudioManager found");
                return;
            }
            m_audioManager.Play2DSound(this);
        }

        public AudioSource PlayWithAudioSourceReturned()
        {
            if (!m_audioManager)
            {
                Debug.LogError("No AudioManager found");
                return null;
            }
            return m_audioManager.Play2DSound(this);
        }
    }
}