using System;
using Game.Global.InputIndicationsManagement;
using Game.Global.SFXManagement;
using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Gameplay.Flows.Results
{
    /// <summary>
    /// The button to leave the results, shown once the close-ups are over. It takes the focus: the main player's
    /// Submit (A, Cross or Enter) presses it, as does a click.
    /// </summary>
    public class ResultsPanel : AUIToolkitScreen
    {
        private const string k_ButtonShownClass = "results__continue--shown";

        [SerializeField]
        private AudioPlayer m_clickAudioPlayer;
        [SerializeField]
        private InputIndicationActionDataAsset m_submitIndicationAction;

        private Button m_backToMenuButton;
        private Action m_buttonCallback;

        protected override void HandleShown()
        {
            m_backToMenuButton = BindButton("back-to-menu-button", HandleBackToMenuButtonClicked, m_clickAudioPlayer);
            AddIndication("back-to-menu-glyph", m_submitIndicationAction);
            HideButton();
        }

        protected override void HandleHidden()
        {
            m_buttonCallback = null;
            m_backToMenuButton = null;
        }

        /// <summary>The results can be left.</summary>
        public bool IsButtonShown => m_buttonCallback != null;

        public void ShowButton(Action buttonCallback)
        {
            m_buttonCallback = buttonCallback;
            if (m_backToMenuButton == null)
                return;
            m_backToMenuButton.AddToClassList(k_ButtonShownClass);
            // Focused once its style shows it (AUIToolkitScreen gives the default focus back every frame).
            DefaultFocus = m_backToMenuButton;
        }

        /// <summary>Online guest: the button, greyed, says the host leaves the results.</summary>
        public void ShowWaitingForHost()
        {
            if (m_backToMenuButton == null)
                return;
            m_backToMenuButton.SetEnabled(false);
            m_backToMenuButton.Q<Label>().text = Game.Global.Localization.Loc.T("En attente de l'hôte…");
            m_backToMenuButton.Q("back-to-menu-glyph").style.display = DisplayStyle.None;
            m_backToMenuButton.AddToClassList(k_ButtonShownClass);
        }

        public void HideButton()
        {
            m_buttonCallback = null;
            if (m_backToMenuButton == null)
                return;
            m_backToMenuButton.RemoveFromClassList(k_ButtonShownClass);
            DefaultFocus = null;
            ReleaseFocus();
        }

        private void HandleBackToMenuButtonClicked()
        {
            var callback = m_buttonCallback;
            // Once only: the flow machine would take a second request too.
            HideButton();
            callback?.Invoke();
        }
    }
}
