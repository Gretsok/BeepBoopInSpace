using System;
using Game.Global.Save;
using Game.Global.Settings;
using Game.Global.SFXManagement;
using Game.MainMenu.ZoneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.MainMenu.SettingsScreen
{
    public class MainMenuSettingsScreen : AMainMenuScreen
    {
        private const int k_VolumeStep = 5;
        private const string k_FocusedRowClass = "settings-row--focused";

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
            var row = Query<VisualElement>(rowName);
            var slider = row.Q<SliderInt>();
            var valueLabel = row.Q<Label>(className: "settings-row__value");

            slider.SetValueWithoutNotify(volume);
            valueLabel.text = volume.ToString();
            slider.RegisterValueChangedCallback(evt =>
            {
                valueLabel.text = evt.newValue.ToString();
                setVolume(evt.newValue);
                m_saveManager.SaveProfile();
            });

            // Left and right change the volume right away (Unity's sliders wait for a Submit first),
            // up and down move to the other rows.
            slider.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                var step = evt.direction switch
                {
                    NavigationMoveEvent.Direction.Left => -k_VolumeStep,
                    NavigationMoveEvent.Direction.Right => k_VolumeStep,
                    _ => 0,
                };
                if (step == 0)
                    return;
                slider.value = Mathf.Clamp(slider.value + step, slider.lowValue, slider.highValue);
                evt.StopPropagation();
                slider.focusController?.IgnoreEvent(evt);
            }, TrickleDown.TrickleDown);
            slider.RegisterCallback<NavigationSubmitEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);

            row.RegisterCallback<FocusInEvent>(_ => row.AddToClassList(k_FocusedRowClass));
            row.RegisterCallback<FocusOutEvent>(_ => row.RemoveFromClassList(k_FocusedRowClass));
            return slider;
        }
    }
}
