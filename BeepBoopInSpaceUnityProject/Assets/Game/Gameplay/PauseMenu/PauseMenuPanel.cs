using System;
using DG.Tweening;
using Game.Global;
using Game.Global.Settings.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.PauseMenu
{
    public class PauseMenuPanel : MonoBehaviour
    {
        [SerializeField]
        private PauseMenuManager m_manager;
        [Header("----- VIEW -----")]
        [SerializeField] 
        private CanvasGroup m_container;
        [SerializeField]
        private SettingsScreen m_settingsScreen;
        
        [Header("Buttons")]
        [SerializeField]
        private Button m_resumeButton;
        [SerializeField]
        private Button m_backToMenuButton;
        [SerializeField] 
        private Button m_quitGameButton;


        private void Awake()
        {
            m_manager.OnPause += HandlePause;
            m_manager.OnResume += HandleResume;
            
            m_container.DOKill();
            m_container.DOFade(0f, 0f);
            m_container.gameObject.SetActive(false);
            
            GlobalContext.RegisterPostInitializationCallback(globalContext =>
            {
                m_settingsScreen.Initialize(globalContext.SettingsManager, globalContext.SaveManager, null);    
            });
        }

        private void HandleResume(PauseMenuManager obj)
        {
            Hide();
        }

        private void HandlePause(PauseMenuManager obj)
        {
            Show();
        }

        private void Show()
        {
            RegisterEvents();
            m_container.gameObject.SetActive(true);
            m_container.DOKill();
            m_container.DOFade(1f, 0.05f).SetEase(Ease.InOutSine);
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
        
        private void Hide()
        {
            UnregisterEvents();
            m_container.DOKill();
            m_container.DOFade(0f, 0.2f).SetEase(Ease.InOutSine).onComplete += () =>
            {
                m_container.gameObject.SetActive(false);
            };
            
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void RegisterEvents()
        {
            m_resumeButton.onClick.AddListener(HandleResumeButtonClicked);
            m_backToMenuButton.onClick.AddListener(HandleBackToMenuButtonClicked);
            m_quitGameButton.onClick.AddListener(HandleQuitGameButtonClicked);
        }

        private void UnregisterEvents()
        {
            m_resumeButton.onClick.RemoveListener(HandleResumeButtonClicked);
            m_backToMenuButton.onClick.RemoveListener(HandleBackToMenuButtonClicked);
            m_quitGameButton.onClick.RemoveListener(HandleQuitGameButtonClicked);
        }
        
        private void HandleResumeButtonClicked()
        {
            m_manager.Resume();
        }
        
        private void HandleBackToMenuButtonClicked()
        {
            throw new NotImplementedException();
        }

        private void HandleQuitGameButtonClicked()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
            #else
            Application.Quit();
            #endif
        }
    }
}