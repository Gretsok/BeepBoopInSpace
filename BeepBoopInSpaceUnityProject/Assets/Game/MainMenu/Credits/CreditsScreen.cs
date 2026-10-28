using System;
using Game.Global.SFXManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.MainMenu.Credits
{
    public class CreditsScreen : AMainMenuScreen
    {
        [SerializeField]
        private AudioPlayer m_backAudioPlayer;


        public Action OnBackRequested;

        public void Initialize()
        {
        }

        protected override void HandleActivation()
        {
            BindButton("back-button", HandleBackButtonClicked, m_backAudioPlayer);
            // Nothing to move to: the screen itself keeps the focus, for Cancel to reach HandleCancel.
            var screen = Query<VisualElement>("credits-screen");
            screen.focusable = true;
            DefaultFocus = screen;
        }

        protected override void HandleCancel()
        {
            m_backAudioPlayer.Play();
            HandleBackButtonClicked();
        }

        private void HandleBackButtonClicked()
        {
            OnBackRequested?.Invoke();
        }
    }
}
