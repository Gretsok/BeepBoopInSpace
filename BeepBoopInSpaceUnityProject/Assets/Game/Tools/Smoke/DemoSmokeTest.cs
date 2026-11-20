using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay;
using Game.Gameplay.Flows.Gameplay;
using Game.Gameplay.Flows.Results;
using Game.Gameplay.Flows.Tutorial;
using Game.Global;
using Game.MainMenu;
using Game.MainMenu.CharacterManagement;
using Game.MainMenu.Hub;
using Game.Tournament;
using Game.Tournament.Results.ResultsScene;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Game.Tools.Smoke
{
    /// <summary>
    /// With -demo-smoke, the built game plays two tournaments by itself: a player on the keyboard who doesn't move, and three
    /// bots. The first one starts from the build's own menu and goes to its end; the second one from the menu that came back
    /// from its bundle, with other copies of the characters, and ends at its first win. Only a built game has the
    /// Addressables bundles and renders for real, the scenario tests don't. Saves pictures to the folder after
    /// -demo-smoke-captures, and quits with 0 if all is well, 1 on a logged error (soon after an exception), 2 if it takes too long.
    /// Logs the frame rate of its games and the machine, in one line: "[DemoSmoke] Performance: ...".
    /// </summary>
    public class DemoSmokeTest : MonoBehaviour
    {
        private const string k_Argument = "-demo-smoke";
        private const string k_CapturesArgument = "-demo-smoke-captures";
        private const float k_TimeScale = 3f;
        private const float k_MaxDuration = 20f * 60f;
        private const float k_SecondsAfterAnException = 10f;
        private const int k_ShortTournamentScore = 1;
        // The frames a capture, or the game stopping out of focus, makes long: they say nothing of its performance.
        private const int k_FramesToSkip = 2;

        private int m_errorsCount;
        private bool m_hasException;
        private int m_tournament;
        private string m_capturesFolder;
        private int m_capturesCount;
        private readonly List<float> m_frameDurations = new();
        private int m_framesToSkip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfAsked()
        {
            if (!Environment.GetCommandLineArgs().Contains(k_Argument))
                return;
            var smokeTest = new GameObject(nameof(DemoSmokeTest));
            DontDestroyOnLoad(smokeTest);
            smokeTest.AddComponent<DemoSmokeTest>();
        }

        private void Awake()
        {
            Application.logMessageReceived += HandleLogMessage;
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, k_CapturesArgument);
            m_capturesFolder = index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
            if (m_capturesFolder != null)
                Directory.CreateDirectory(m_capturesFolder);
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLogMessage;
        }

        private void HandleLogMessage(string message, string stackTrace, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert)
            {
                ++m_errorsCount;
                m_hasException |= type == LogType.Exception;
                Debug.Log($"[DemoSmoke] Error logged: {message}");
            }
        }

        private void Start()
        {
            StartCoroutine(PlayRoutine());
            StartCoroutine(TimeoutRoutine());
        }

        // The games themselves, in real time: they run faster than a player's, their frames are rendered as they are.
        private void Update()
        {
            if (m_framesToSkip > 0)
            {
                m_framesToSkip--;
                return;
            }
            var context = GameplayContext.Instance;
            if (context && context.FlowMachine && context.FlowMachine.CurrentState is GameplayState)
                m_frameDurations.Add(Time.unscaledDeltaTime);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            m_framesToSkip = k_FramesToSkip;
        }

        private void LogPerformance()
        {
            var frames = m_frameDurations.Count;
            var slowestFrames = m_frameDurations.OrderByDescending(duration => duration).Take(Mathf.Max(1, frames / 100)).ToList();
            var measure = frames == 0
                ? "no game frame measured"
                : FormattableString.Invariant($"{frames / m_frameDurations.Sum():0.0} FPS on average, {slowestFrames.Count / slowestFrames.Sum():0.0} FPS in the slowest 1%, worst frame {slowestFrames[0] * 1000f:0} ms, {frames} frames");
            var display = FormattableString.Invariant($"{Screen.width}x{Screen.height}, vSync {QualitySettings.vSyncCount}, target {Application.targetFrameRate}, screen {Screen.currentResolution.refreshRateRatio.value:0} Hz");
            Debug.Log($"[DemoSmoke] Performance: {measure}; {display}; GPU {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}); " +
                      $"CPU {SystemInfo.processorType} ({SystemInfo.processorCount} threads); {SystemInfo.systemMemorySize} MB of RAM.");
        }

        private IEnumerator TimeoutRoutine()
        {
            var deadline = Time.realtimeSinceStartup + k_MaxDuration;
            yield return new WaitUntil(() => m_hasException || Time.realtimeSinceStartup > deadline);
            if (m_hasException)
            {
                // Its game usually stays stuck: the time limit would only say so much later.
                yield return new WaitForSecondsRealtime(k_SecondsAfterAnException);
                Debug.Log("[DemoSmoke] An exception was logged: stopping.");
                LogPerformance();
                Application.Quit(1);
                yield break;
            }
            Debug.Log("[DemoSmoke] Too long: stopping.");
            LogPerformance();
            Application.Quit(2);
        }

        private IEnumerator PlayRoutine()
        {
            yield return new WaitUntil(() => MainMenuContext.Instance && GlobalContext.Instance
                                             && GlobalContext.Instance.IsInitialized && MainMenuContext.Instance.IsInitialized
                                             && CharacterBankManager.Instance);
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture("home");

            Debug.Log("[DemoSmoke] A player joins, the tournament starts.");
            var playerManager = GlobalContext.Instance.PlayerManager;
            playerManager.AddPlayerFromDevice(Keyboard.current);
            yield return new WaitUntil(() => playerManager.Players.Count == 1);
            playerManager.Players[0].SetCharacterData(CharacterBankManager.Instance.Characters[0]);
            yield return PlayTournament();

            // As a player would: from the character selection the menu opens on, to the hub.
            Debug.Log("[DemoSmoke] Back in the menu, from its bundle: a second tournament starts.");
            var mainMenu = MainMenuContext.Instance;
            mainMenu.MainMenuOrchestrator.SwitchToHubScreen();
            yield return new WaitUntil(() => mainMenu.FlowMachine.CurrentState is HubState);
            yield return new WaitForSecondsRealtime(1f);
            yield return PlayTournament();

            LogPerformance();
            Debug.Log($"[DemoSmoke] Done, {m_errorsCount} error(s).");
            Application.Quit(m_errorsCount > 0 ? 1 : 0);
        }

        private IEnumerator PlayTournament()
        {
            ++m_tournament;
            Time.timeScale = k_TimeScale;
            MainMenuContext.Instance.GameModesLauncher.LaunchTournament();
            CheckCharacters();

            yield return new WaitUntil(() => TournamentContext.Instance);
            if (m_tournament > 1)
                TournamentContext.Instance.ResultsManager.ScoreToReachOverride = k_ShortTournamentScore;
            var games = 0;
            while (TournamentContext.Instance)
            {
                var hasGame = false;
                var context = GameplayContext.Instance;
                if (context && context.IsInitialized)
                {
                    hasGame = true;
                    yield return PlayGame(context, ++games);
                }

                var results = FindAnyObjectByType<TournamentResultsCanvas>();
                if (results && results.IsShown)
                {
                    hasGame = true;
                    yield return new WaitForSecondsRealtime(1f);
                    if (games <= 1)
                        yield return Capture("tournament-results");
                    Press(results.GetComponent<UIDocument>().rootVisualElement.Q<Button>("ok-button"));
                    yield return new WaitForSecondsRealtime(1f);
                }
                if (!hasGame)
                    yield return null;
            }

            Time.timeScale = 1f;
            Debug.Log($"[DemoSmoke] Tournament {m_tournament} over after {games} game(s). Errors: {m_errorsCount}.");
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu" && MainMenuContext.Instance
                                             && MainMenuContext.Instance.IsInitialized);
            yield return new WaitForSecondsRealtime(2f);
            yield return Capture("back-in-the-menu");
        }

        // Right after the launch: the copies of the characters of this menu's bank, each player its own, no robot without its art.
        private void CheckCharacters()
        {
            var bank = CharacterBankManager.Instance.Characters;
            var players = GlobalContext.Instance.PlayerManager.GamePlayers;
            var names = players.Select(player => player.CharacterDataAsset ? player.CharacterDataAsset.name : "nobody").ToList();
            Debug.Log($"[DemoSmoke] Tournament {m_tournament} with: {string.Join(", ", names)}.");
            if (!players.All(player => bank.Contains(player.CharacterDataAsset)))
                Debug.LogError("[DemoSmoke] A player's character isn't one of the bank's.");
            if (names.Distinct().Count() < names.Count)
                Debug.LogError("[DemoSmoke] Two players have the same character.");
            if (players.Any(player => player.IsBot && player.CharacterDataAsset && !player.CharacterDataAsset.SelectionIcon))
                Debug.LogError("[DemoSmoke] A robot has a character without its art.");
        }

        private IEnumerator PlayGame(GameplayContext context, int game)
        {
            // The context is initialized before its level is loaded: its CurrentLevelInfoManager comes after (GameplayContext.Start).
            yield return new WaitUntil(() => !context || context.CurrentLevelInfoManager);
            if (!context)
                yield break;
            Debug.Log($"[DemoSmoke] Game {game}: {context.CurrentLevelInfoManager.CurrentLevelDataAsset.name}");
            var flowMachine = context.FlowMachine;

            yield return new WaitUntil(() => !context || flowMachine.CurrentState is TutorialState);
            if (!context)
                yield break;
            yield return new WaitForSecondsRealtime(1f);
            if (game == 1)
                yield return Capture("tutorial");
            var tutorialPanel = context.UIManager.GetPanel<TutorialPanel>();
            tutorialPanel.SetPlayerStatus(GlobalContext.Instance.PlayerManager.Players[0], true);

            yield return new WaitUntil(() => !context || flowMachine.CurrentState is not TutorialState);
            yield return new WaitForSecondsRealtime(8f);
            if (!context)
                yield break;
            if (game <= 2)
                yield return Capture($"game-{game}");

            yield return new WaitUntil(() => !context || flowMachine.CurrentState is ResultsState);
            if (!context)
                yield break;
            var resultsPanel = context.UIManager.GetPanel<ResultsPanel>();
            var button = resultsPanel.GetComponent<UIDocument>().rootVisualElement.Q<Button>("back-to-menu-button");
            yield return new WaitUntil(() => !context || resultsPanel.IsButtonShown);
            yield return new WaitForSecondsRealtime(1f);
            if (!context)
                yield break;
            if (game == 1)
                yield return Capture("results");
            Press(button);
            yield return new WaitUntil(() => !context);
        }

        private static void Press(Button button)
        {
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }

        private IEnumerator Capture(string name)
        {
            if (m_capturesFolder == null)
                yield break;
            yield return new WaitForEndOfFrame();
            var tournament = m_tournament > 1 ? $"tournament-{m_tournament}-" : string.Empty;
            ScreenCapture.CaptureScreenshot(Path.Combine(m_capturesFolder, $"{++m_capturesCount:00}-{tournament}{name}.png"));
            m_framesToSkip = k_FramesToSkip;
            yield return null;
        }
    }
}
