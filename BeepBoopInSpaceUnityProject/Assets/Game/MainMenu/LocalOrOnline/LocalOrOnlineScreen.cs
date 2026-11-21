using Game.Global.PlayerManagement;
using Game.Global.SFXManagement;
using UnityEngine;

namespace Game.MainMenu.LocalOrOnline
{
    /// <summary>
    /// Between the home screen and the character selection, how the players play: locally (this machine's players,
    /// with robots of the computer if need be: the hub's modes) or online (a room with players of other machines: the
    /// online screen). Retour goes back to the home screen.
    /// </summary>
    public class LocalOrOnlineScreen : AMainMenuScreen
    {
        [SerializeField]
        private AudioPlayer m_confirmAudioPlayer;
        [SerializeField]
        private AudioPlayer m_backAudioPlayer;

        private MainMenuOrchestrator m_orchestrator;
        private PlayerManager m_playerManager;

        public void Initialize(MainMenuOrchestrator orchestrator, PlayerManager playerManager)
        {
            m_orchestrator = orchestrator;
            m_playerManager = playerManager;
        }

        protected override void HandleActivation()
        {
            var localButton = BindButton("local-button", HandleLocalButtonClicked, m_confirmAudioPlayer);
            var onlineButton = BindButton("online-button", HandleOnlineButtonClicked, m_confirmAudioPlayer);
            BindButton("back-button", HandleBackButtonClicked, m_backAudioPlayer);
            // Back from the character selection: the way the players chose.
            DefaultFocus = m_playerManager.PlaysOnline ? onlineButton : localButton;
        }

        protected override void HandleCancel()
        {
            m_backAudioPlayer.Play();
            HandleBackButtonClicked();
        }

        private void HandleLocalButtonClicked()
        {
            m_orchestrator.PlayLocally();
        }

        private void HandleOnlineButtonClicked()
        {
            m_orchestrator.PlayOnline();
        }

        private void HandleBackButtonClicked()
        {
            MainMenuContext.Instance.FlowMachine.RequestBack();
        }
    }
}
