using System;
using Game.Global.Save;
using Game.Global.Settings;
using Game.Global.SFXManagement;
using Game.MainMenu.ZoneManagement;
using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.MainMenu.SettingsScreen
{
    public class MainMenuSettingsScreen : AMainMenuScreen
    {
        [SerializeField]
        private AudioPlayer m_backAudioPlayer;

        public event Action OnBackRequested;

        private ZoneManager m_zoneManager;
        private SettingsManager m_settingsManager;
        private SaveManager m_saveManager;

        public void Initialize(ZoneManager zoneManager, SettingsManager settingsManager, SaveManager saveManager)
        {
            m_zoneManager = zoneManager;
            m_settingsManager = settingsManager;
            m_saveManager = saveManager;
        }

        protected override void HandleActivation()
        {
            DefaultFocus = BindVolume("master-volume", m_settingsManager.GetMasterVolume(), m_settingsManager.SetMasterVolume);
            BindVolume("music-volume", m_settingsManager.GetMusicVolume(), m_settingsManager.SetMusicVolume);
            BindVolume("sfx-volume", m_settingsManager.GetSfxVolume(), m_settingsManager.SetSfxVolume);
            BindButton("back-button", HandleBackRequested, m_backAudioPlayer);

            m_zoneManager.SwitchToOptionsCamera();
        }

        protected override void HandleCancel()
        {
            m_backAudioPlayer.Play();
            HandleBackRequested();
        }

        private void HandleBackRequested()
        {
            OnBackRequested?.Invoke();
        }

        private SliderInt BindVolume(string rowName, int volume, Action<int> setVolume)
        {
            var row = new VolumeRow(Query<VisualElement>(rowName), newVolume =>
            {
                setVolume(newVolume);
                m_saveManager.SaveProfile();
            });
            row.SetVolume(volume);
            return row.Slider;
        }
    }
}
