using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Gameplay;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.Movement;
using Game.Gameplay.Flows.Gameplay;
using Game.Gameplay.Flows.Results;
using Game.Gameplay.Flows.Tutorial;
using Game.Global;
using Game.MainMenu;
using Game.MainMenu.CharacterManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Game.Online.Testing
{
    /// <summary>
    /// A guest that plays on its own, for the online scenario test: the game launched with
    /// "-online-direct &lt;address&gt; -online-test-guest" joins the room at the address with one player on a virtual
    /// gamepad, gets ready in the tutorials, presses random buttons in the games, and quits once the host closes the
    /// room: code 0, or 1 if an error was logged or no game was played, 2 after too long, 3 if it couldn't join.
    /// At each game's results, it logs how many moves of each robot it saw and the scores, for the test to compare
    /// with the host's.
    /// </summary>
    public class OnlineGuestBot : MonoBehaviour
    {
        private const string k_Argument = "-online-test-guest";
        private const float k_Timeout = 20 * 60f;

        private static readonly GamepadButton[] s_actionButtons =
        {
            GamepadButton.South, GamepadButton.North, GamepadButton.East, GamepadButton.West,
            GamepadButton.LeftShoulder, GamepadButton.RightShoulder,
        };

        private int m_errors;
        private int m_gamesPlayed;
        private Gamepad m_gamepad;
        private int m_readyGameNumber = -1;
        private bool m_isInResults;
        private CharactersManager m_countedCharacters;
        private int[] m_moves;
        private readonly System.Random m_random = new(7);

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
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLogMessage;
        }

        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert)
                m_errors++;
        }

        private IEnumerator Start()
        {
            var deadline = Time.realtimeSinceStartup + k_Timeout;
            yield return new WaitUntil(() => GlobalContext.Instance && GlobalContext.Instance.IsInitialized
                                             && MainMenuContext.Instance && MainMenuContext.Instance.IsInitialized
                                             && CharacterBankManager.Instance);

            // One player, with the second robot: the host's first player has the first one.
            m_gamepad = InputSystem.AddDevice<Gamepad>("Guest Bot Gamepad");
            var playerInput = GlobalContext.Instance.PlayerManager.AddPlayerFromDevice(m_gamepad);
            playerInput.GetComponent<Global.PlayerManagement.AbstractPlayer>().SetCharacterData(CharacterBankManager.Instance.Characters[1]);
            Log("one player on a virtual gamepad");

            // The host may not have opened its room yet.
            var onlineManager = OnlineManager.Instance;
            string error = "not tried";
            for (int attempt = 0; attempt < 60 && error != null; attempt++)
            {
                yield return onlineManager.JoinRoom(null).ToCoroutine(result => error = result);
                if (error == null)
                    break;
                Log($"joining failed ({error}), trying again");
                yield return new WaitForSecondsRealtime(1f);
            }
            if (error != null)
            {
                Quit(3, "couldn't join the room");
                yield break;
            }
            Log($"in the room, {onlineManager.Roster.Count} players");
            MainMenuContext.Instance.MainMenuOrchestrator.SwitchToOnlineLobbyScreen();

            var nextPress = 0f;
            while (true)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Quit(2, "timed out");
                    yield break;
                }
                if (!onlineManager.IsInRoom && !onlineManager.IsInGame && MainMenuContext.Instance)
                {
                    Quit(m_errors == 0 && m_gamesPlayed > 0 ? 0 : 1,
                        $"room closed after {m_gamesPlayed} game(s), {m_errors} error(s)");
                    yield break;
                }

                var gameplayContext = GameplayContext.Instance;
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
                    CountMoves();
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
            Debug.Log($"[GuestBot] {message}");
        }

        private static void Quit(int exitCode, string reason)
        {
            Log($"quitting with code {exitCode}: {reason}");
            Application.Quit(exitCode);
        }
    }
}
