using System;
using System.Collections.Generic;
using Game.Global.PlayerManagement;
using Game.Global.SFXManagement;
using Game.MainMenu.CharacterManagement;
using Game.MainMenu.ZoneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.MainMenu.CharacterSelection
{
    public class CharacterSelectionScreen : AMainMenuScreen
    {
        // Each widget drives the "player-slot-<n>" element of the screen, n starting at 1.
        [SerializeField] private List<CharacterWidget> m_widgets;

        private Button m_startGameButton;

        private PlayerManager m_playerManager;
        private ZoneManager m_zoneManager;
        private MainMenuOrchestrator m_orchestrator;

        [Header("Sounds")]
        [SerializeField]
        private AudioSource m_playerConnectedAudioSource;
        [SerializeField]
        private AudioPlayer m_startGameAudioPlayer;
        [SerializeField]
        private AudioPlayer m_backAudioPlayer;

        public void Initialize(ZoneManager zoneManager, PlayerManager playerManager, MainMenuOrchestrator orchestrator)
        {
            m_zoneManager = zoneManager;
            m_playerManager = playerManager;
            m_orchestrator = orchestrator;
        }

        protected override void HandleActivation()
        {
            m_playerManager.RemoveAllPlayers();

            m_playerManager.OnPlayerJoined += HandlePlayerJoined;
            m_playerManager.OnPlayerLeft += HandlePlayerLeft;

            BindButton("back-button", HandleBackButtonClicked, m_backAudioPlayer);
            m_startGameButton = BindButton("start-button", HandleStartGameButtonClicked, m_startGameAudioPlayer);

            for (int i = 0; i < m_widgets.Count; i++)
            {
                m_widgets[i].SetView(Query<VisualElement>($"player-slot-{i + 1}"));
                m_widgets[i].OnCharacterDataUpdated += HandleCharacterDataUpdated;
            }

            InflateWithPlayers();
            m_playerManager.ListenForNewPlayers();

            m_zoneManager.SwitchToCharacterSelectionCamera();
        }

        private void UpdateStartButtonState()
        {
            var canStart = m_playerManager.Players.Count >= 2 && m_widgets.TrueForAll(widget => widget.CanPlay || !widget.IsActivated);
            m_startGameButton.SetEnabled(canStart);

            // Nothing has the focus until the game can start: the button players press to join must not click anything.
            DefaultFocus = canStart ? m_startGameButton : null;
            if (canStart)
                FocusSilently(m_startGameButton);
            else if (FocusedElement == m_startGameButton)
                m_startGameButton.Blur();
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

        protected override void HandleCancel()
        {
            m_backAudioPlayer.Play();
            HandleBackButtonClicked();
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
                characterWidget.Activate();
                playerController.SetDependencies(player, characterWidget);
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
            if (!m_playerManager)
                return;

            m_playerManager.StopListeningForNewPlayers();

            m_playerManager.OnPlayerJoined -= HandlePlayerJoined;
            m_playerManager.OnPlayerLeft -= HandlePlayerLeft;

            for (int i = m_playerJoiningPlayers.Count - 1; i >= 0; i--)
            {
                var controller = m_playerJoiningPlayers[i];
                if (controller == null)
                    continue;
                controller.Deactivate();
                Destroy(controller);
            }
            m_playerJoiningPlayers.Clear();

            for (int i = 0; i < m_widgets.Count; i++)
            {
                var characterWidget = m_widgets[i];
                characterWidget.OnCharacterDataUpdated -= HandleCharacterDataUpdated;
                characterWidget.Deactivate();
            }
        }
    }
}
