using System;
using DG.Tweening;
using Game.Global;
using Game.Global.InputIndicationsManagement;
using Game.Global.Settings;
using Game.UITools;
using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Game.Gameplay.PauseMenu
{
    /// <summary>
    /// The pause menu: its visual tree lives with the game, hidden until the pause. Resume, leave, and the volumes.
    /// B, Circle and Escape resume too.
    /// </summary>
    public class PauseMenuPanel : AUIToolkitScreen
    {
        private const int k_VolumeStep = 5;
        private const string k_HiddenClass = "pause--hidden";
        private const string k_FocusedRowClass = "volume-row--focused";
        private const string k_VolumeValueClass = "volume-row__value";

        [SerializeField]
        private PauseMenuManager m_manager;
        [SerializeField]
        private InputIndicationActionDataAsset m_backIndicationAction;

        private VisualElement m_pause;
        private Button m_resumeButton;
        private SliderInt m_masterVolume;
        private SliderInt m_musicVolume;
        private SliderInt m_sfxVolume;
        private bool m_isOpen;
        private int m_openingFrame = -1;

        private void Awake()
        {
            m_manager.OnPause += HandlePause;
            m_manager.OnResume += HandleResume;
        }

        private void OnDestroy()
        {
            if (m_manager)
            {
                m_manager.OnPause -= HandlePause;
                m_manager.OnResume -= HandleResume;
            }
        }

        protected override void HandleShown()
        {
            m_pause = Query<VisualElement>("pause");
            m_pause.EnableInClassList(k_HiddenClass, !m_isOpen);

            m_resumeButton = BindButton("resume-button", HandleResumeButtonClicked);
            // Leaving a game from the pause isn't implemented yet: the button stays, disabled.
            BindButton("back-to-menu-button", () => {}).SetEnabled(false);
            BindButton("quit-game-button", HandleQuitGameButtonClicked);
            BindButton("back-button", HandleResumeButtonClicked);
            AddIndication("back-glyph", m_backIndicationAction);

            m_masterVolume = BindVolume("master-volume", (settings, volume) => settings.SetMasterVolume(volume));
            m_musicVolume = BindVolume("music-volume", (settings, volume) => settings.SetMusicVolume(volume));
            m_sfxVolume = BindVolume("sfx-volume", (settings, volume) => settings.SetSfxVolume(volume));
        }

        protected override void HandleHidden()
        {
            m_pause?.KillTweens();
            m_pause = null;
        }

        protected override void HandleCancel()
        {
            // Escape both opens the pause and cancels: not on the frame it opened.
            if (m_isOpen && Time.frameCount != m_openingFrame)
                m_manager.Resume();
        }

        private void HandlePause(PauseMenuManager obj)
        {
            Show();
        }

        private void HandleResume(PauseMenuManager obj)
        {
            Hide();
        }

        private void Show()
        {
            m_isOpen = true;
            m_openingFrame = Time.frameCount;
            if (m_pause != null)
            {
                RefreshVolumes();
                m_pause.KillTweens();
                m_pause.RemoveFromClassList(k_HiddenClass);
                m_pause.TweenOpacity(0f, 1f, 0.05f).SetEase(Ease.InOutSine).SetLink(gameObject);
            }
            // Focused once its style shows it (AUIToolkitScreen gives the default focus back every frame).
            DefaultFocus = m_resumeButton;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }

        private void Hide()
        {
            m_isOpen = false;
            DefaultFocus = null;
            ReleaseFocus();
            if (m_pause != null)
            {
                var pause = m_pause;
                pause.KillTweens();
                pause.TweenOpacity(0f, 0.2f).SetEase(Ease.InOutSine).SetLink(gameObject).OnComplete(() =>
                {
                    pause.AddToClassList(k_HiddenClass);
                    pause.ResetTransform();
                });
            }

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void HandleResumeButtonClicked()
        {
            m_manager.Resume();
        }

        private void HandleQuitGameButtonClicked()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
            #else
            Application.Quit();
            #endif
        }

        private void RefreshVolumes()
        {
            var settings = GlobalContext.Instance ? GlobalContext.Instance.SettingsManager : null;
            if (!settings)
                return;
            ShowVolume(m_masterVolume, settings.GetMasterVolume());
            ShowVolume(m_musicVolume, settings.GetMusicVolume());
            ShowVolume(m_sfxVolume, settings.GetSfxVolume());
        }

        private static void ShowVolume(SliderInt slider, int volume)
        {
            slider.SetValueWithoutNotify(volume);
            slider.parent.Q<Label>(className: k_VolumeValueClass).text = volume.ToString();
        }

        private SliderInt BindVolume(string rowName, Action<SettingsManager, int> setVolume)
        {
            var row = Query<VisualElement>(rowName);
            var slider = row.Q<SliderInt>();
            var valueLabel = row.Q<Label>(className: k_VolumeValueClass);

            slider.RegisterValueChangedCallback(evt =>
            {
                valueLabel.text = evt.newValue.ToString();
                var globalContext = GlobalContext.Instance;
                if (!globalContext)
                    return;
                setVolume(globalContext.SettingsManager, evt.newValue);
                globalContext.SaveManager.SaveProfile();
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
