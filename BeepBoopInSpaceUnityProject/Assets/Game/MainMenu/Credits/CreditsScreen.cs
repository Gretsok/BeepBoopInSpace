using System;
using Game.Global.SFXManagement;
using Game.MainMenu.ZoneManagement;
using UnityEngine;

namespace Game.MainMenu.Credits
{
    public class CreditsScreen : AMainMenuScreen
    {
        [SerializeField]
        private AudioPlayer m_backAudioPlayer;

        private ZoneManager m_zoneManager;

        public Action OnBackRequested;

        public void Initialize(ZoneManager zoneManager)
        {
            m_zoneManager = zoneManager;
        }

        protected override void HandleActivation()
        {
            DefaultFocus = BindButton("back-button", HandleBackButtonClicked, m_backAudioPlayer);
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
