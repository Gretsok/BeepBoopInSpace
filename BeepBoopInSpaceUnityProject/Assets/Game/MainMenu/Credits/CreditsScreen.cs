using System;
using Game.MainMenu.CameraManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.MainMenu.Credits
{
    public class CreditsScreen : AMainMenuScreen
    {
        private CameraManager m_cameraManager;
        [SerializeField]
        private Button m_backButton;
        protected override void HandleActivation()
        {
            base.HandleActivation();
            
            gameObject.SetActive(true);
            
            m_backButton.onClick.AddListener(HandleBackButtonClicked);
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

        public void Initialize(CameraManager cameraManager)
        {
            m_cameraManager = cameraManager;
        }
    }
}