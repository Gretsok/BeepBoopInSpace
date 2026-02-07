using System;
using Game.Global.Save;
using Game.Global.Settings;
using UnityEngine;

namespace Game.MainMenu.SettingsScreen
{
    public class MainMenuSettingsScreen : AMainMenuScreen
    {
        [SerializeField]
        private Global.Settings.UI.SettingsScreen m_settingsScreen;

        public event Action OnBackRequested;

        public void Initialize(SettingsManager settingsManager, SaveManager saveManager)
        {
            m_settingsScreen.Initialize(settingsManager, saveManager, () => OnBackRequested?.Invoke());
        }
        protected override void HandleActivation()
        {
            base.HandleActivation();
            gameObject.SetActive(true);
            m_settingsScreen.Show();
        }

        protected override void HandleDeactivation()
        {
            base.HandleDeactivation();
            gameObject.SetActive(false);
            m_settingsScreen.Hide();
        }
    }
}
