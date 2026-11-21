using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Gameplay;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.Movement;
using Game.Gameplay.Flows.Finish;
using Game.Gameplay.Flows.Gameplay;
using Game.Gameplay.Flows.Results;
using Game.Gameplay.Flows.Tutorial;
using Game.Global;
using Game.Global.PlayerManagement;
using Game.MainMenu;
using Game.MainMenu.CharacterManagement;
using Game.MainMenu.Online;
using Game.Tournament;
using Game.Online.Gameplay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Game.Online.Testing
{
    /// <summary>
    /// A guest that plays on its own, for the online scenario tests: the game launched with
    /// "-online-direct &lt;address&gt; -online-test-guest" joins the room at the address with one player on a virtual
    /// gamepad, gets ready in the tutorials, presses random buttons in the games, and quits once the host closes the
    /// room: code 0, or 1 if an error was logged or no game was played, 2 after too long, 3 if it couldn't join.
    /// At each game's results, it logs how many moves of each robot it saw and the scores, for the test to compare
    /// with the host's.
    /// "-online-test-guest-scenario a,b,..." adds what goes wrong (see <see cref="ReadScenario"/>): leaving and coming
    /// back, freezing (a network cut, or a lag spike when shorter than the transport's timeout), leaving a game...
    /// "-online-test-guest-report &lt;file&gt;" also writes the bot's lines there as they happen: the player's log only
    /// reaches the disk by blocks.
    /// It also fails (an error) when a game comes back to life while it unloads (a state entered after the end state),
    /// or when the menu doesn't open on the online screen saying why after the room was lost.
    /// "-relay-test-host &lt;file&gt;" and "-relay-test-guest &lt;file&gt;" play a real room, through Unity's services and
    /// Relay (no -online-direct). The host opens it, writes its code in the file, waits for the guest, launches Points
    /// Rush, leaves the results, and closes the room once back in it; the guest reads the code in the file and joins.
    /// More exit codes then: 4 room not opened, 5 a code the online screen can't type, 6 not on Relay, 7 no code or no
    /// guest in time, 8 no game launched, 9 the host lost the room or the guest left it, 10 the room never finished closing.
    /// </summary>
    public class OnlineGuestBot : MonoBehaviour
    {
        private const string k_Argument = "-online-test-guest";
        private const string k_ScenarioArgument = "-online-test-guest-scenario";
        private const string k_ReportArgument = "-online-test-guest-report";
        private const string k_RelayHostArgument = "-relay-test-host";
        private const string k_RelayGuestArgument = "-relay-test-guest";
        private const string k_RelayLevel = " - Points Rush - ";
        // How long the host waits for a guest, and the guest for the host's code.
        private const float k_RelayWaitSeconds = 180f;
        // A guest that just came needs a moment to be in the room on its side, one just back from a game too.
        private const float k_RelayLaunchDelay = 3f;
        private const float k_RelayCloseDelay = 5f;
        private const float k_RelayClosingTimeout = 20f;
        private const float k_Timeout = 20 * 60f;
        // Seconds into a game before freezing in it.
        private const float k_FreezeDelay = 3f;
        // Seconds into the results before freezing: the host is about to leave them.
        private const float k_FreezeAtResultsDelay = 1.5f;
        // The end of a game (FinishState in the base game scene), in game seconds: it then asks for the results.
        private const float k_FinishSeconds = 4f;

        private static readonly GamepadButton[] s_actionButtons =
        {
            GamepadButton.South, GamepadButton.North, GamepadButton.East, GamepadButton.West,
            GamepadButton.LeftShoulder, GamepadButton.RightShoulder,
        };

        private int m_errors;
        private int m_gamesPlayed;
        private int m_roomsLost;
        private Gamepad m_gamepad;
        private AbstractPlayer m_player;
        private int m_readyGameNumber = -1;
        private bool m_isInResults;
        private CharactersManager m_countedCharacters;
        private int[] m_moves;
        private readonly System.Random m_random = new(7);
        private static string s_reportPath;

        // A real room: the file that gives its code to the guest, and this machine's part in it.
        private string m_relayCodeFile;
        private bool m_isRelayHost;
        private string m_roomCode;
        private bool m_hasGuestStayed;

        // The scenario.
        private bool m_leavesTheRoomAndComesBack;
        private float m_freezeInGameSeconds;
        private float m_freezeAtResultsSeconds;
        private float m_freezeAtLaunchSeconds;
        private float m_freezeAsFinishEndsSeconds;
        private bool m_quitsWhenItsRobotIsDead;
        private bool m_quitsWhileLoading;
        private bool m_neverLoads;
        private bool m_expectsToLoseTheRoom;
        private bool m_requiresAGame;

        private bool m_hasFrozen;
        private bool m_hasFrozenAtResults;
        private bool m_hasFrozenAtLaunch;
        private bool m_hasFrozenAsFinishEnds;
        private float m_finishStartTime = -1f;
        private float m_resultsStartTime = -1f;
        private bool m_isGameEnding;
        private bool m_expectsTheOnlineScreen;
        private bool m_hasLeftAGame;
        private bool m_comesBackAfterLeaving;
        private float m_gameplayStartTime = -1f;
        private GameplayContext m_handledGameplayContext;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfAsked()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains(k_Argument) && !args.Contains(k_RelayHostArgument) && !args.Contains(k_RelayGuestArgument))
                return;
            var bot = new GameObject(nameof(OnlineGuestBot));
            DontDestroyOnLoad(bot);
            bot.AddComponent<OnlineGuestBot>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Application.logMessageReceived += HandleLogMessage;
            s_reportPath = ArgumentAfter(k_ReportArgument);
            m_isRelayHost = ArgumentAfter(k_RelayHostArgument) != null;
            m_relayCodeFile = ArgumentAfter(k_RelayHostArgument) ?? ArgumentAfter(k_RelayGuestArgument);
            ReadScenario();
        }

        private static string ArgumentAfter(string argument)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, argument);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLogMessage;
            if (OnlineManager.Instance)
                OnlineManager.Instance.OnRoomLost -= HandleRoomLost;
        }

        // rejoin: leaves the room once in it, then comes back. freeze-in-game:N: freezes N seconds in the first game.
        // freeze-at-results:N: freezes N seconds in the first game's results, as the host leaves them.
        // freeze-at-launch:N: freezes N seconds as the first game launches from the menu (its scene not loading yet).
        // freeze-as-finish-ends:N: freezes N seconds just before the first game's end (FinishState) would ask for its
        // results, a request the host's driving makes this machine ignore (in the results already).
        // quit-when-dead: leaves the first game (pause, Menu principal) once its robot is dead, then joins the room
        // again, refused until the host is back in it. quit-while-loading: leaves the first game as it starts loading,
        // then joins again. stuck-loading: never tells the host its game is loaded. expect-room-lost: the room is lost
        // (host cut off or leaving): no game needs to end, the room must be lost.
        private void ReadScenario()
        {
            var scenario = ArgumentAfter(k_ScenarioArgument) ?? string.Empty;
            foreach (var token in scenario.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = token.Split(':');
                switch (parts[0])
                {
                    case "rejoin":
                        m_leavesTheRoomAndComesBack = true;
                        break;
                    case "freeze-in-game":
                        m_freezeInGameSeconds = parts.Length > 1 ? float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 3f;
                        break;
                    case "freeze-at-results":
                        m_freezeAtResultsSeconds = parts.Length > 1 ? float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 8f;
                        break;
                    case "freeze-at-launch":
                        m_freezeAtLaunchSeconds = parts.Length > 1 ? float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 3f;
                        break;
                    case "freeze-as-finish-ends":
                        m_freezeAsFinishEndsSeconds = parts.Length > 1 ? float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 3f;
                        break;
                    case "quit-when-dead":
                        m_quitsWhenItsRobotIsDead = true;
                        break;
                    case "quit-while-loading":
                        m_quitsWhileLoading = true;
                        break;
                    case "stuck-loading":
                        m_neverLoads = true;
                        break;
                    case "expect-room-lost":
                        m_expectsToLoseTheRoom = true;
                        break;
                    default:
                        Debug.LogError($"[GuestBot] Unknown scenario step '{token}'.");
                        break;
                }
            }
            m_requiresAGame = !m_quitsWhenItsRobotIsDead && !m_quitsWhileLoading && !m_neverLoads && !m_expectsToLoseTheRoom;
            Log($"scenario '{scenario}'");
        }

        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert)
                m_errors++;
            else if (type == LogType.Log)
                CheckNothingStartsAfterTheEnd(condition);
        }

        // "[FM] FlowMachine - EndState - ENTER": the game unloads. Until a flow machine starts running again (the menu's,
        // the next game's), a state that enters is the game coming back to life while it unloads.
        private void CheckNothingStartsAfterTheEnd(string message)
        {
            if (!message.StartsWith("[FM] ", StringComparison.Ordinal))
                return;
            if (message.EndsWith(" - started running.", StringComparison.Ordinal))
                m_isGameEnding = false;
            else if (message.Contains(" - EndState - ENTER"))
                m_isGameEnding = true;
            else if (m_isGameEnding && message.TrimEnd().EndsWith(" - ENTER", StringComparison.Ordinal))
            {
                m_errors++;
                Log($"ERROR a state entered after the game's end state: {message}");
            }
        }

        private void HandleRoomLost(string reason)
        {
            m_roomsLost++;
            m_expectsTheOnlineScreen = true;
            Log($"room lost: {reason}");
            Log($"room lost while {WhereItIs()}");
        }

        private static string WhereItIs()
        {
            var gameplayContext = GameplayContext.Instance;
            if (gameplayContext && gameplayContext.FlowMachine)
            {
                var state = gameplayContext.FlowMachine.CurrentState;
                return $"in game state {(state ? state.GetType().Name : "none")}";
            }
            if (MainMenuContext.Instance)
                return OnlineManager.Instance.IsInGame ? "launching a game from the menu" : "in the menu";
            return TournamentContext.Instance ? "between tournament games" : "elsewhere";
        }

        // Back in the menu after the room was lost: the online screen, which has said why (the reason taken).
        private IEnumerator CheckTheOnlineScreenSaysWhy()
        {
            if (!m_expectsTheOnlineScreen)
                yield break;
            var flowMachine = MainMenuContext.Instance.FlowMachine;
            var timeout = Time.realtimeSinceStartup + 10f;
            while (!flowMachine.CurrentState && Time.realtimeSinceStartup < timeout)
                yield return null;
            // A request to switch screens may still be on its way.
            yield return null;
            yield return null;
            var screen = flowMachine.CurrentState ? flowMachine.CurrentState.GetType().Name : "none";
            Log($"back in the menu on {screen}");
            if (flowMachine.CurrentState is not OnlineState)
                Debug.LogError($"[GuestBot] The room was lost, but the menu opened on {screen} instead of the online screen.");
            else if (OnlineManager.Instance.HasLostRoomReason)
                Debug.LogError("[GuestBot] The room was lost, but the online screen didn't say why.");
        }

        private IEnumerator Start()
        {
            var deadline = Time.realtimeSinceStartup + k_Timeout;
            yield return new WaitUntil(IsMenuReady);

            // One player: as a guest, with the second robot, the host's first player having the first one.
            m_gamepad = InputSystem.AddDevice<Gamepad>("Guest Bot Gamepad");
            var playerInput = GlobalContext.Instance.PlayerManager.AddPlayerFromDevice(m_gamepad);
            m_player = playerInput.GetComponent<AbstractPlayer>();
            m_player.SetCharacterData(CharacterBankManager.Instance.Characters[m_isRelayHost ? 0 : 1]);
            Log("one player on a virtual gamepad");

            var onlineManager = OnlineManager.Instance;
            onlineManager.OnRoomLost += HandleRoomLost;
            var joined = false;
            if (m_isRelayHost)
            {
                yield return HostTheRelayRoom(result => joined = result);
                if (!joined)
                    yield break;
            }
            else
            {
                if (m_relayCodeFile != null)
                {
                    yield return ReadTheRoomCode();
                    if (m_roomCode == null)
                    {
                        Quit(7, "no room code came");
                        yield break;
                    }
                    Log($"room code {m_roomCode}");
                }
                // The host may not have opened its room yet.
                yield return JoinTheRoom(60, result => joined = result);
                if (!joined)
                {
                    Quit(3, "couldn't join the room");
                    yield break;
                }
                if (m_relayCodeFile != null && !IsOnRelay)
                {
                    Quit(6, "the connection doesn't go through Relay");
                    yield break;
                }
            }

            if (m_leavesTheRoomAndComesBack)
            {
                yield return new WaitForSecondsRealtime(6f);
                Log("leaving the room");
                MainMenuContext.Instance.MainMenuOrchestrator.BackToOnlineScreen();
                onlineManager.LeaveRoom();
                yield return new WaitForSecondsRealtime(3f);
                yield return JoinTheRoom(30, result => joined = result);
                if (!joined)
                {
                    Quit(3, "couldn't join the room again");
                    yield break;
                }
            }

            var nextPress = 0f;
            while (true)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Quit(2, "timed out");
                    yield break;
                }
                if (!onlineManager.IsInRoom && !onlineManager.IsInGame && onlineManager.Status == ERoomStatus.None && IsMenuReady())
                {
                    if (!CheckNoGameSceneLeft())
                    {
                        Quit(1, "game scenes left in the menu");
                        yield break;
                    }
                    yield return CheckTheOnlineScreenSaysWhy();
                    m_expectsTheOnlineScreen = false;
                    if (m_comesBackAfterLeaving)
                    {
                        // Refused while the host plays the game this machine left: until the host is back in the room.
                        m_comesBackAfterLeaving = false;
                        yield return JoinTheRoom(240, result => joined = result);
                        if (!joined)
                        {
                            Quit(3, "couldn't join the room after leaving the game");
                            yield break;
                        }
                        continue;
                    }
                    if (m_relayCodeFile != null)
                    {
                        // A session that never finishes closing would hold the next room back, and what it logs counts.
                        var closingDeadline = Time.realtimeSinceStartup + k_RelayClosingTimeout;
                        while (onlineManager.IsClosing && Time.realtimeSinceStartup < closingDeadline)
                            yield return null;
                        if (onlineManager.IsClosing)
                        {
                            Quit(10, "the room never finished closing");
                            yield break;
                        }
                        if (m_isRelayHost && (m_roomsLost > 0 || !m_hasGuestStayed))
                        {
                            Quit(9, $"{m_roomsLost} room(s) lost, guest in the room at the end: {m_hasGuestStayed}");
                            yield break;
                        }
                    }
                    var isSuccess = m_errors == 0 && (m_gamesPlayed > 0 || !m_requiresAGame) && (m_roomsLost > 0 || !m_expectsToLoseTheRoom);
                    Quit(isSuccess ? 0 : 1, $"room closed after {m_gamesPlayed} game(s), {m_roomsLost} room(s) lost, {m_errors} error(s)");
                    yield break;
                }

                // Back in the room after its game, the host of a real room closes it.
                if (m_isRelayHost && onlineManager.IsInRoom && !onlineManager.IsInGame && m_gamesPlayed > 0 && IsMenuReady())
                {
                    yield return CloseTheRelayRoom();
                    continue;
                }

                var gameplayContext = GameplayContext.Instance;
                // Launching from the menu: the launcher has just been asked, the game's scene isn't loading yet.
                if (m_freezeAtLaunchSeconds > 0f && !m_hasFrozenAtLaunch && onlineManager.IsInGame && !gameplayContext
                    && MainMenuContext.Instance)
                {
                    m_hasFrozenAtLaunch = true;
                    Log("launching a game from the menu");
                    Freeze(m_freezeAtLaunchSeconds);
                }
                // Once the game's context has started (its sync is attached then).
                if (gameplayContext && gameplayContext != m_handledGameplayContext && gameplayContext.TryGetComponent(out GameplaySync sync))
                {
                    m_handledGameplayContext = gameplayContext;
                    m_gameplayStartTime = -1f;
                    if (!HandleGameStart(gameplayContext, sync))
                    {
                        yield return null;
                        continue;
                    }
                }

                var state = gameplayContext && gameplayContext.FlowMachine ? gameplayContext.FlowMachine.CurrentState : null;
                if (state is TutorialState && m_readyGameNumber != onlineManager.GameNumber)
                {
                    // Once the tutorial's controllers listen.
                    yield return new WaitForSecondsRealtime(1f);
                    m_readyGameNumber = onlineManager.GameNumber;
                    Log($"game {onlineManager.GameNumber}: ready");
                    yield return Press(GamepadButton.South);
                }
                if (state is GameplayState)
                {
                    CountMoves();
                    if (m_gameplayStartTime < 0f)
                        m_gameplayStartTime = Time.realtimeSinceStartup;
                    if (m_freezeInGameSeconds > 0f && !m_hasFrozen && Time.realtimeSinceStartup > m_gameplayStartTime + k_FreezeDelay)
                    {
                        m_hasFrozen = true;
                        Freeze(m_freezeInGameSeconds);
                    }
                    if (m_quitsWhenItsRobotIsDead && !m_hasLeftAGame && IsOwnRobotDead())
                    {
                        Log($"game {onlineManager.GameNumber}: leaving the game while its robot is dead");
                        LeaveTheGame(gameplayContext);
                        yield return null;
                        continue;
                    }
                }
                if (state is GameplayState && Time.realtimeSinceStartup >= nextPress)
                {
                    nextPress = Time.realtimeSinceStartup + 0.1f + (float)m_random.NextDouble() * 0.3f;
                    yield return Press(s_actionButtons[m_random.Next(s_actionButtons.Length)]);
                }

                // Game time: a freeze doesn't count in it (a frame's time is capped), the end of game's timer neither.
                if (state is FinishState && m_finishStartTime < 0f)
                    m_finishStartTime = Time.time;
                if (m_freezeAsFinishEndsSeconds > 0f && !m_hasFrozenAsFinishEnds && m_finishStartTime >= 0f
                    && Time.time > m_finishStartTime + k_FinishSeconds - 0.15f)
                {
                    m_hasFrozenAsFinishEnds = true;
                    Log($"the end of game is about to ask for its results, in {(state ? state.GetType().Name : "no state")}");
                    Freeze(m_freezeAsFinishEndsSeconds);
                }

                var isInResults = state is ResultsState;
                if (isInResults && m_freezeAtResultsSeconds > 0f && !m_hasFrozenAtResults && m_resultsStartTime >= 0f
                    && Time.realtimeSinceStartup > m_resultsStartTime + k_FreezeAtResultsDelay)
                {
                    m_hasFrozenAtResults = true;
                    Freeze(m_freezeAtResultsSeconds);
                }
                if (isInResults && !m_isInResults)
                {
                    m_resultsStartTime = Time.realtimeSinceStartup;
                    m_gamesPlayed++;
                    var pawns = CharactersManager.Instance.CharacterPawns;
                    var scores = string.Join(", ", pawns.Select(pawn => pawn.ReferencesHolder.ScoringController.Score));
                    var moves = m_moves != null ? string.Join(", ", m_moves) : "none";
                    Log($"game {onlineManager.GameNumber}: results, moves {moves}, scores {scores}");
                }
                m_isInResults = isInResults;
                if (m_isRelayHost && isInResults)
                    yield return LeaveTheResults(gameplayContext);
                yield return null;
            }
        }

        // A game scene just loaded. False when the bot leaves it right away.
        private bool HandleGameStart(GameplayContext gameplayContext, GameplaySync sync)
        {
            if (m_quitsWhileLoading && !m_hasLeftAGame)
            {
                Log("leaving the game while it loads");
                LeaveTheGame(gameplayContext);
                return false;
            }
            if (m_neverLoads)
            {
                // Its game loads, but never says so: the host waits for it.
                Log("never telling the host the game is loaded");
                sync.enabled = false;
            }
            return true;
        }

        // Pause, Menu principal: this machine leaves the room, and comes back once in the menu.
        private void LeaveTheGame(GameplayContext gameplayContext)
        {
            m_hasLeftAGame = true;
            m_expectsTheOnlineScreen = false;
            m_comesBackAfterLeaving = true;
            gameplayContext.PauseMenuManager.QuitToMainMenu();
        }

        private bool IsOwnRobotDead()
        {
            var charactersManager = CharactersManager.Instance;
            var slot = GlobalContext.Instance.PlayerManager.GamePlayers.ToList().IndexOf(m_player);
            if (!charactersManager || slot < 0 || slot >= charactersManager.CharacterPawns.Count)
                return false;
            return !charactersManager.CharacterPawns[slot].ReferencesHolder.DeathController.IsAlive;
        }

        // The main thread stops: no heartbeat, no acknowledgement, as with a network cut or a frozen machine.
        private static void Freeze(float seconds)
        {
            Log($"freezing {seconds:0.#} s");
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
            Log("unfrozen");
        }

        private IEnumerator JoinTheRoom(int attempts, Action<bool> onDone)
        {
            var onlineManager = OnlineManager.Instance;
            string error = "not tried";
            for (int attempt = 0; attempt < attempts && error != null; attempt++)
            {
                yield return onlineManager.JoinRoom(m_roomCode).ToCoroutine(result => error = result);
                if (error == null)
                    break;
                Log($"joining refused: {error}");
                yield return new WaitForSecondsRealtime(1f);
            }
            if (error == null)
            {
                m_expectsTheOnlineScreen = false;
                Log($"in the room as machine {onlineManager.LocalMachineId}, {onlineManager.Roster.Count} players");
                MainMenuContext.Instance.MainMenuOrchestrator.SwitchToOnlineLobbyScreen();
            }
            onDone(error == null);
        }

        private static bool IsOnRelay => NetworkManager.Singleton
                                         && NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport { Protocol: UnityTransport.ProtocolType.RelayUnityTransport };

        // What the boxes of the online screen can type (RoomCodeField).
        private static bool CanBeTyped(string code)
        {
            return code != null && code.Length == RoomCodeField.k_Length
                                && code.All(character => character is >= 'A' and <= 'Z' or >= '0' and <= '9');
        }

        // Opens the room, gives its code to the guest, and launches the game once the guest is in. It quits on a failure.
        private IEnumerator HostTheRelayRoom(Action<bool> onDone)
        {
            var onlineManager = OnlineManager.Instance;
            string error = "not tried";
            yield return onlineManager.CreateRoom().ToCoroutine(result => error = result);
            if (error != null || !onlineManager.IsInRoom)
            {
                Quit(4, $"couldn't open the room: {error}");
                yield break;
            }
            var code = onlineManager.RoomCode;
            Log($"room open, code {code}");
            if (!CanBeTyped(code))
            {
                Quit(5, $"the online screen can't type the code {code}");
                yield break;
            }
            if (!IsOnRelay)
            {
                Quit(6, "the connection doesn't go through Relay");
                yield break;
            }
            MainMenuContext.Instance.MainMenuOrchestrator.SwitchToOnlineLobbyScreen();

            // Whole or not there: the guest reads the file as soon as it exists.
            var temporaryFile = m_relayCodeFile + ".tmp";
            System.IO.File.WriteAllText(temporaryFile, code);
            System.IO.File.Move(temporaryFile, m_relayCodeFile);

            var deadline = Time.realtimeSinceStartup + k_RelayWaitSeconds;
            while (onlineManager.IsInRoom && !onlineManager.CanLaunch && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (!onlineManager.CanLaunch)
            {
                Quit(onlineManager.IsInRoom ? 7 : 9, "no guest joined the room");
                yield break;
            }
            Log($"{onlineManager.Roster.Count} players in the room");
            yield return new WaitForSecondsRealtime(k_RelayLaunchDelay);

            var level = onlineManager.TrainingLevels.FirstOrDefault(candidate => candidate.name.Contains(k_RelayLevel));
            if (level)
            {
                Log($"launching {level.name}");
                onlineManager.LaunchTraining(level);
            }
            if (!onlineManager.IsInGame)
            {
                Quit(8, "the game couldn't be launched");
                yield break;
            }
            onDone(true);
        }

        private IEnumerator ReadTheRoomCode()
        {
            var deadline = Time.realtimeSinceStartup + k_RelayWaitSeconds;
            while (m_roomCode == null && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                try
                {
                    if (System.IO.File.Exists(m_relayCodeFile))
                        m_roomCode = System.IO.File.ReadAllText(m_relayCodeFile).Trim();
                }
                catch (System.IO.IOException e)
                {
                    Log($"the room code can't be read yet: {e.Message}");
                }
            }
        }

        // The host leaves the results for everyone once they can be left, as its player's button would.
        private static IEnumerator LeaveTheResults(GameplayContext gameplayContext)
        {
            var resultsPanel = gameplayContext.UIManager.GetPanel<ResultsPanel>();
            if (!resultsPanel.IsButtonShown)
                yield break;
            Log("leaving the results");
            var button = resultsPanel.GetComponent<UIDocument>().rootVisualElement.Q<Button>("back-to-menu-button");
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
            yield return new WaitForSecondsRealtime(1f);
        }

        private IEnumerator CloseTheRelayRoom()
        {
            yield return new WaitForSecondsRealtime(k_RelayCloseDelay);
            var onlineManager = OnlineManager.Instance;
            m_hasGuestStayed = onlineManager.Roster.Count >= OnlineManager.k_MinPlayers;
            Log($"closing the room, {onlineManager.Roster.Count} players in it");
            onlineManager.LeaveRoom();
            MainMenuContext.Instance.MainMenuOrchestrator.BackToOnlineScreen();
        }

        private static bool IsMenuReady()
        {
            return GlobalContext.Instance && GlobalContext.Instance.IsInitialized
                                          && MainMenuContext.Instance && MainMenuContext.Instance.IsInitialized
                                          && CharacterBankManager.Instance;
        }

        // Back in the menu, nothing of the game may remain: a scene still loading when the game was left would.
        private static bool CheckNoGameSceneLeft()
        {
            var scenes = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                scenes.Add(SceneManager.GetSceneAt(i).name);
            if (scenes.Count <= 1)
                return true;
            Debug.LogError($"[GuestBot] Scenes loaded with the main menu: {string.Join(", ", scenes)}");
            return false;
        }

        // The moves of each robot, as the host's messages play them here.
        private void CountMoves()
        {
            var charactersManager = CharactersManager.Instance;
            if (!charactersManager || charactersManager == m_countedCharacters)
                return;
            m_countedCharacters = charactersManager;
            var pawns = charactersManager.CharacterPawns;
            m_moves = new int[pawns.Count];
            for (int i = 0; i < pawns.Count; i++)
            {
                var index = i;
                pawns[i].ReferencesHolder.MovementController.OnPositionUpdated += (CharacterMovementController _, Cell _) => m_moves[index]++;
            }
        }

        private IEnumerator Press(GamepadButton button)
        {
            InputSystem.QueueStateEvent(m_gamepad, new GamepadState().WithButton(button));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(m_gamepad, new GamepadState());
            yield return null;
        }

        private static void Log(string message)
        {
            var line = $"[GuestBot] {message}";
            Debug.Log(line);
            if (s_reportPath == null)
                return;
            try
            {
                System.IO.File.AppendAllText(s_reportPath, line + Environment.NewLine);
            }
            catch (System.IO.IOException e)
            {
                Debug.LogWarning($"[GuestBot] The report can't be written: {e.Message}");
            }
        }

        private static void Quit(int exitCode, string reason)
        {
            Log($"quitting with code {exitCode}: {reason}");
            Application.Quit(exitCode);
        }
    }
}
