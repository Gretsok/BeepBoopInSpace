using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.MainMenu
{
    public class HomeScreen : AMainMenuScreen
    {
        [SerializeField] 
        private CanvasGroup m_container;

        [SerializeField] 
        private Button m_playButton;
        [SerializeField] 
        private Button m_creditsButton;
        [SerializeField] 
        private Button m_quitButton;

        protected override void HandleActivation()
        {
            gameObject.SetActive(true);
            
            m_playButton.onClick.AddListener(HandlePlayButtonClicked);
            m_creditsButton.onClick.AddListener(HandleCreditsButtonClicked);
            m_quitButton.onClick.AddListener(HandleQuitButtonClicked);
        }

        public Action OnPlayRequested;
        private void HandlePlayButtonClicked()
        {
            OnPlayRequested?.Invoke();
        }
        
        public Action OnCreditsRequested;
        private void HandleCreditsButtonClicked()
        {
            OnCreditsRequested?.Invoke();
        }
        
        private void HandleQuitButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        protected override void HandleDeactivation()
        {
            gameObject.SetActive(false);
            
            m_playButton.onClick.RemoveListener(HandlePlayButtonClicked);
            m_creditsButton.onClick.RemoveListener(HandleCreditsButtonClicked);
            m_quitButton.onClick.RemoveListener(HandleQuitButtonClicked);
        }
    }
}