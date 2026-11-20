using Game.Global;
using Game.Global.Localization;
using Game.Global.SFXManagement;
using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.MainMenu.Home
{
    public class HomeScreen : AMainMenuScreen
    {
        [SerializeField]
        private AudioPlayer m_playAudioPlayer;

        private MainMenuOrchestrator m_mainMenuOrchestrator;
        private ConfirmationDialog m_consent;

        public void Initialize(MainMenuOrchestrator orchestrator)
        {
            m_mainMenuOrchestrator = orchestrator;
        }

        protected override void HandleActivation()
        {
            DefaultFocus = BindButton("play-button", HandlePlayButtonClicked, m_playAudioPlayer);
            BindButton("credits-button", HandleCreditsButtonClicked, m_playAudioPlayer);
            BindButton("settings-button", HandleSettingsButtonClicked);
            BindButton("quit-button", HandleQuitButtonClicked);
            Query<Label>("version-label").text = Loc.T($"Démo {Application.version}");

            var analyticsManager = GlobalContext.Instance.AnalyticsManager;
            m_consent = new ConfirmationDialog(Query<VisualElement>("consent"), () => AnswerConsent(true), () => AnswerConsent(false));
            m_consent.ConfirmButton.text = Loc.T("Oui");
            m_consent.CancelButton.text = Loc.T("Non");
            if (analyticsManager.HasToAskConsent)
            {
                ShowDialog(m_consent);
                // Starting on No: nobody agrees by pressing on without reading.
                DefaultFocus = m_consent.CancelButton;
            }
        }

        private void AnswerConsent(bool isGiven)
        {
            GlobalContext.Instance.AnalyticsManager.SetConsent(isGiven);
            GlobalContext.Instance.SaveManager.SaveProfile();
            HideDialog();
        }

        private void HandlePlayButtonClicked()
        {
            m_mainMenuOrchestrator.SwitchToCharacterSelectionScreen();
        }

        private void HandleCreditsButtonClicked()
        {
            m_mainMenuOrchestrator.SwitchToCreditsScreen();
        }

        private void HandleQuitButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        private void HandleSettingsButtonClicked()
        {
            m_mainMenuOrchestrator.SwitchToSettingsScreen();
        }
    }
}
