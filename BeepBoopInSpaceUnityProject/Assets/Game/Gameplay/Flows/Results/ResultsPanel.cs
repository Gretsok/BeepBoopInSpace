using System.Collections;
using Game.ArchitectureTools.Manager;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Gameplay.Flows.Results
{
    public class ResultsPanel : AManager<ResultsPanel>
    {
        [SerializeField]
        private Button m_backToMenuButton;

        protected override IEnumerator Initialize()
        {
            HideButton();
            return base.Initialize();
        }

        private void OnEnable()
        {
            HideButton();
        }

        private void OnDisable()
        {
            HideButton();
        }

        public void ShowButton(UnityAction buttonCallback)
        {
            m_backToMenuButton.onClick.AddListener(buttonCallback);
            m_backToMenuButton.gameObject.SetActive(true);
        }

        public void HideButton()
        {
            m_backToMenuButton.onClick.RemoveAllListeners();
            m_backToMenuButton.gameObject.SetActive(false);
        }
    }
}