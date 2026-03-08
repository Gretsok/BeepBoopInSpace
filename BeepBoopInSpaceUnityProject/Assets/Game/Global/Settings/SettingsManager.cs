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
            
            SetMasterVolume(saveManager.Profile.MasterVolume);
            SetSfxVolume(saveManager.Profile.SfxVolume);
            SetMusicVolume(saveManager.Profile.MusicVolume);
        }
        
#region Sounds

        [SerializeField]
        private AudioMixer m_audioMixer;
        
        public void SetMasterVolume(int volume)
        {
            volume = Mathf.Clamp(volume, 0, 100);
            m_saveManager.Profile.MasterVolume = volume;

            if (volume == 0)
            {
                m_audioMixer.SetFloat("MasterVolume", -80);
                return;
            }
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

            if (volume == 0)
            {
                m_audioMixer.SetFloat("SfxVolume", -80);
                return;
            }
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

            if (volume == 0)
            {
                m_audioMixer.SetFloat("MusicVolume", -80);
                return;
            }
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
