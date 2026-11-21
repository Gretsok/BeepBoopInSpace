using Game.ArchitectureTools.FlowMachine;
using Game.Global;
using Game.LoadingScreen;
using Game.MainMenu.CharacterManagement;
using Game.MainMenu.CharacterSelection;
using Game.MainMenu.Credits;
using Game.MainMenu.Home;
using Game.MainMenu.Hub;
using Game.MainMenu.LevelSelection;
using Game.MainMenu.LocalOrOnline;
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
        private LocalOrOnlineState m_localOrOnlineState;
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
            m_localOrOnlineState.Initialize(globalContext, mainMenuContext);
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
        /// characters there, and Retour leads to the choice between local and online. Back from an online game, the
        /// room; from a game whose room was lost, the online screen, which says why. Retour goes back the way the
        /// players came.
        /// </summary>
        public void Run()
        {
            var onlineManager = m_globalContext.OnlineManager;
            var playerManager = m_globalContext.PlayerManager;
            if (onlineManager.IsInRoom)
            {
                playerManager.PlaysOnline = true;
                m_flowMachine.Run(m_onlineLobbyState, m_localOrOnlineState, m_characterSelectionState, m_onlineState);
            }
            else if (onlineManager.HasLostRoomReason && playerManager.Players.Count > 0)
            {
                playerManager.PlaysOnline = true;
                m_flowMachine.Run(m_onlineState, m_localOrOnlineState, m_characterSelectionState);
            }
            else if (playerManager.Players.Count > 0)
                m_flowMachine.Run(m_characterSelectionState, m_localOrOnlineState);
            else
                m_flowMachine.Run();
        }

        // The room is over (the host left, the connection failed...): its screens give way to the online screen, which
        // says why.
        private void HandleRoomLost(string reason)
        {
            // A game launches: the online screen would take the reason under the loading screen. The game goes back to
            // the menu once loaded, which opens on the online screen.
            if (m_globalContext.OnlineManager.IsInGame)
                return;
            var currentState = m_flowMachine.CurrentState;
            if (currentState == m_onlineLobbyState || currentState == m_levelSelectionState)
                BackToOnlineScreen();
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
        public void SwitchToLocalOrOnlineScreen()
        {
            m_flowMachine.RequestState(m_localOrOnlineState);
        }

        /// <summary>The players chose the local games: the character selection, then the hub.</summary>
        public void PlayLocally()
        {
            m_globalContext.PlayerManager.PlaysOnline = false;
            SwitchToCharacterSelectionScreen();
        }

        /// <summary>The players chose an online room: the character selection, then the online screen.</summary>
        public void PlayOnline()
        {
            m_globalContext.PlayerManager.PlaysOnline = true;
            SwitchToCharacterSelectionScreen();
        }

        [Button]
        public void SwitchToCharacterSelectionScreen()
        {
            m_flowMachine.RequestState(m_characterSelectionState);
        }

        /// <summary>Everybody is ready in the character selection: the modes the players chose, local or online.</summary>
        public void SwitchToModesScreen()
        {
            if (m_globalContext.PlayerManager.PlaysOnline)
                SwitchToOnlineScreen();
            else
                SwitchToHubScreen();
        }

        /// <summary>Out of the room (left or lost): back to the online screen, whose Retour leads to the character selection.</summary>
        public void BackToOnlineScreen()
        {
            m_flowMachine.RequestBackTo(m_onlineState);
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