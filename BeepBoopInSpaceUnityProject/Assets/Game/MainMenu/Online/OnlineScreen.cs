using Cysharp.Threading.Tasks;
using Game.Global.SFXManagement;
using Game.Online;
using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Direction = UnityEngine.UIElements.NavigationMoveEvent.Direction;

namespace Game.MainMenu.Online
{
    /// <summary>
    /// The online modes, reached from the character selection by the players who chose to play online, with their
    /// robots: create a room, which gives a code to share, or join one with its code. A dialog waits for the connection,
    /// which Retour cancels. Out of a room (left, or lost while playing: the host left...), the players come back here;
    /// a lost room comes with a word on why.
    /// </summary>
    public class OnlineScreen : AMainMenuScreen
    {
        [SerializeField]
        private AudioPlayer m_confirmAudioPlayer;
        [SerializeField]
        private AudioPlayer m_backAudioPlayer;

        private MainMenuOrchestrator m_orchestrator;
        private OnlineManager m_onlineManager;

        private Button m_createButton;
        private Button m_joinButton;
        private Label m_status;
        private RoomCodeField m_codeField;
        private ConfirmationDialog m_connectingDialog;
        private ConfirmationDialog m_notice;
        private bool m_isConnecting;

        public void Initialize(MainMenuOrchestrator orchestrator, OnlineManager onlineManager)
        {
            m_orchestrator = orchestrator;
            m_onlineManager = onlineManager;
        }

        protected override void HandleActivation()
        {
            m_createButton = BindButton("create-button", HandleCreateButtonClicked, m_confirmAudioPlayer);
            m_joinButton = BindButton("join-button", HandleJoinButtonClicked, m_confirmAudioPlayer);
            BindButton("back-button", HandleBackButtonClicked, m_backAudioPlayer);
            m_status = Query<Label>("status");
            m_codeField = new RoomCodeField(Query<VisualElement>("code"), () => m_joinButton);
            m_codeField.OnChanged += RefreshJoinButton;

            // Connecting: the dialog's only button cancels, as does Retour.
            m_connectingDialog = new ConfirmationDialog(Query<VisualElement>("connecting-dialog"), CancelConnecting, CancelConnecting);
            m_connectingDialog.CancelButton.style.display = DisplayStyle.None;
            m_connectingDialog.ConfirmButton.text = Game.Global.Localization.Loc.T("Annuler");
            m_notice = new ConfirmationDialog(Query<VisualElement>("notice"), HideNotice, HideNotice);
            m_notice.CancelButton.style.display = DisplayStyle.None;
            m_notice.ConfirmButton.text = "OK";

            // Left, the room to create; right, the code then Rejoindre.
            SetNavigation(m_createButton, Direction.Right, m_codeField.FirstSlot);
            SetNavigation(m_joinButton, Direction.Left, m_createButton);
            SetNavigation(m_joinButton, Direction.Up, m_codeField.FirstSlot);

            var keyboard = Keyboard.current;
            if (keyboard != null)
                keyboard.onTextInput += HandleTextInput;

            // On a local network (-online-direct), the guests join the address given to the game: no code to type.
            if (OnlineConnection.DirectAddress != null)
                SetStatus($"Réseau local : {OnlineConnection.DirectAddress}", false);
            else
                SetStatus(null, false);
            RefreshJoinButton();
            DefaultFocus = m_createButton;

            var lostRoomReason = m_onlineManager.TakeLostRoomReason();
            if (lostRoomReason != null)
            {
                m_notice.SetContent(Game.Global.Localization.Loc.T("Partie en ligne terminée"), null);
                Query<VisualElement>("notice").Q<Label>("message").text = Game.Global.Localization.Loc.T(lostRoomReason);
                ShowDialog(m_notice);
            }
        }

        private void HideNotice()
        {
            m_confirmAudioPlayer.Play();
            HideDialog();
        }

        protected override void HandleDeactivation()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
                keyboard.onTextInput -= HandleTextInput;
            m_codeField = null;
        }

        // Connecting, or telling why the last room was lost: the code waits.
        private bool CanTypeTheCode => !m_isConnecting && !IsShowingDialog(m_notice);

        protected override void HandleUpdate()
        {
            if (CanTypeTheCode)
                m_codeField?.Update();
        }

        protected override void HandleCancel()
        {
            m_backAudioPlayer.Play();
            HandleBackButtonClicked();
        }

        private void HandleBackButtonClicked()
        {
            if (!m_isConnecting)
                MainMenuContext.Instance.FlowMachine.RequestBack();
        }

        private void HandleTextInput(char character)
        {
            if (CanTypeTheCode)
                m_codeField?.Type(character);
        }

        private bool CanJoin => OnlineConnection.DirectAddress != null || (m_codeField != null && m_codeField.IsComplete);

        private void RefreshJoinButton()
        {
            m_joinButton?.SetEnabled(CanJoin);
        }

        private void HandleCreateButtonClicked()
        {
            Connect("Ouverture de la salle…", m_onlineManager.CreateRoom).Forget();
        }

        private void HandleJoinButtonClicked()
        {
            if (!CanJoin)
                return;
            var code = m_codeField.Code;
            Connect($"Connexion à la salle {code}…", () => m_onlineManager.JoinRoom(code)).Forget();
        }

        private async UniTaskVoid Connect(string title, System.Func<UniTask<string>> connect)
        {
            if (m_isConnecting)
                return;
            m_isConnecting = true;
            SetStatus(null, false);
            m_connectingDialog.SetContent(Game.Global.Localization.Loc.T(title), null);
            ShowDialog(m_connectingDialog);

            var error = await connect();

            m_isConnecting = false;
            if (!IsActivated)
                return;
            HideDialog();
            if (m_onlineManager.IsInRoom)
            {
                m_orchestrator.SwitchToOnlineLobbyScreen();
                return;
            }
            SetStatus(error, true);
        }

        private void CancelConnecting()
        {
            m_backAudioPlayer.Play();
            m_onlineManager.CancelConnecting();
        }

        private void SetStatus(string message, bool isError)
        {
            if (m_status == null)
                return;
            m_status.text = Game.Global.Localization.Loc.T(message) ?? string.Empty;
            m_status.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
            m_status.EnableInClassList("online__status--error", isError);
        }
    }
}
