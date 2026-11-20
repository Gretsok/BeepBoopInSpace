using Game.Global.SFXManagement;
using Game.Global.Localization;
using Game.Online;
using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;
using Direction = UnityEngine.UIElements.NavigationMoveEvent.Direction;

namespace Game.MainMenu.OnlineLobby
{
    /// <summary>
    /// The online room: its code to share, its players (up to 4, with their robots), and for the host the modes to
    /// launch for everyone, once two players are in. The guests wait for the host's choice. The players come back here
    /// after each game; Retour leaves the room, after a confirmation (the host's leaving closes it).
    /// </summary>
    public class OnlineLobbyScreen : AMainMenuScreen
    {
        private const string k_EmptyCardClass = "lobby__player--empty";
        private const string k_LocalCardClass = "lobby__player--local";

        [SerializeField]
        private AudioPlayer m_confirmAudioPlayer;
        [SerializeField]
        private AudioPlayer m_backAudioPlayer;
        [SerializeField]
        private AudioPlayer m_playerJoinedAudioPlayer;

        private MainMenuOrchestrator m_orchestrator;
        private OnlineManager m_onlineManager;

        private Button m_copyButton;
        private Button m_tournamentButton;
        private Button m_trainingButton;
        private Label m_hint;
        private ConfirmationDialog m_leaveDialog;
        private ConfirmationDialog m_tournamentDialog;
        private int m_playersCount;

        public void Initialize(MainMenuOrchestrator orchestrator, OnlineManager onlineManager)
        {
            m_orchestrator = orchestrator;
            m_onlineManager = onlineManager;
        }

        protected override void HandleActivation()
        {
            Query<Label>("code").text = m_onlineManager.RoomCode;
            m_copyButton = BindButton("copy-button", HandleCopyButtonClicked, m_confirmAudioPlayer);
            m_tournamentButton = BindButton("tournament-button", HandleTournamentButtonClicked, m_confirmAudioPlayer);
            m_trainingButton = BindButton("training-button", HandleTrainingButtonClicked, m_confirmAudioPlayer);
            BindButton("back-button", HandleBackButtonClicked, m_backAudioPlayer);
            m_hint = Query<Label>("hint");

            m_leaveDialog = new ConfirmationDialog(Query<VisualElement>("leave-confirmation"), HandleLeaveConfirmed, HandleDialogCancelled);
            m_leaveDialog.ConfirmButton.text = Loc.T("Quitter");
            Query<VisualElement>("leave-confirmation").Q<Label>("message").text = Loc.T(m_onlineManager.IsHost
                ? "La salle fermera pour tous ses joueurs."
                : "Vous pourrez la rejoindre avec son code.");
            m_tournamentDialog = new ConfirmationDialog(Query<VisualElement>("tournament-confirmation"), HandleTournamentConfirmed, HandleDialogCancelled);

            // Only the host chooses the mode.
            var modesDisplay = m_onlineManager.IsHost ? DisplayStyle.Flex : DisplayStyle.None;
            Query<VisualElement>("modes").style.display = modesDisplay;
            SetNavigation(m_copyButton, Direction.Down, m_trainingButton);
            SetNavigation(m_tournamentButton, Direction.Up, m_copyButton);
            SetNavigation(m_trainingButton, Direction.Up, m_copyButton);

            m_playersCount = m_onlineManager.Roster.Count;
            m_onlineManager.OnRosterChanged += HandleRosterChanged;
            Refresh();
        }

        protected override void HandleDeactivation()
        {
            if (m_onlineManager)
                m_onlineManager.OnRosterChanged -= HandleRosterChanged;
        }

        protected override void HandleCancel()
        {
            m_backAudioPlayer.Play();
            HandleBackButtonClicked();
        }

        private void HandleRosterChanged()
        {
            if (!m_onlineManager.IsInRoom)
                return;
            if (m_onlineManager.Roster.Count > m_playersCount && m_playerJoinedAudioPlayer)
                m_playerJoinedAudioPlayer.Play();
            m_playersCount = m_onlineManager.Roster.Count;
            Refresh();
        }

        private void Refresh()
        {
            var roster = m_onlineManager.Roster;
            var characters = m_onlineManager.Characters;
            for (int slot = 0; slot < OnlineManager.k_MaxPlayers; slot++)
            {
                var card = Query<VisualElement>($"player-{slot + 1}");
                var isTaken = slot < roster.Count;
                card.EnableInClassList(k_EmptyCardClass, !isTaken);
                var icon = card.Q("icon");
                var badge = card.Q("badge");
                var name = card.Q<Label>("name");
                var tag = card.Q<Label>("tag");
                if (!isTaken)
                {
                    icon.style.backgroundImage = StyleKeyword.Null;
                    badge.style.backgroundColor = StyleKeyword.Null;
                    name.text = Loc.T("Place libre");
                    tag.text = string.Empty;
                    card.EnableInClassList(k_LocalCardClass, false);
                    continue;
                }

                var roomPlayer = roster[slot];
                var character = roomPlayer.CharacterIndex < characters.Count ? characters[roomPlayer.CharacterIndex] : null;
                var isLocal = m_onlineManager.IsLocal(roomPlayer);
                card.EnableInClassList(k_LocalCardClass, isLocal);
                card.Q<Label>("badge-label").text = Loc.PlayerBadge(slot + 1);
                if (character)
                {
                    var picture = character.SelectionPreview ? character.SelectionPreview : character.SelectionIcon;
                    icon.style.backgroundImage = picture ? new StyleBackground(picture) : new StyleBackground(character.AvatarSprite);
                    badge.style.backgroundColor = character.SelectionColor;
                    name.text = character.Name;
                }
                tag.text = Loc.T(isLocal ? roomPlayer.MachineId == 0 ? "Vous, l'hôte" : "Vous"
                    : roomPlayer.MachineId == 0 ? "Hôte" : string.Empty);
            }

            var canLaunch = m_onlineManager.CanLaunch;
            m_tournamentButton.SetEnabled(canLaunch);
            m_trainingButton.SetEnabled(canLaunch);
            // A player left while the host was confirming the tournament.
            if (roster.Count < OnlineManager.k_MinPlayers && IsShowingDialog(m_tournamentDialog))
                HideDialog();
            if (m_onlineManager.IsHost)
                m_hint.text = Loc.T(roster.Count < OnlineManager.k_MinPlayers
                    ? "Donnez le code à vos amis : la partie se lance à deux joueurs ou plus."
                    : "Choisissez le mode de jeu de la salle.");
            else
                m_hint.text = Loc.T("L'hôte choisit le mode de jeu…");

            // Focused once enabled; the code's button meanwhile, which also keeps Retour working.
            var defaultFocus = canLaunch ? m_trainingButton : m_copyButton;
            if (DefaultFocus != defaultFocus)
            {
                DefaultFocus = defaultFocus;
                if (FocusedElement == null || FocusedElement is VisualElement { enabledSelf: false })
                    FocusSilently(defaultFocus);
            }
        }

        private void HandleCopyButtonClicked()
        {
            GUIUtility.systemCopyBuffer = m_onlineManager.RoomCode;
            m_hint.text = Loc.T("Code copié : collez-le à vos amis.");
        }

        private void HandleTrainingButtonClicked()
        {
            if (m_onlineManager.CanLaunch)
                m_orchestrator.SwitchToLevelSelectionScreen();
        }

        // A tournament is a series of games: a confirmation first, so that a press too many doesn't start one.
        private void HandleTournamentButtonClicked()
        {
            if (m_onlineManager.CanLaunch)
                ShowDialog(m_tournamentDialog);
        }

        private void HandleTournamentConfirmed()
        {
            m_confirmAudioPlayer.Play();
            m_onlineManager.LaunchTournament();
        }

        private void HandleBackButtonClicked()
        {
            m_leaveDialog.SetContent(Loc.T("Quitter la salle ?"), null);
            ShowDialog(m_leaveDialog);
        }

        private void HandleLeaveConfirmed()
        {
            m_backAudioPlayer.Play();
            m_onlineManager.LeaveRoom();
            m_orchestrator.SwitchToHubScreen();
        }

        private void HandleDialogCancelled()
        {
            m_backAudioPlayer.Play();
            HideDialog();
        }
    }
}
