using System;
using System.Collections.Generic;
using Game.Global.PlayerManagement;
using Game.MainMenu.ZoneManagement;
using Game.PlayerManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.MainMenu.CharacterSelection
{
    public class CharacterSelectionScreen : AMainMenuScreen
    {
        [SerializeField] 
        private CanvasGroup m_container;

        [SerializeField] private List<CharacterWidget> m_widgets;

        [SerializeField] private Button m_backButton;
        [SerializeField] private Button m_startGameButton;
        
        private PlayerManager m_playerManager;
        private ZoneManager m_zoneManager;
        private MainMenuOrchestrator m_orchestrator;

        [Header("Sounds")]
        [SerializeField]
        private AudioSource m_playerConnectedAudioSource;

        public void Initialize(ZoneManager zoneManager, PlayerManager playerManager, MainMenuOrchestrator orchestrator)
        {
            m_zoneManager = zoneManager;
            m_playerManager = playerManager;
            m_orchestrator = orchestrator;
        }
        
        protected override void HandleActivation()
        {
            gameObject.SetActive(true);

            m_playerManager.RemoveAllPlayers();
            
            m_playerManager.OnPlayerJoined += HandlePlayerJoined;
            m_playerManager.OnPlayerLeft += HandlePlayerLeft;
            
            m_backButton.onClick.AddListener(HandleBackButtonClicked);
            m_startGameButton.onClick.AddListener(HandleStartGameButtonClicked);

            m_widgets.ForEach(widget => widget.OnCharacterDataUpdated += HandleCharacterDataUpdated);
            
            InflateWithPlayers();
            m_playerManager.ListenForNewPlayers();
            
            UpdateStartButtonState();
            
            m_zoneManager.SwitchToCharacterSelectionCamera();
        }

        private void UpdateStartButtonState()
        {
            m_startGameButton.interactable = m_playerManager.Players.Count >= 2 && m_widgets.TrueForAll(widget => widget.CanPlay || !widget.IsActivated);
        }

        private void HandleCharacterDataUpdated()
        {
            InflateWithPlayers();
        }

        private void HandleStartGameButtonClicked()
        {
            if (m_playerManager.Players.Count < 2)
                return;
            
            m_orchestrator.SwitchToHubScreen();
        }

        public Action OnBack;
        private void HandleBackButtonClicked()
        {
            OnBack?.Invoke();
        }

        private void HandlePlayerJoined(PlayerManager playerManager, AbstractPlayer abstractPlayer)
        {
            UpdateStartButtonState();
            InflateWithPlayers();
            m_playerConnectedAudioSource.Play();
        }
        
        private void HandlePlayerLeft(PlayerManager playerManager, AbstractPlayer abstractPlayer)
        {
            if (!gameObject)
                return;
            UpdateStartButtonState();
            InflateWithPlayers();
        }

        private List<CharacterSelectionPlayerController> m_playerJoiningPlayers = new ();

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
                
                var playerController = gameObject.AddComponent<CharacterSelectionPlayerController>();
                var characterWidget = m_widgets[i];
                playerController.SetDependencies(player, characterWidget);
                characterWidget.Activate();
                playerController.Activate();
                m_playerJoiningPlayers.Add(playerController);
            }
            
            for (int i = 0; i < m_playerManager.Players.Count; i++)
            {
                var characterWidget = m_widgets[i];
                characterWidget.UpdateModel();
            }

            for (int i = m_playerManager.Players.Count; i < 4; i++)
            {
                var characterWidget = m_widgets[i];
                
                characterWidget.Deactivate();
            }
            
            UpdateStartButtonState();
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

    }
}