using System;
using System.Collections.Generic;
using Game.PlayerManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.MainMenu
{
    public class PlayerJoiningScreen : AMainMenuScreen
    {
        [SerializeField] 
        private CanvasGroup m_container;

        [SerializeField] private CharacterWidget m_aeroWidget;
        [SerializeField] private CharacterWidget m_miloWidget;
        [SerializeField] private CharacterWidget m_turbyoWidget;
        [SerializeField] private CharacterWidget m_dynamoWidget;

        [SerializeField] private Button m_backButton;
        [SerializeField] private Button m_startGameButton;
        
        private PlayerManager m_playerManager;
        
        protected override void HandleActivation()
        {
            gameObject.SetActive(true);

            m_playerManager = PlayerManager.Instance;

            m_playerManager.RemoveAllPlayers();
            
            m_playerManager.OnPlayerJoined += HandlePlayerJoined;
            m_playerManager.OnPlayerLeft += HandlePlayerLeft;
            
            m_backButton.onClick.AddListener(HandleBackButtonClicked);
            m_startGameButton.onClick.AddListener(HandleStartGameButtonClicked);
            
            InflateWithPlayers();
            m_playerManager.ListenForNewPlayers();
        }

        private void HandleStartGameButtonClicked()
        {
            SceneManager.LoadSceneAsync(1);
        }

        public Action OnBack;
        private void HandleBackButtonClicked()
        {
            OnBack?.Invoke();
        }

        private void HandlePlayerJoined(PlayerManager playerManager, AbstractPlayer abstractPlayer)
        {
            InflateWithPlayers();
        }
        
        private void HandlePlayerLeft(PlayerManager playerManager, AbstractPlayer abstractPlayer)
        {
            if (!gameObject)
                return;
            InflateWithPlayers();
        }

        private List<PlayerJoiningPlayerController> m_playerJoiningPlayers = new ();
        private void InflateWithPlayers()
        {
            for (int i = m_playerJoiningPlayers.Count - 1; i >= 0; i--)
            {
                var controller = m_playerJoiningPlayers[i];
                controller.Deactivate();
                Destroy(controller);
            }
            m_playerJoiningPlayers.Clear();
            
            for (int i = 0; i < m_playerManager.Players.Count; i++)
            {
                var player = m_playerManager.Players[i];
                
                var playerController = gameObject.AddComponent<PlayerJoiningPlayerController>();
                var characterWidget = GetCharacterWidgetFor(i);
                playerController.SetDependencies(player, characterWidget);
                playerController.Activate();
                characterWidget.Activate();
                m_playerJoiningPlayers.Add(playerController);
            }

            for (int i = m_playerManager.Players.Count; i < 4; i++)
            {
                var characterWidget = GetCharacterWidgetFor(i);
                
                characterWidget.Deactivate();
            }
        }

        protected override void HandleDeactivation()
        {
            gameObject.SetActive(false);
            
            if (!m_playerManager) 
                return;
            
            m_playerManager.StopListeningForNewPlayers();
                
            m_playerManager.OnPlayerJoined -= HandlePlayerJoined;
            m_playerManager.OnPlayerLeft -= HandlePlayerLeft;
            
                        
            m_backButton.onClick.RemoveListener(HandleBackButtonClicked);
            m_startGameButton.onClick.RemoveListener(HandleStartGameButtonClicked);
            
            for (int i = m_playerJoiningPlayers.Count - 1; i >= 0; i--)
            {
                var controller = m_playerJoiningPlayers[i];
                if (controller == null)
                    continue;
                controller.Deactivate();
                Destroy(controller);
            }
        }

        private CharacterWidget GetCharacterWidgetFor(int index)
        {
            switch (index)
            {
                case 0:
                    return m_aeroWidget;
                case 1:
                    return m_miloWidget;
                case 2:
                    return m_turbyoWidget;
                case 3:
                    return m_dynamoWidget;
            }
            return null;
        }
    }
}