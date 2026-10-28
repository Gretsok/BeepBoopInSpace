using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Global.InputIndicationsManagement;
using Game.Global.PlayerManagement;
using Game.Global.SFXManagement;
using Game.MainMenu.CharacterManagement;
using Game.MainMenu.ZoneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Game.MainMenu.CharacterSelection
{
    /// <summary>
    /// Players join, each picks a character in the grid and gets ready; once every player (2 at least) is ready, the
    /// game moves on to the hub. Each player's circle is a <see cref="CharacterWidget"/>, the grid a <see cref="CharacterRoster"/>.
    /// </summary>
    public class CharacterSelectionScreen : AMainMenuScreen
    {
        private const int k_MinPlayers = 2;
        private const string k_LaunchingClass = "cs-screen--launching";

        // Each widget drives the "player-slot-<n>" element of the screen, n starting at 1.
        [SerializeField] private List<CharacterWidget> m_widgets;

        [SerializeField, Tooltip("Cells of the character grid. Those past the characters show locked robots.")]
        private int m_rosterCellCount = 20;

        [SerializeField, Tooltip("Time between the last player getting ready and the hub, in seconds.")]
        private float m_launchDelay = 1.2f;

        // A texture, not a sprite: the menus' atlases are tightly packed, and UI Toolkit can draw the neighbors of a
        // sprite in its transparent corners.
        [SerializeField, Tooltip("Grey robot of the grid cells without a character, and of the characters without a picture.")]
        private Texture2D m_missingCharacterIcon;

        [Header("Input indications")]
        [SerializeField] private InputIndicationActionDataAsset m_randomIndicationAction;
        [SerializeField] private InputIndicationActionDataAsset m_callLeftIndicationAction;
        [SerializeField] private InputIndicationActionDataAsset m_callRightIndicationAction;
        [SerializeField] private InputIndicationActionDataAsset m_readyIndicationAction;

        private PlayerManager m_playerManager;
        private ZoneManager m_zoneManager;
        private MainMenuOrchestrator m_orchestrator;

        private VisualElement m_screenElement;
        private CharacterRoster m_roster;
        private readonly List<CharacterSelectionPlayerController> m_playerControllers = new();
        private InputAction m_joinAction;
        private Tween m_launch;

        [Header("Sounds")]
        [SerializeField]
        private AudioSource m_playerConnectedAudioSource;
        [SerializeField]
        private AudioPlayer m_startGameAudioPlayer;
        [SerializeField]
        private AudioPlayer m_backAudioPlayer;
        [SerializeField]
        private AudioPlayer m_readyAudioPlayer;
        [SerializeField]
        private AudioPlayer m_unreadyAudioPlayer;
        [SerializeField]
        private AudioPlayer m_refusedAudioPlayer;

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

            // Nothing on this screen is a button players move to: the screen itself keeps the focus, so that the main
            // player's Cancel reaches HandleCancel.
            m_screenElement = Query<VisualElement>("character-selection-screen");
            m_screenElement.focusable = true;
            DefaultFocus = m_screenElement;
            BindButton("back-button", HandleBackButtonClicked, m_backAudioPlayer);

            m_roster = new CharacterRoster(Query<VisualElement>("roster"), CharacterBankManager.Instance, m_rosterCellCount, m_missingCharacterIcon);
            for (int i = 0; i < m_widgets.Count; i++)
            {
                var widget = m_widgets[i];
                widget.SetView(Query<VisualElement>($"player-slot-{i + 1}"), i, m_missingCharacterIcon);
                widget.OnCharacterDataUpdated += HandleCharacterDataUpdated;
                widget.OnReadyChanged += HandleReadyChanged;
                widget.OnRefused += HandleRefused;
            }

            AddIndication("random-glyph", m_randomIndicationAction);
            AddIndication("call-left-glyph", m_callLeftIndicationAction);
            AddIndication("call-right-glyph", m_callRightIndicationAction);
            AddIndication("ready-glyph", m_readyIndicationAction);

            m_joinAction = CreateJoinAction();
            InflateWithPlayers();

            m_zoneManager.SwitchToCharacterSelectionCamera();
        }

        // Players join with the buttons of the "Rejoindre" and "Prêts ?" indications, or A. Not with any button: B on a
        // gamepad that hasn't joined goes back to the home screen.
        private static InputAction CreateJoinAction()
        {
            var action = new InputAction("Join", InputActionType.Button);
            action.AddBinding("<Gamepad>/buttonNorth");
            action.AddBinding("<Gamepad>/buttonSouth");
            action.AddBinding("<Keyboard>/space");
            action.AddBinding("<Keyboard>/enter");
            return action;
        }

        private CharacterWidget WidgetOf(AbstractPlayer player) => m_widgets.FirstOrDefault(widget => widget.IsActivated && widget.Player == player);

        private void HandleCharacterDataUpdated()
        {
            m_roster.Refresh(m_widgets);
        }

        private void HandleReadyChanged(CharacterWidget widget)
        {
            if (widget.IsReady)
            {
                m_readyAudioPlayer.Play();
                m_roster.PlayTaken(widget.CharacterDataAsset);
            }
            else
                m_unreadyAudioPlayer.Play();
            m_roster.Refresh(m_widgets);
            UpdateLaunch();
        }

        private void HandleRefused(CharacterWidget widget)
        {
            m_refusedAudioPlayer.Play();
            m_roster.PlayRefused(widget.CharacterDataAsset);
        }

        private bool AreAllPlayersReady => m_playerManager.Players.Count >= k_MinPlayers
                                           && m_widgets.TrueForAll(widget => widget.CanPlay || !widget.IsActivated);

        // Everybody ready: the hub opens after a short celebration, unless someone changes their mind or joins meanwhile.
        private void UpdateLaunch()
        {
            var isLaunching = m_launch != null && m_launch.IsActive();
            if (AreAllPlayersReady == isLaunching)
                return;
            m_screenElement.EnableInClassList(k_LaunchingClass, !isLaunching);
            if (isLaunching)
            {
                m_launch.Kill();
                m_launch = null;
                return;
            }

            for (int i = 0; i < m_widgets.Count; i++)
                m_widgets[i].PlayLaunch(0.35f + i * 0.08f);
            m_launch = DOVirtual.DelayedCall(m_launchDelay, Launch, ignoreTimeScale: true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void Launch()
        {
            m_launch = null;
            if (!IsActivated || !AreAllPlayersReady)
                return;
            m_startGameAudioPlayer.Play();
            m_playerManager.StopListeningForNewPlayers();
            m_orchestrator.SwitchToHubScreen();
        }

        public Action OnBack;
        private void HandleBackButtonClicked()
        {
            OnBack?.Invoke();
        }

        // B, Circle or Escape of the main player: first cancels being ready, then goes back to the home screen.
        protected override void HandleCancel()
        {
            var mainPlayerWidget = WidgetOf(MainPlayer);
            if (mainPlayerWidget && mainPlayerWidget.IsReady)
            {
                mainPlayerWidget.CancelReady();
                return;
            }
            m_backAudioPlayer.Play();
            HandleBackButtonClicked();
        }

        private void HandlePlayerJoined(PlayerManager playerManager, AbstractPlayer abstractPlayer)
        {
            InflateWithPlayers();
            m_playerConnectedAudioSource.Play();
        }

        private void HandlePlayerLeft(PlayerManager playerManager, AbstractPlayer abstractPlayer)
        {
            if (!gameObject)
                return;
            InflateWithPlayers();
        }

        private void InflateWithPlayers()
        {
            ClearPlayerControllers();

            var players = m_playerManager.Players;
            var mainPlayer = MainPlayer;
            for (int i = 0; i < m_widgets.Count; i++)
            {
                var widget = m_widgets[i];
                if (i >= players.Count)
                {
                    widget.Deactivate();
                    widget.SetJoinSlot(i == players.Count);
                    widget.SetJoinIndicationPlayer(mainPlayer);
                    continue;
                }

                if (widget.Player != players[i])
                {
                    widget.Deactivate();
                    widget.SetPlayer(players[i]);
                    widget.Activate();
                }
                var playerController = gameObject.AddComponent<CharacterSelectionPlayerController>();
                playerController.SetDependencies(players[i], widget, m_roster, handlesBack: players[i] != mainPlayer);
                playerController.Activate();
                m_playerControllers.Add(playerController);
            }

            // Four players at most: one per circle.
            if (players.Count >= m_widgets.Count)
                m_playerManager.StopListeningForNewPlayers();
            else if (IsActivated && m_joinAction != null)
                m_playerManager.ListenForNewPlayers(m_joinAction);

            RefreshIndications();
            m_roster.Refresh(m_widgets);
            UpdateLaunch();
        }

        private void ClearPlayerControllers()
        {
            foreach (var controller in m_playerControllers)
            {
                if (controller == null)
                    continue;
                controller.Deactivate();
                Destroy(controller);
            }
            m_playerControllers.Clear();
        }

        protected override void HandleDeactivation()
        {
            if (!m_playerManager)
                return;

            m_playerManager.StopListeningForNewPlayers();
            m_joinAction?.Dispose();
            m_joinAction = null;

            m_playerManager.OnPlayerJoined -= HandlePlayerJoined;
            m_playerManager.OnPlayerLeft -= HandlePlayerLeft;

            m_launch?.Kill();
            m_launch = null;
            ClearPlayerControllers();

            for (int i = 0; i < m_widgets.Count; i++)
            {
                var characterWidget = m_widgets[i];
                characterWidget.OnCharacterDataUpdated -= HandleCharacterDataUpdated;
                characterWidget.OnReadyChanged -= HandleReadyChanged;
                characterWidget.OnRefused -= HandleRefused;
                characterWidget.Deactivate();
                characterWidget.SetJoinSlot(false);
            }
        }
    }
}
