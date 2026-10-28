using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Gameplay.RoundsManagement
{
    public class NewRoundPanel : BannerScreen
    {
        [SerializeField]
        private string m_roundText;

        private string m_text;

        // Set by the round announcement before it shows the panel: kept until the visual tree exists.
        public void SetText(string text)
        {
            m_text = text;
            RefreshText();
        }

        public void SetRoundText(int a_round)
        {
            SetText(string.Format(m_roundText, a_round));
        }

        protected override void HandleShown()
        {
            RefreshText();
            base.HandleShown();
        }

        private void RefreshText()
        {
            if (IsShown && m_text != null)
                Query<Label>("round-label").text = m_text.ToUpperInvariant();
        }
    }
}
