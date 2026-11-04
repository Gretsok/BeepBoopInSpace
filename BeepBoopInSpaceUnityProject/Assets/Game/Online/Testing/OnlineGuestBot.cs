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
using Game.Gameplay.Flows.Gameplay;
using Game.Gameplay.Flows.Results;
using Game.Gameplay.Flows.Tutorial;
using Game.Global;
using Game.Global.PlayerManagement;
using Game.MainMenu;
using Game.MainMenu.CharacterManagement;
using Game.Online.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

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
    /// </summary>
    public class OnlineGuestBot : MonoBehaviour
    {
        private const string k_Argument = "-online-test-guest";
        private const string k_ScenarioArgument = "-online-test-guest-scenario";
        private const string k_ReportArgument = "-online-test-guest-report";
        private const float k_Timeout = 20 * 60f;
        // Seconds into a game before freezing in it.
        private const float k_FreezeDelay = 3f;

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

        // The scenario.
        private bool m_leavesTheRoomAndComesBack;
        private float m_freezeInGameSeconds;
        private bool m_quitsWhenItsRobotIsDead;
        private bool m_quitsWhileLoading;
        private bool m_neverLoads;
        private bool m_expectsToLoseTheRoom;
        private bool m_requiresAGame;

        private bool m_hasFrozen;
        private bool m_hasLeftAGame;
        private bool m_comesBackAfterLeaving;
        private float m_gameplayStartTime = -1f;
        private GameplayContext m_handledGameplayContext;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfAsked()
        {
            if (!Environment.GetCommandLineArgs().Contains(k_Argument))
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
        }

        private void HandleRoomLost(string reason)
        {
            m_roomsLost++;
            Log($"room lost: {reason}");
        }

        private IEnumerator Start()
        {
            var deadline = Time.realtimeSinceStartup + k_Timeout;
            yield return new WaitUntil(IsMenuReady);

            // One player, with the second robot: the host's first player has the first one.
            m_gamepad = InputSystem.AddDevice<Gamepad>("Guest Bot Gamepad");
            var playerInput = GlobalContext.Instance.PlayerManager.AddPlayerFromDevice(m_gamepad);
            m_player = playerInput.GetComponent<AbstractPlayer>();
            m_player.SetCharacterData(CharacterBankManager.Instance.Characters[1]);
            Log("one player on a virtual gamepad");

            var onlineManager = OnlineManager.Instance;
            onlineManager.OnRoomLost += HandleRoomLost;
            // The host may not have opened its room yet.
            var joined = false;
            yield return JoinTheRoom(60, result => joined = result);
            if (!joined)
            {
                Quit(3, "couldn't join the room");
                yield break;
            }

            if (m_leavesTheRoomAndComesBack)
            {
                yield return new WaitForSecondsRealtime(6f);
                Log("leaving the room");
                MainMenuContext.Instance.MainMenuOrchestrator.SwitchToHubScreen();
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
                    var isSuccess = m_errors == 0 && (m_gamesPlayed > 0 || !m_requiresAGame) && (m_roomsLost > 0 || !m_expectsToLoseTheRoom);
                    Quit(isSuccess ? 0 : 1, $"room closed after {m_gamesPlayed} game(s), {m_roomsLost} room(s) lost, {m_errors} error(s)");
                    yield break;
                }

                var gameplayContext = GameplayContext.Instance;
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

                var isInResults = state is ResultsState;
                if (isInResults && !m_isInResults)
                {
                    m_gamesPlayed++;
                    var pawns = CharactersManager.Instance.CharacterPawns;
                    var scores = string.Join(", ", pawns.Select(pawn => pawn.ReferencesHolder.ScoringController.Score));
                    var moves = m_moves != null ? string.Join(", ", m_moves) : "none";
                    Log($"game {onlineManager.GameNumber}: results, moves {moves}, scores {scores}");
                }
                m_isInResults = isInResults;
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
                yield return onlineManager.JoinRoom(null).ToCoroutine(result => error = result);
                if (error == null)
                    break;
                Log($"joining refused: {error}");
                yield return new WaitForSecondsRealtime(1f);
            }
            if (error == null)
            {
                Log($"in the room as machine {onlineManager.LocalMachineId}, {onlineManager.Roster.Count} players");
                MainMenuContext.Instance.MainMenuOrchestrator.SwitchToOnlineLobbyScreen();
            }
            onDone(error == null);
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
