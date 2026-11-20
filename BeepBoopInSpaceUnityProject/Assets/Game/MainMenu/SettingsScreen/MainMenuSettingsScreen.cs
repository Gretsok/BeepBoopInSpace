using System;
using Game.Global;
using Game.Global.Localization;
using Game.Global.Save;
using Game.Global.Settings;
using Game.Global.SFXManagement;
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

        private SettingsManager m_settingsManager;
        private SaveManager m_saveManager;
        private Button m_fullscreenButton;
        private Button m_languageButton;
        private Button m_statisticsButton;
        // Screen.fullScreen only changes on the next frame: the button says what was asked.
        private bool m_isFullscreen;

        public void Initialize(SettingsManager settingsManager, SaveManager saveManager)
        {
            m_settingsManager = settingsManager;
            m_saveManager = saveManager;
        }

        protected override void HandleActivation()
        {
            DefaultFocus = BindVolume("master-volume", m_settingsManager.GetMasterVolume(), m_settingsManager.SetMasterVolume);
            BindVolume("music-volume", m_settingsManager.GetMusicVolume(), m_settingsManager.SetMusicVolume);
            BindVolume("sfx-volume", m_settingsManager.GetSfxVolume(), m_settingsManager.SetSfxVolume);
            BindButton("back-button", HandleBackRequested, m_backAudioPlayer);

            m_isFullscreen = Screen.fullScreen;
            m_fullscreenButton = BindButton("fullscreen-button", HandleFullscreenClicked, m_backAudioPlayer);
            m_languageButton = BindButton("language-button", HandleLanguageClicked, m_backAudioPlayer);
            m_statisticsButton = BindButton("statistics-button", HandleStatisticsClicked, m_backAudioPlayer);
            // No question and no statistics in the copies that don't send any.
            m_statisticsButton.style.display = GlobalContext.Instance.AnalyticsManager.IsAvailable ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshOptions();
        }

        private void RefreshOptions()
        {
            m_fullscreenButton.text = Loc.T(m_isFullscreen ? "Plein écran : oui" : "Plein écran : non");
            m_languageButton.text = Loc.T($"Langue : {(Loc.Language == ELanguage.French ? "Français" : "English")}");
            m_statisticsButton.text = Loc.T(GlobalContext.Instance.AnalyticsManager.IsConsentGiven ? "Statistiques : oui" : "Statistiques : non");
        }

        private void HandleFullscreenClicked()
        {
            m_isFullscreen = !m_isFullscreen;
            Screen.fullScreen = m_isFullscreen;
            RefreshOptions();
        }

        private void HandleLanguageClicked()
        {
            m_settingsManager.SetLanguage(Loc.Language == ELanguage.French ? ELanguage.English : ELanguage.French);
            m_saveManager.SaveProfile();
            Loc.Translate(Root);
            RefreshOptions();
        }

        private void HandleStatisticsClicked()
        {
            var analyticsManager = GlobalContext.Instance.AnalyticsManager;
            analyticsManager.SetConsent(!analyticsManager.IsConsentGiven);
            m_saveManager.SaveProfile();
            RefreshOptions();
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
