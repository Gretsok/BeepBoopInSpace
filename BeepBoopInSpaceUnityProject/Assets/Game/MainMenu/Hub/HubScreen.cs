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
        [SerializeField]
        private AudioPlayer m_confirmAudioPlayer;
        [SerializeField]
        private AudioPlayer m_backAudioPlayer;

        private Button m_tournamentButton;
        private Button m_trainingButton;
        private Button m_onlineButton;
        private Button m_botsButton;
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
            m_botsButton = BindButton("bots-button", HandleBotsButtonClicked, m_confirmAudioPlayer);
            m_tournamentConfirmation = new ConfirmationDialog(Query<VisualElement>("tournament-confirmation"),
                HandleTournamentConfirmed, HandleTournamentCancelled);
            m_notice = new ConfirmationDialog(Query<VisualElement>("notice"), HideNotice, HideNotice);
            m_notice.CancelButton.style.display = DisplayStyle.None;
            m_notice.ConfirmButton.text = "OK";

            var firstMode = m_tournamentButton;
            RefreshBotsButton();

            // Four rows: the corner buttons, the modes, the character selection, the bots. The closest element ahead would
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
            SetNavigation(characterSelectionButton, Direction.Left, null);
            SetNavigation(characterSelectionButton, Direction.Right, null);
            SetNavigation(characterSelectionButton, Direction.Down, m_botsButton);
            SetNavigation(m_botsButton, Direction.Left, null);
            SetNavigation(m_botsButton, Direction.Right, null);
            SetNavigation(m_botsButton, Direction.Up, characterSelectionButton);
            SetNavigation(m_botsButton, Direction.Down, null);
            // Up goes back to the mode the player came from.
            SetNavigation(characterSelectionButton, Direction.Up, firstMode);
            foreach (var modeButton in new[] { m_tournamentButton, m_trainingButton, m_onlineButton })
                modeButton.RegisterCallback<FocusInEvent>(_ => SetNavigation(characterSelectionButton, Direction.Up, modeButton));

            DefaultFocus = firstMode;

            var lostRoomReason = OnlineManager.Instance ? OnlineManager.Instance.TakeLostRoomReason() : null;
            if (lostRoomReason != null)
            {
                m_notice.SetContent(Game.Global.Localization.Loc.T("Partie en ligne terminée"), null);
                Query<VisualElement>("notice").Q<Label>("message").text = Game.Global.Localization.Loc.T(lostRoomReason);
                ShowDialog(m_notice);
            }
        }

        private void HandleBotsButtonClicked()
        {
            GlobalContext.Instance.PlayerManager.CycleBotsCount();
            RefreshBotsButton();
        }

        private void RefreshBotsButton()
        {
            var playerManager = GlobalContext.Instance.PlayerManager;
            m_botsButton.text = Game.Global.Localization.Loc.T($"Robots adversaires : {playerManager.BotsCount}");
            // Four players: no room for a bot.
            m_botsButton.style.display = playerManager.MaxBotsCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
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
