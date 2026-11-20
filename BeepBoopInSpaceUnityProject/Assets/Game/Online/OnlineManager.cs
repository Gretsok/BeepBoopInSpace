using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Characters;
using Game.Gameplay.Levels._0_Core;
using Game.Global;
using Game.Global.PlayerManagement;
using Game.MainMenu;
using Game.MainMenu.CharacterManagement;
using Game.Tournament;
using UnityEngine;

namespace Game.Online
{
    public enum ERoomStatus
    {
        None,
        Connecting,
        InRoom,
    }

    /// <summary>
    /// The online room: up to 4 players on several machines, joined with a code. The host's players come first, then
    /// each guest machine's, in the order they joined. The host decides everything (who is in the room, the robots,
    /// the games, what happens in them) and tells the guests. Each machine plays with its own local players, who
    /// chose their robots in the character selection before going online; the other machines' players are
    /// <see cref="AbstractPlayer"/>s without input, in <see cref="PlayerManager.GamePlayers"/>.
    /// </summary>
    public class OnlineManager : MonoBehaviour
    {
        public const int k_MaxPlayers = 4;
        public const int k_MinPlayers = 2;
        // Machines with another version can't join: their messages wouldn't say the same things.
        private const int k_ProtocolVersion = 1;
        // Netcode's id of the host's machine.
        private const ulong k_HostMachineId = 0;

        [SerializeField, Tooltip("The levels of the training: the host sends the guests the rank of the one it launches.")]
        private LevelsListDataAsset m_trainingLevels;

        public static OnlineManager Instance { get; private set; }

        /// <summary>Host: seconds the guests have to load a game once the host's started loading, before being sent away.</summary>
        public static float GuestLoadingTimeout { get; set; } = 60f;

        private readonly OnlineConnection m_connection = new();
        private readonly List<RoomPlayer> m_roster = new();
        private readonly List<AbstractPlayer> m_gamePlayers = new();
        private readonly Dictionary<(ulong, byte), AbstractPlayer> m_remotePlayers = new();
        // Guests approved by the host, not connected yet: the robots they come with.
        private readonly Dictionary<ulong, byte[]> m_pendingGuests = new();
        private readonly Queue<byte[]> m_gameInbox = new();
        private readonly HashSet<ulong> m_loadedGuests = new();
        private List<CharacterDataAsset> m_characters = new();
        private CancellationTokenSource m_connectingCancellation;
        private string m_lostRoomReason;
        // Guest, tournament: the number of the game the host goes on with, until this machine starts loading it.
        private int m_nextTournamentGame;
        private bool m_isKeepingRunningInBackground;
        private bool m_runInBackgroundOutsideRooms;
        private bool m_isClosing;

        public ERoomStatus Status { get; private set; }
        public bool IsInRoom => Status == ERoomStatus.InRoom;
        public bool IsHost { get; private set; }
        /// <summary>Joined a room as a guest. Stays true in a game the room was lost in, until back in the menu.</summary>
        public bool IsGuest { get; private set; }
        /// <summary>From the launch of a game (or a tournament) until the players are back in the menu.</summary>
        public bool IsInGame { get; private set; }
        /// <summary>Counts the games of the room: a guest's "loaded" answers the right one.</summary>
        public int GameNumber { get; private set; }
        public string RoomCode => m_connection.Code;
        public ulong LocalMachineId => m_connection.LocalClientId;
        public IReadOnlyList<RoomPlayer> Roster => m_roster;
        public IReadOnlyList<CharacterDataAsset> Characters => m_characters;
        /// <summary>The host can launch a game: enough players, nobody joining, nobody playing.</summary>
        public bool CanLaunch => IsHost && IsInRoom && !IsInGame && m_roster.Count >= k_MinPlayers && m_pendingGuests.Count == 0;

        public event Action OnRosterChanged;
        /// <summary>The room is over without this machine leaving it, with the reason for the players.</summary>
        public event Action<string> OnRoomLost;
        /// <summary>Host: a guest's machine left during a game (<see cref="HasLeftRoom"/> tells whose players).</summary>
        public event Action OnGuestLeftGame;
        /// <summary>Host: a game message from a guest's machine.</summary>
        public event Action<ulong, byte[]> OnGuestGameMessage;
        /// <summary>Guest: the host closed the tournament's results: the rank of the next level, or -1 for the end.</summary>
        public event Action<int> OnTournamentNextGame;

        private void Awake()
        {
            Instance = this;
            m_connection.OnMessageReceived += HandleMessageReceived;
            m_connection.OnGuestConnected += HandleGuestConnected;
            m_connection.OnGuestDisconnected += HandleGuestDisconnected;
            m_connection.OnConnectionLost += HandleConnectionLost;
            m_connection.ApproveGuest = ApproveGuest;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            KeepRunningInBackground(false);
        }

        // The room's messages only go through while the game runs: a machine of the room keeps playing when its window
        // loses the focus (the host pasting the code to friends...), else the others freeze, then lose the room.
        // In the editor, this is the project's Player Settings: given back once out of the room.
        private void KeepRunningInBackground(bool keep)
        {
            if (keep == m_isKeepingRunningInBackground)
                return;
            m_isKeepingRunningInBackground = keep;
            if (keep)
                m_runInBackgroundOutsideRooms = Application.runInBackground;
            Application.runInBackground = keep || m_runInBackgroundOutsideRooms;
        }

        #region Opening and leaving

        /// <summary>Opens a room with the local players. Null once in it, else what went wrong, for the players.</summary>
        public async UniTask<string> CreateRoom()
        {
            if (!TryStartConnecting(isHost: true, out var error))
                return error;
            try
            {
                await WaitForTheLastRoomToClose();
                await m_connection.StartHost(k_MaxPlayers, m_connectingCancellation.Token);
                m_roster.Clear();
                var localPlayers = LocalPlayers;
                for (int i = 0; i < localPlayers.Count; i++)
                    m_roster.Add(new RoomPlayer
                    {
                        MachineId = m_connection.LocalClientId,
                        LocalIndex = (byte)i,
                        CharacterIndex = (byte)CharacterIndexOf(localPlayers[i].CharacterDataAsset),
                    });
                Status = ERoomStatus.InRoom;
                RefreshGamePlayers();
                OnRosterChanged?.Invoke();
                return null;
            }
            catch (Exception e)
            {
                return await HandleConnectingFailed(e, "Impossible de créer la salle.");
            }
        }

        /// <summary>Joins the room of the code with the local players. Null once in it, else what went wrong, for the players.</summary>
        public async UniTask<string> JoinRoom(string code)
        {
            if (!TryStartConnecting(isHost: false, out var error))
                return error;
            try
            {
                var localPlayers = LocalPlayers;
                var joinRequest = OnlineMessage.Write(EOnlineMessage.JoinRequest, writer =>
                {
                    writer.Write(k_ProtocolVersion);
                    writer.Write(Application.version);
                    writer.Write((byte)localPlayers.Count);
                    foreach (var player in localPlayers)
                        writer.Write((byte)CharacterIndexOf(player.CharacterDataAsset));
                });
                await WaitForTheLastRoomToClose();
                await m_connection.StartGuest(code, joinRequest, m_connectingCancellation.Token);
                // The host sends the room's players as soon as the machine is connected.
                var timeout = Time.realtimeSinceStartup + 10f;
                await UniTask.WaitUntil(() => m_roster.Count > 0 || !m_connection.IsConnected || Time.realtimeSinceStartup > timeout,
                    cancellationToken: m_connectingCancellation.Token);
                if (m_roster.Count == 0)
                    throw new OnlineException("L'hôte ne répond pas.");
                Status = ERoomStatus.InRoom;
                IsGuest = true;
                RefreshGamePlayers();
                OnRosterChanged?.Invoke();
                return null;
            }
            catch (Exception e)
            {
                return await HandleConnectingFailed(e, "Impossible de rejoindre la salle.");
            }
        }

        /// <summary>Stops opening or joining a room.</summary>
        public void CancelConnecting()
        {
            m_connectingCancellation?.Cancel();
        }

        /// <summary>Leaves the room, which closes it for everyone when this machine is its host.</summary>
        public void LeaveRoom()
        {
            if (Status == ERoomStatus.None)
                return;
            Debug.Log("[Online] Leaving the room.");
            _ = CloseRoom(notifyGuests: true);
        }

        public bool HasLostRoomReason => m_lostRoomReason != null;

        // A room left a moment ago may still be closing its connection, which the next one reuses.
        private UniTask WaitForTheLastRoomToClose()
        {
            return m_isClosing
                ? UniTask.WaitWhile(() => m_isClosing, cancellationToken: m_connectingCancellation.Token)
                : UniTask.CompletedTask;
        }

        /// <summary>Why the last room ended without the players leaving it, once: the menu tells them.</summary>
        public string TakeLostRoomReason()
        {
            var reason = m_lostRoomReason;
            m_lostRoomReason = null;
            return reason;
        }

        private bool TryStartConnecting(bool isHost, out string error)
        {
            error = null;
            if (Status != ERoomStatus.None)
            {
                error = "Une connexion est déjà en cours.";
                return false;
            }
            var bank = CharacterBankManager.Instance;
            if (!bank || LocalPlayers.Count == 0)
            {
                error = "Choisissez d'abord vos robots.";
                return false;
            }
            if (LocalPlayers.Count > k_MaxPlayers - (isHost ? 0 : 1))
            {
                error = $"Une salle en ligne accueille {k_MaxPlayers} joueurs au plus.";
                return false;
            }
            // The rank of a robot in the bank names it: every machine has the same bank.
            m_characters = bank.Characters.ToList();
            m_roster.Clear();
            m_pendingGuests.Clear();
            m_lostRoomReason = null;
            Status = ERoomStatus.Connecting;
            KeepRunningInBackground(true);
            IsHost = isHost;
            m_connectingCancellation?.Dispose();
            m_connectingCancellation = new CancellationTokenSource();
            return true;
        }

        private async UniTask<string> HandleConnectingFailed(Exception exception, string defaultMessage)
        {
            var message = exception switch
            {
                OperationCanceledException => null,
                OnlineException onlineException => onlineException.Message,
                _ => defaultMessage,
            };
            if (exception is not OperationCanceledException and not OnlineException)
                Debug.LogWarning($"[Online] {defaultMessage} {exception}");
            else
                Debug.Log($"[Online] {defaultMessage} {message}");
            await CloseRoom(notifyGuests: false);
            return message;
        }

        private async UniTask CloseRoom(bool notifyGuests)
        {
            var wasHost = IsHost;
            Status = ERoomStatus.None;
            IsHost = false;
            if (!IsInGame)
                IsGuest = false;
            m_roster.Clear();
            m_pendingGuests.Clear();
            m_loadedGuests.Clear();
            m_gameInbox.Clear();
            if (!IsInGame)
                ForgetRoomPlayers();
            var wasClosing = m_isClosing;
            m_isClosing = true;
            try
            {
                if (wasHost && notifyGuests && m_connection.IsConnected)
                {
                    m_connection.SendToGuests(OnlineMessage.Write(EOnlineMessage.RoomClosed));
                    // Lets the message leave before the connection closes.
                    await UniTask.Delay(TimeSpan.FromSeconds(0.2f), ignoreTimeScale: true);
                }
                await m_connection.Stop();
            }
            finally
            {
                // A closing that started first ends on its own.
                if (!wasClosing)
                    m_isClosing = false;
            }
            // Unless another room is on its way meanwhile.
            if (Status == ERoomStatus.None)
                KeepRunningInBackground(false);
        }

        private void HandleConnectionLost(string reason)
        {
            // While connecting, the connecting task reports what happened.
            if (Status != ERoomStatus.InRoom)
                return;
            HandleRoomLost(reason);
        }

        private void HandleRoomLost(string reason)
        {
            Debug.Log($"[Online] Room lost: {reason}");
            m_lostRoomReason = reason;
            _ = CloseRoom(notifyGuests: false);
            OnRoomLost?.Invoke(reason);
        }

        #endregion

        #region Players

        private static IReadOnlyList<AbstractPlayer> LocalPlayers => GlobalContext.Instance.PlayerManager.Players;

        public int CharacterIndexOf(CharacterDataAsset character) => Mathf.Max(0, m_characters.IndexOf(character));

        /// <summary>Whether the player at this rank of the room plays on this machine.</summary>
        public bool IsLocal(RoomPlayer player) => player.MachineId == LocalMachineId;

        // The players of the next game: the room's, in its order. A game keeps its own until the players are back in
        // the menu, even those who left during it.
        private void RefreshGamePlayers()
        {
            if (IsInGame)
                return;
            m_gamePlayers.Clear();
            var localPlayers = LocalPlayers;
            for (int slot = 0; slot < m_roster.Count; slot++)
            {
                var roomPlayer = m_roster[slot];
                AbstractPlayer player;
                if (IsLocal(roomPlayer))
                {
                    if (roomPlayer.LocalIndex >= localPlayers.Count)
                        continue;
                    player = localPlayers[roomPlayer.LocalIndex];
                }
                else
                {
                    player = GetOrCreateRemotePlayer(roomPlayer.MachineId, roomPlayer.LocalIndex);
                }
                if (roomPlayer.CharacterIndex < m_characters.Count)
                    player.SetCharacterData(m_characters[roomPlayer.CharacterIndex]);
                player.SetNumber(slot + 1);
                m_gamePlayers.Add(player);
            }

            foreach (var key in m_remotePlayers.Keys.ToList())
            {
                if (m_gamePlayers.Contains(m_remotePlayers[key]))
                    continue;
                Destroy(m_remotePlayers[key].gameObject);
                m_remotePlayers.Remove(key);
            }
            GlobalContext.Instance.PlayerManager.SetGamePlayers(m_gamePlayers);
        }

        private AbstractPlayer GetOrCreateRemotePlayer(ulong machineId, byte localIndex)
        {
            if (m_remotePlayers.TryGetValue((machineId, localIndex), out var player) && player)
                return player;
            var playerObject = new GameObject($"RemotePlayer {machineId}-{localIndex}");
            playerObject.transform.SetParent(transform, false);
            player = playerObject.AddComponent<AbstractPlayer>();
            m_remotePlayers[(machineId, localIndex)] = player;
            return player;
        }

        private void ForgetRoomPlayers()
        {
            foreach (var player in m_remotePlayers.Values)
            {
                if (player)
                    Destroy(player.gameObject);
            }
            m_remotePlayers.Clear();
            m_gamePlayers.Clear();
            var globalContext = GlobalContext.Instance;
            if (!globalContext)
                return;
            foreach (var player in globalContext.PlayerManager.Players)
                player.SetNumber(0);
            globalContext.PlayerManager.SetGamePlayers(null);
        }

        // Host: the robots of the guest's players, another one for a robot already in the room.
        private void AddGuestPlayers(ulong machineId, byte[] characters)
        {
            var taken = new HashSet<int>(m_roster.Select(player => (int)player.CharacterIndex));
            for (int i = 0; i < characters.Length; i++)
            {
                int character = characters[i];
                if (character >= m_characters.Count || taken.Contains(character))
                    character = Enumerable.Range(0, m_characters.Count).FirstOrDefault(index => !taken.Contains(index));
                taken.Add(character);
                m_roster.Add(new RoomPlayer { MachineId = machineId, LocalIndex = (byte)i, CharacterIndex = (byte)character });
            }
        }

        #endregion

        #region Host

        private string ApproveGuest(ulong machineId, byte[] request)
        {
            if (!IsInRoom || !IsHost)
                return "La salle n'est pas prête.";
            byte[] characters;
            try
            {
                using var reader = OnlineMessage.Read(request);
                if (reader.ReadInt32() != k_ProtocolVersion || reader.ReadString() != Application.version)
                    return "Cette salle utilise une autre version du jeu.";
                characters = reader.ReadBytes(reader.ReadByte());
            }
            catch (Exception)
            {
                return "Cette salle utilise une autre version du jeu.";
            }
            if (IsInGame)
                return "Une partie est en cours dans cette salle : réessayez à la fin.";
            var playersCount = m_roster.Count + m_pendingGuests.Values.Sum(pending => pending.Length);
            if (characters.Length == 0 || playersCount + characters.Length > k_MaxPlayers)
                return playersCount >= k_MaxPlayers
                    ? "La salle est pleine."
                    : $"Il ne reste que {k_MaxPlayers - playersCount} place(s) dans la salle.";
            m_pendingGuests[machineId] = characters;
            return null;
        }

        private void HandleGuestConnected(ulong machineId)
        {
            if (!m_pendingGuests.Remove(machineId, out var characters))
                return;
            Debug.Log($"[Online] Machine {machineId} joined with {characters.Length} player(s).");
            AddGuestPlayers(machineId, characters);
            SendRoster();
        }

        private void HandleGuestDisconnected(ulong machineId)
        {
            m_pendingGuests.Remove(machineId);
            m_loadedGuests.Remove(machineId);
            if (m_roster.RemoveAll(player => player.MachineId == machineId) == 0)
                return;
            Debug.Log($"[Online] Machine {machineId} left.");
            if (IsInGame)
                OnGuestLeftGame?.Invoke();
            SendRoster();
        }

        private void SendRoster()
        {
            m_connection.SendToGuests(OnlineMessage.Write(EOnlineMessage.Roster, WriteRoster));
            RefreshGamePlayers();
            OnRosterChanged?.Invoke();
        }

        private void WriteRoster(BinaryWriter writer)
        {
            writer.Write((byte)m_roster.Count);
            foreach (var player in m_roster)
                player.Write(writer);
        }

        public void LaunchTraining(LevelDataAsset level)
        {
            var levelIndex = IndexOf(m_trainingLevels.LevelDataList, level);
            if (!CanLaunch || levelIndex < 0)
            {
                Debug.LogWarning($"[Online] Can't launch {(level ? level.name : "no level")} now.");
                return;
            }
            StartGame(GameNumber + 1);
            m_connection.SendToGuests(OnlineMessage.Write(EOnlineMessage.LaunchTraining, writer =>
            {
                writer.Write(GameNumber);
                writer.Write((byte)levelIndex);
            }));
            MainMenuContext.Instance.GameModesLauncher.LaunchTraining(level);
        }

        public void LaunchTournament()
        {
            if (!CanLaunch)
            {
                Debug.LogWarning("[Online] Can't launch the tournament now.");
                return;
            }
            StartGame(GameNumber + 1);
            m_connection.SendToGuests(OnlineMessage.Write(EOnlineMessage.LaunchTournament, writer => writer.Write(GameNumber)));
            MainMenuContext.Instance.GameModesLauncher.LaunchTournament();
        }

        /// <summary>Host, tournament: the players leave the results for the level at this rank of the config, or for its end (-1).</summary>
        public void SendTournamentNextGame(int levelIndex)
        {
            if (!IsHost || !IsInRoom)
                return;
            if (levelIndex >= 0)
                StartGame(GameNumber + 1);
            m_connection.SendToGuests(OnlineMessage.Write(EOnlineMessage.TournamentNextGame, writer =>
            {
                writer.Write(GameNumber);
                writer.Write((short)levelIndex);
            }));
        }

        /// <summary>Host: every guest still in the room has loaded the current game.</summary>
        public bool AreGuestsLoaded => !IsHost || m_connection.GuestIds.All(id => m_loadedGuests.Contains(id));

        /// <summary>Host: the guests that still haven't loaded the current game leave the room, told why.</summary>
        public void SendAwayGuestsStillLoading()
        {
            const string reason = "La partie a mis trop de temps à charger sur cette machine.";
            if (!IsHost || !IsInRoom)
                return;
            foreach (var machineId in m_connection.GuestIds.Where(id => !m_loadedGuests.Contains(id)).ToList())
            {
                Debug.Log($"[Online] Machine {machineId} hasn't loaded game {GameNumber} in time: sent away.");
                m_connection.DisconnectGuest(machineId, reason);
                // Netcode may not call back for a machine it disconnects itself.
                HandleGuestDisconnected(machineId);
            }
        }

        /// <summary>Host: tells the guests what just happened in the game.</summary>
        public void SendGameMessage(byte[] message)
        {
            if (IsHost && IsInRoom)
                m_connection.SendToGuests(message);
        }

        /// <summary>Host: a player of the game whose machine left the room.</summary>
        public bool HasLeftRoom(AbstractPlayer player)
        {
            if (!player || !player.IsRemote)
                return false;
            foreach (var roomPlayer in m_roster)
            {
                if (m_remotePlayers.TryGetValue((roomPlayer.MachineId, roomPlayer.LocalIndex), out var remotePlayer) && remotePlayer == player)
                    return false;
            }
            return true;
        }

        /// <summary>Host: whether the player at this rank of the game plays on that machine.</summary>
        public bool IsPlayerOf(ulong machineId, int gameSlot)
        {
            if (gameSlot < 0 || gameSlot >= m_gamePlayers.Count)
                return false;
            var player = m_gamePlayers[gameSlot];
            return m_remotePlayers.Any(pair => pair.Key.Item1 == machineId && pair.Value == player);
        }

        #endregion

        #region Guest

        /// <summary>Guest: tells the host what this machine's players did.</summary>
        public void SendToHost(byte[] message)
        {
            if (IsGuest && IsInRoom)
                m_connection.SendToHost(message);
        }

        /// <summary>Guest: the next thing the host said about the game, in order. The game reads them once loaded.</summary>
        public bool TryTakeGameMessage(out byte[] message)
        {
            return m_gameInbox.TryDequeue(out message);
        }

        /// <summary>Guest, tournament: the last game is over here too, the one the host chose next starts loading.</summary>
        public void StartTheNextTournamentGame()
        {
            if (IsGuest && m_nextTournamentGame > GameNumber)
                StartGame(m_nextTournamentGame);
        }

        /// <summary>Guest: the game is loaded, the host may start it.</summary>
        public void SendGameLoaded()
        {
            SendToHost(OnlineMessage.Write(EOnlineMessage.GameLoaded, writer => writer.Write(GameNumber)));
        }

        #endregion

        /// <summary>The main menu opened: a game is over, its players who left the room go away.</summary>
        public void HandleBackInMenu()
        {
            IsInGame = false;
            m_gameInbox.Clear();
            if (IsInRoom)
            {
                // The same ranks, in this menu's copies of the characters (CharacterBankManager.RemapCharactersOf).
                m_characters = CharacterBankManager.Instance.Characters.ToList();
                RefreshGamePlayers();
                return;
            }
            IsGuest = false;
            ForgetRoomPlayers();
        }

        private void StartGame(int gameNumber)
        {
            IsInGame = true;
            GameNumber = gameNumber;
            m_loadedGuests.Clear();
            m_gameInbox.Clear();
        }

        private void HandleMessageReceived(ulong senderId, byte[] message)
        {
            // Left or lost: what was still on its way doesn't count (the connection takes a moment to close).
            if (Status == ERoomStatus.None)
                return;
            var type = OnlineMessage.TypeOf(message);
            if (IsHost)
            {
                HandleMessageFromGuest(senderId, type, message);
                return;
            }
            if (senderId != k_HostMachineId)
                return;
            // Joining: only the room's players.
            if (Status != ERoomStatus.InRoom && type != EOnlineMessage.Roster)
                return;
            if (OnlineMessage.IsGameMessage(type))
            {
                // Kept for the game, which reads them once loaded.
                if (IsInGame)
                    m_gameInbox.Enqueue(message);
                return;
            }
            using var reader = OnlineMessage.Read(message);
            switch (type)
            {
                case EOnlineMessage.Roster:
                    m_roster.Clear();
                    var count = reader.ReadByte();
                    for (int i = 0; i < count; i++)
                        m_roster.Add(RoomPlayer.Read(reader));
                    if (IsInRoom)
                    {
                        RefreshGamePlayers();
                        OnRosterChanged?.Invoke();
                    }
                    break;
                case EOnlineMessage.LaunchTraining:
                {
                    var gameNumber = reader.ReadInt32();
                    var level = m_trainingLevels.LevelDataList[reader.ReadByte()];
                    StartCoroutine(LaunchInMenu(gameNumber, launcher => launcher.LaunchTraining(level)));
                    break;
                }
                case EOnlineMessage.LaunchTournament:
                    StartCoroutine(LaunchInMenu(reader.ReadInt32(), launcher => launcher.LaunchTournament()));
                    break;
                case EOnlineMessage.TournamentNextGame:
                {
                    var gameNumber = reader.ReadInt32();
                    var levelIndex = reader.ReadInt16();
                    // Not started yet: the last game's messages may still be waiting to be played (this machine lagged
                    // as it ended), the next game's only come once it's loaded (StartTheNextTournamentGame).
                    if (levelIndex >= 0)
                        m_nextTournamentGame = gameNumber;
                    OnTournamentNextGame?.Invoke(levelIndex);
                    break;
                }
                case EOnlineMessage.RoomClosed:
                    HandleRoomLost("L'hôte a fermé la salle.");
                    break;
            }
        }

        private void HandleMessageFromGuest(ulong senderId, EOnlineMessage type, byte[] message)
        {
            if (type == EOnlineMessage.GameLoaded)
            {
                using var reader = OnlineMessage.Read(message);
                if (reader.ReadInt32() == GameNumber)
                    m_loadedGuests.Add(senderId);
                return;
            }
            if (OnlineMessage.IsGameMessage(type) && IsInGame)
                OnGuestGameMessage?.Invoke(senderId, message);
        }

        // A guest may still be on its way back to the menu when the host launches the next game: the game starts once
        // there (the menu's opening ends the last game, HandleBackInMenu), if the room is still on.
        private IEnumerator LaunchInMenu(int gameNumber, Action<GameModesLauncher> launch)
        {
            yield return new WaitUntil(() => MainMenuContext.Instance && MainMenuContext.Instance.IsInitialized);
            if (!IsInRoom)
            {
                Debug.Log($"[Online] The room was lost before game {gameNumber} started.");
                yield break;
            }
            StartGame(gameNumber);
            launch(MainMenuContext.Instance.GameModesLauncher);
        }

        private static int IndexOf<T>(IReadOnlyList<T> list, T item) where T : class
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == item)
                    return i;
            }
            return -1;
        }

        /// <summary>The level of the tournament at this rank of its config.</summary>
        public static LevelDataAsset TournamentLevel(int levelIndex)
        {
            var tournamentContext = TournamentContext.Instance;
            return tournamentContext ? tournamentContext.ConfigDataAsset.LevelDataAssets[levelIndex] : null;
        }
    }
}
