using System;
using DG.Tweening;
using Game.Global;
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
        private const string k_HiddenClass = "pause--hidden";

        [SerializeField]
        private PauseMenuManager m_manager;

        private VisualElement m_pause;
        private Button m_resumeButton;
        private VolumeRow m_masterVolume;
        private VolumeRow m_musicVolume;
        private VolumeRow m_sfxVolume;
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
            BindButton("back-to-menu-button", HandleBackToMenuButtonClicked);
            BindButton("quit-game-button", HandleQuitGameButtonClicked);
            BindButton("back-button", HandleResumeButtonClicked);

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

        private void HandleBackToMenuButtonClicked()
        {
            m_manager.QuitToMainMenu();
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
            m_masterVolume.SetVolume(settings.GetMasterVolume());
            m_musicVolume.SetVolume(settings.GetMusicVolume());
            m_sfxVolume.SetVolume(settings.GetSfxVolume());
        }

        private VolumeRow BindVolume(string rowName, Action<SettingsManager, int> setVolume)
        {
            return new VolumeRow(Query<VisualElement>(rowName), volume =>
            {
                var globalContext = GlobalContext.Instance;
                if (!globalContext)
                    return;
                setVolume(globalContext.SettingsManager, volume);
                globalContext.SaveManager.SaveProfile();
            });
        }
    }
}
