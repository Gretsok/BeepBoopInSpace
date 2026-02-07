using Game.Global.Save;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Global.Settings
{
    /// <summary>
    /// Get or set options global settings.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        private SaveManager m_saveManager;
        
        public void Initialize(SaveManager saveManager)
        {
            m_saveManager = saveManager;
        }
        
#region Sounds

        [SerializeField]
        private AudioMixer m_audioMixer;
        
        public void SetMasterVolume(int volume)
        {
            volume = Mathf.Clamp(volume, 0, 100);
            m_saveManager.Profile.MasterVolume = volume;

            float db = math.log10(volume / 100f) * 20f;
            m_audioMixer.SetFloat("MasterVolume", db);
        }

        public int GetMasterVolume()
        {
            return m_saveManager.Profile.MasterVolume;
        }
        
        public void SetSfxVolume(int volume)
        {
            volume = Mathf.Clamp(volume, 0, 100);
            m_saveManager.Profile.SfxVolume = volume;

            float db = math.log10(volume / 100f) * 20f;
            m_audioMixer.SetFloat("SfxVolume", db);
        }

        public int GetSfxVolume()
        {
            return m_saveManager.Profile.SfxVolume;
        }

        public void SetMusicVolume(int volume)
        {
            volume = Mathf.Clamp(volume, 0, 100);
            m_saveManager.Profile.MusicVolume = volume;

            float db = math.log10(volume / 100f) * 20f;
            m_audioMixer.SetFloat("MusicVolume", db);
        }

        public int GetMusicVolume()
        {
            return m_saveManager.Profile.MusicVolume;
        }
#endregion Sounds
    }
}
