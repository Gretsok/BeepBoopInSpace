using Game.ArchitectureTools.FlowMachine;
using TMPro;
using UnityEngine;

namespace Game.Gameplay.RoundsManagement
{
    public class NewRoundPanel : Panel
    {
        [SerializeField]
        private string m_roundText;
        [SerializeField]
        private TMP_Text m_text;

        public void SetText(string text)
        {
            m_text.text = text;
        }

        public void SetRoundText(int a_round)
        {
            m_text.text = string.Format(m_roundText, a_round);
        }
    }
}
