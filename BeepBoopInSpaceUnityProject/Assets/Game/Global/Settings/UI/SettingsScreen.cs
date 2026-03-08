using System;
using Game.ArchitectureTools.Exception;
using Game.Global.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Global.Settings.UI
{
    public class SettingsScreen : MonoBehaviour
    {
        [SerializeField]
        private VolumeSliderWidget m_masterVolumeWidget;
        [SerializeField]
        private VolumeSliderWidget m_sfxVolumeWidget;
        [SerializeField]
        private VolumeSliderWidget m_musicVolumeWidget;
        
        [SerializeField]
        private Button m_backButton;

        private SettingsManager m_settingsManager;
        private SaveManager m_saveManager;
        
        private Action m_onBackRequested;
        
        private bool m_isInitialized;
        public void Initialize(SettingsManager settingsManager, SaveManager saveManager, Action onBackCallback)
        {
            m_settingsManager = settingsManager;
            m_saveManager = saveManager;
            
            m_onBackRequested = onBackCallback;
            
            m_masterVolumeWidget.Initialize(m_settingsManager.GetMasterVolume(), newValue =>
            {
                m_settingsManager.SetMasterVolume((int)newValue);
                m_saveManager.SaveProfile();
            });
            m_sfxVolumeWidget.Initialize(m_settingsManager.GetSfxVolume(), newValue =>
            {
                m_settingsManager.SetSfxVolume((int)newValue);
                m_saveManager.SaveProfile();
            });
            m_musicVolumeWidget.Initialize(m_settingsManager.GetMusicVolume(), newValue =>
            {
                m_settingsManager.SetMusicVolume((int)newValue);
                m_saveManager.SaveProfile();
            });
            
            m_isInitialized = true;
        }

        public void Show()
        {
            if (!m_isInitialized)
                throw new NotInitializedException();
            gameObject.SetActive(true);
            
            m_backButton.onClick.AddListener(HandleBackButtonClicked);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            m_backButton.onClick.RemoveListener(HandleBackButtonClicked);
        }

        private void HandleBackButtonClicked()
        {
            m_onBackRequested?.Invoke();
        }
    }
}
