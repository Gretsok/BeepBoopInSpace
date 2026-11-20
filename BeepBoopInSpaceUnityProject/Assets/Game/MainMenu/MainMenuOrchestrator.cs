using Game.ArchitectureTools.FlowMachine;
using Game.Global;
using Game.LoadingScreen;
using Game.MainMenu.CharacterManagement;
using Game.MainMenu.CharacterSelection;
using Game.MainMenu.Credits;
using Game.MainMenu.Home;
using Game.MainMenu.Hub;
using Game.MainMenu.LevelSelection;
using Game.MainMenu.Online;
using Game.MainMenu.OnlineLobby;
using Game.MainMenu.SettingsScreen;
using NaughtyAttributes;
using UnityEngine;

namespace Game.MainMenu
{
    public class MainMenuOrchestrator : MonoBehaviour
    {
        private FlowMachine m_flowMachine;
        private GlobalContext m_globalContext;
        private bool m_isGoingBackToTheRoom;

        [Header("States")]
        [SerializeField]
        private HomeState m_homeState;
        [SerializeField]
        private CharacterSelectionState m_characterSelectionState;
        [SerializeField]
        private CreditsState m_creditsState;
        [SerializeField]
        private MainMenuSettingsState m_settingsState;
        [SerializeField]
        private HubState m_hubState;
        [SerializeField]
        private LevelSelectionState m_levelSelectionState;
        [SerializeField]
        private OnlineState m_onlineState;
        [SerializeField]
        private OnlineLobbyState m_onlineLobbyState;

        public void Initialize(GlobalContext globalContext, MainMenuContext mainMenuContext)
        {
            m_flowMachine = mainMenuContext.FlowMachine;
            m_globalContext = globalContext;
            m_homeState.Initialize(globalContext, mainMenuContext);
            m_characterSelectionState.Initialize(globalContext, mainMenuContext);
            m_creditsState.Initialize(globalContext, mainMenuContext);
            m_settingsState.Initialize(globalContext, mainMenuContext);
            m_hubState.Initialize(globalContext, mainMenuContext);
            m_levelSelectionState.Initialize(globalContext, mainMenuContext);
            m_onlineState.Initialize(globalContext, mainMenuContext);
            m_onlineLobbyState.Initialize(globalContext, mainMenuContext);

            CharacterBankManager.Instance.RemapCharactersOf(globalContext.PlayerManager.Players);
            // A game is over: the players who left the online room go away.
            globalContext.OnlineManager.HandleBackInMenu();
            globalContext.OnlineManager.OnRoomLost += HandleRoomLost;
            globalContext.OnlineManager.OnRosterChanged += HandleRosterChanged;
            m_flowMachine.OnStateEntering += HandleStateEntering;

            // Games hide and lock the cursor: the menus are also played with the mouse.
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            LoadingScreenManager.Instance.HideLoadingScreen();
        }

        private void OnDestroy()
        {
            if (m_globalContext && m_globalContext.OnlineManager)
            {
                m_globalContext.OnlineManager.OnRoomLost -= HandleRoomLost;
                m_globalContext.OnlineManager.OnRosterChanged -= HandleRosterChanged;
            }
            if (m_flowMachine)
                m_flowMachine.OnStateEntering -= HandleStateEntering;
        }

        /// <summary>
        /// Opens the home screen, or the character selection when players come back from a game: they find their
        /// characters there, and Retour leads to the home screen. Back from an online game, the room; from a game
        /// whose room was lost, the hub, which says why.
        /// </summary>
        public void Run()
        {
            var onlineManager = m_globalContext.OnlineManager;
            if (onlineManager.IsInRoom)
                m_flowMachine.Run(m_onlineLobbyState);
            else if (onlineManager.HasLostRoomReason && m_globalContext.PlayerManager.Players.Count > 0)
                m_flowMachine.Run(m_hubState);
            else if (m_globalContext.PlayerManager.Players.Count > 0)
                m_flowMachine.Run(m_characterSelectionState);
            else
                m_flowMachine.Run();
        }

        // The room is over (the host left, the connection failed...): its screens give way to the hub, which says why.
        private void HandleRoomLost(string reason)
        {
            // A game launches: the hub would take the reason under the loading screen. The game goes back to the menu
            // once loaded, which opens on the hub.
            if (m_globalContext.OnlineManager.IsInGame)
                return;
            var currentState = m_flowMachine.CurrentState;
            if (currentState == m_onlineLobbyState || currentState == m_levelSelectionState)
                SwitchToHubScreen();
        }

        // The host was choosing a level, or confirming it, for players who left: back to the room, which says it needs
        // more players. Once, though several may leave before the room opens.
        private void HandleRosterChanged()
        {
            var onlineManager = m_globalContext.OnlineManager;
            if (m_isGoingBackToTheRoom || !onlineManager.IsHost || onlineManager.IsInGame
                || onlineManager.Roster.Count >= global::Game.Online.OnlineManager.k_MinPlayers
                || m_flowMachine.CurrentState != m_levelSelectionState)
                return;
            m_isGoingBackToTheRoom = true;
            SwitchToOnlineLobbyScreen();
        }

        private void HandleStateEntering(AFlowState state)
        {
            m_isGoingBackToTheRoom = false;
        }

        [Button]
        public void SwitchToHomeScreen()
        {
            m_flowMachine.RequestState(m_homeState);
        }

        [Button]
        public void SwitchToCharacterSelectionScreen()
        {
            m_flowMachine.RequestState(m_characterSelectionState);
        }

        [Button]
        public void SwitchToCreditsScreen()
        {
            m_flowMachine.RequestState(m_creditsState);
        }

        [Button]
        public void SwitchToSettingsScreen()
        {
            m_flowMachine.RequestState(m_settingsState);
        }

        [Button]
        public void SwitchToHubScreen()
        {
            m_flowMachine.RequestState(m_hubState);
        }

        [Button]
        public void SwitchToLevelSelectionScreen()
        {
            m_flowMachine.RequestState(m_levelSelectionState);
        }

        [Button]
        public void SwitchToOnlineScreen()
        {
            m_flowMachine.RequestState(m_onlineState);
        }

        [Button]
        public void SwitchToOnlineLobbyScreen()
        {
            m_flowMachine.RequestState(m_onlineLobbyState);
        }
    }
}