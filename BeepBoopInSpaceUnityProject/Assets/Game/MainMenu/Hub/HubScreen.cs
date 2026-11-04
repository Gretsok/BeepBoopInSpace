using Game.Global;
using Game.Global.SFXManagement;
using Game.Online;
using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;
using Direction = UnityEngine.UIElements.NavigationMoveEvent.Direction;

namespace Game.MainMenu.Hub
{
    /// <summary>
    /// The game modes: Tournoi and Entraînement for the players of this machine (two at least), En ligne for a room with
    /// players of other machines. A room lost while playing (the host left...) comes back here, with a word on why.
    /// </summary>
    public class HubScreen : AMainMenuScreen
    {
        private const int k_MinLocalPlayers = 2;

        [SerializeField]
        private AudioPlayer m_confirmAudioPlayer;
        [SerializeField]
        private AudioPlayer m_backAudioPlayer;

        private Button m_tournamentButton;
        private Button m_trainingButton;
        private Button m_onlineButton;
        private ConfirmationDialog m_tournamentConfirmation;
        private ConfirmationDialog m_notice;

        private GameModesLauncher m_gameModesLauncher;
        private MainMenuOrchestrator m_orchestrator;

        public void Initialize(GameModesLauncher gameModesLauncher, MainMenuOrchestrator mainMenuOrchestrator)
        {
            m_gameModesLauncher = gameModesLauncher;
            m_orchestrator = mainMenuOrchestrator;
        }

        protected override void HandleActivation()
        {
            m_tournamentButton = BindButton("tournament-button", HandleTournamentButtonClicked, m_confirmAudioPlayer);
            m_trainingButton = BindButton("training-button", HandleTrainingButtonClicked, m_confirmAudioPlayer);
            m_onlineButton = BindButton("online-button", HandleOnlineButtonClicked, m_confirmAudioPlayer);
            var settingsButton = BindButton("settings-button", HandleSettingsButtonClicked, m_confirmAudioPlayer);
            var characterSelectionButton = BindButton("character-selection-button", HandleCharacterSelectionButtonClicked, m_confirmAudioPlayer);
            var quitButton = BindButton("quit-button", HandleQuitGameButtonClicked);
            m_tournamentConfirmation = new ConfirmationDialog(Query<VisualElement>("tournament-confirmation"),
                HandleTournamentConfirmed, HandleTournamentCancelled);
            m_notice = new ConfirmationDialog(Query<VisualElement>("notice"), HideNotice, HideNotice);
            m_notice.CancelButton.style.display = DisplayStyle.None;
            m_notice.ConfirmButton.text = "OK";

            // The local modes need two players of this machine: alone, the online modes.
            var canPlayLocally = GlobalContext.Instance.PlayerManager.Players.Count >= k_MinLocalPlayers;
            m_tournamentButton.SetEnabled(canPlayLocally);
            m_trainingButton.SetEnabled(canPlayLocally);
            var requirementDisplay = canPlayLocally ? DisplayStyle.None : DisplayStyle.Flex;
            Query<Label>("tournament-requirement").style.display = requirementDisplay;
            Query<Label>("training-requirement").style.display = requirementDisplay;
            var firstMode = canPlayLocally ? m_tournamentButton : m_onlineButton;

            // Three rows: the corner buttons, the modes, the character selection. The closest element ahead would
            // otherwise jump from a mode to a corner button, Quitter on the right of En ligne.
            SetNavigation(settingsButton, Direction.Right, quitButton);
            SetNavigation(settingsButton, Direction.Down, firstMode);
            SetNavigation(quitButton, Direction.Left, settingsButton);
            SetNavigation(quitButton, Direction.Down, m_onlineButton);
            SetNavigation(m_tournamentButton, Direction.Left, null);
            SetNavigation(m_tournamentButton, Direction.Up, settingsButton);
            SetNavigation(m_trainingButton, Direction.Up, settingsButton);
            SetNavigation(m_onlineButton, Direction.Right, null);
            SetNavigation(m_onlineButton, Direction.Up, quitButton);
            if (!canPlayLocally)
                SetNavigation(m_onlineButton, Direction.Left, null);
            SetNavigation(characterSelectionButton, Direction.Left, null);
            SetNavigation(characterSelectionButton, Direction.Right, null);
            // Up goes back to the mode the player came from.
            SetNavigation(characterSelectionButton, Direction.Up, firstMode);
            foreach (var modeButton in new[] { m_tournamentButton, m_trainingButton, m_onlineButton })
                modeButton.RegisterCallback<FocusInEvent>(_ => SetNavigation(characterSelectionButton, Direction.Up, modeButton));

            DefaultFocus = firstMode;

            var lostRoomReason = OnlineManager.Instance ? OnlineManager.Instance.TakeLostRoomReason() : null;
            if (lostRoomReason != null)
            {
                m_notice.SetContent("Partie en ligne terminée", null);
                Query<VisualElement>("notice").Q<Label>("message").text = lostRoomReason;
                ShowDialog(m_notice);
            }
        }

        // A tournament is a series of games: a confirmation first, so that a press too many doesn't start one.
        private void HandleTournamentButtonClicked()
        {
            ShowDialog(m_tournamentConfirmation);
        }

        private void HandleTournamentConfirmed()
        {
            m_confirmAudioPlayer.Play();
            m_gameModesLauncher.LaunchTournament();
        }

        private void HandleTournamentCancelled()
        {
            m_backAudioPlayer.Play();
            HideDialog();
        }

        private void HideNotice()
        {
            m_confirmAudioPlayer.Play();
            HideDialog();
        }

        private void HandleTrainingButtonClicked()
        {
            m_orchestrator.SwitchToLevelSelectionScreen();
        }

        private void HandleOnlineButtonClicked()
        {
            m_orchestrator.SwitchToOnlineScreen();
        }

        private void HandleSettingsButtonClicked()
        {
            m_orchestrator.SwitchToSettingsScreen();
        }

        private void HandleCharacterSelectionButtonClicked()
        {
            m_orchestrator.SwitchToCharacterSelectionScreen();
        }

        private void HandleQuitGameButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
