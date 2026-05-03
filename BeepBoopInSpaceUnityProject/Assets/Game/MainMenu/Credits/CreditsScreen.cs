using System;
using Game.MainMenu.ZoneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.MainMenu.Credits
{
    public class CreditsScreen : AMainMenuScreen
    {
        private ZoneManager m_zoneManager;
        [SerializeField]
        private Button m_backButton;
        protected override void HandleActivation()
        {
            base.HandleActivation();
            
            gameObject.SetActive(true);
            
            m_backButton.onClick.AddListener(HandleBackButtonClicked);
            EventSystem.current.SetSelectedGameObject(m_backButton.gameObject);
        }

        public Action OnBackRequested;

        private void HandleBackButtonClicked()
        {
            OnBackRequested?.Invoke();
        }

        protected override void HandleDeactivation()
        {
            base.HandleDeactivation();
            
            gameObject.SetActive(false);
            
            m_backButton.onClick.RemoveListener(HandleBackButtonClicked);
        }

        public void Initialize(ZoneManager zoneManager)
        {
            m_zoneManager = zoneManager;
        }
    }
}