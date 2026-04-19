using System.Collections;
using Game.ArchitectureTools.FlowMachine;
using Game.ArchitectureTools.Manager;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Gameplay.Flows.Results
{
    public class ResultsPanel : Panel
    {
        [SerializeField]
        private Button m_backToMenuButton;

        private void Awake()
        {
            HideButton();
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

        public void ManualClickButton()
        {
            ExecuteEvents.Execute(m_backToMenuButton.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }

        public void HideButton()
        {
            m_backToMenuButton.onClick.RemoveAllListeners();
            m_backToMenuButton.gameObject.SetActive(false);
        }
    }
}