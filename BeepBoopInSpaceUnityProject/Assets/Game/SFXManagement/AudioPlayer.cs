using UnityEngine;
using UnityEngine.Audio;

namespace Game.SFXManagement
{
    public class AudioPlayer : MonoBehaviour
    {
        [field: SerializeField]
        public AudioResource AudioResource { get; private set; }

        [field: SerializeField]
        public float Volume { get; private set; } = 1f;

        public void Play()
        {
            if (!AudioManager.Instance)
            {
                Debug.LogError("No AudioManager found");
                return;
            }
            AudioManager.Instance.Play2DSound(this);
        }

        public AudioSource PlayWithAudioSourceReturned()
        {
            if (!AudioManager.Instance)
            {
                Debug.LogError("No AudioManager found");
                return null;
            }
            return AudioManager.Instance.Play2DSound(this);
        }
    }
}