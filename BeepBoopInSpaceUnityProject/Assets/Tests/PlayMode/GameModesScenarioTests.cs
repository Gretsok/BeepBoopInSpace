using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static Game.Tests.PlayMode.ScenarioDriver;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// End-to-end scenarios with four virtual gamepads: main menu, character selection, a game mode, a full game,
    /// and back to the main menu. Everything is driven through input, the way players would.
    /// Run from Window > General > Test Runner > PlayMode, or headless with -runTests -testPlatform PlayMode.
    /// </summary>
    public class GameModesScenarioTests
    {
        // Speeds up the 120s game timer and the results close-ups. Set to 1 to watch a scenario in real time.
        private const float k_TimeScale = 4f;
        private const int k_MaxTournamentGames = 12;

        // Training level names are "SwampArena - <mode> - LevelDataAsset".
        public static readonly string[] TrainingModes =
        {
            "Points Rush",
            "Shooters",
            "Last Man Standing",
            "Points Rush in Hell",
            "Meteorites Last Man Standing",
            "Meteorites Survivors",
            "Shooters in Hell",
        };

        // Errors the game is known to log. They are reported but don't fail a scenario: remove an entry once it's fixed.
        private static readonly string[] k_KnownErrors = { };

        private ScenarioDriver m_driver;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // The devices live for the whole run: players stay paired to them between scenarios, like in the real game.
            m_driver = new ScenarioDriver();
            m_driver.AddGamepads();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Time.timeScale = 1f;
            m_driver.RemoveGamepads();
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            m_driver.BeginTest(k_KnownErrors);
            Time.timeScale = k_TimeScale;
            yield return GoToMainMenuHome();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            m_driver.EndTest();
            yield break;
        }

        [UnityTest, Order(1), Timeout(15 * 60 * 1000)]
        public IEnumerator Training_PlaysAGameThenReturnsToMenu([ValueSource(nameof(TrainingModes))] string mode)
        {
            // Errors are collected and reported together by AssertNoErrors. The test framework resets this flag
            // between set-up and the test body, so it is set here too.
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(Array.IndexOf(TrainingModes, mode) + 1);

            yield return JoinFourPlayersAndOpenHub();

            m_driver.Step("Hub: open Training");
            yield return m_driver.Select(HubButton("m_trainingButton"));
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the level selection screen", () => MenuState == "LevelSelectionState");

            m_driver.Step($"Level selection: pick '{mode}'");
            var levelSelection = FindActive("Game.MainMenu.LevelSelection.LevelSelectionScreen");
            yield return m_driver.WaitUntil("the level widgets", () => LevelWidgets(levelSelection).Any());
            var widget = LevelWidgets(levelSelection).FirstOrDefault(w => LevelNameOf(w).Contains($" - {mode} - "));
            if (!widget)
                m_driver.Fail($"No level for '{mode}'. Available: {string.Join(", ", LevelWidgets(levelSelection).Select(LevelNameOf))}");
            yield return m_driver.Select(widget);
            yield return m_driver.Submit();

            var played = new List<string>();
            yield return PlayOneGame(random, played);
            Assert.That(played[0], Does.Contain($" - {mode} - "), "The game loaded another level than the one selected.");

            m_driver.Step("Back to the main menu");
            yield return m_driver.WaitUntil("the main menu home screen", () => IsOnMainMenuHome);

            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(2), Timeout(40 * 60 * 1000)]
        public IEnumerator Tournament_PlaysUntilAChampionThenReturnsToMenu()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(42);

            yield return JoinFourPlayersAndOpenHub();

            m_driver.Step("Hub: start a Tournament");
            yield return m_driver.Select(HubButton("m_tournamentButton"));
            yield return m_driver.Submit();

            // Tournament picks its levels itself; it ends once a player has won ScoreToReach games.
            var played = new List<string>();
            while (true)
            {
                if (played.Count >= k_MaxTournamentGames)
                    m_driver.Fail($"The tournament was still running after {played.Count} games: {string.Join(", ", played)}");

                yield return PlayOneGame(random, played);

                m_driver.Step($"Tournament results after game {played.Count}");
                yield return m_driver.WaitUntil("the tournament results screen",
                    () => IsInitialized(Manager(k_TournamentResultsContext)));
                yield return RealSeconds(0.5f);
                while (Manager(k_TournamentResultsContext) != null)
                {
                    yield return m_driver.Submit();
                    yield return RealSeconds(0.5f);
                }

                yield return m_driver.WaitUntil("the next game or the main menu",
                    () => IsOnMainMenuHome || GameplayState != null, 180f);
                if (IsOnMainMenuHome)
                    break;
            }

            Debug.Log($"[Scenario] Tournament over after {played.Count} games: {string.Join(", ", played)}");
            Assert.IsNull(Manager(k_TournamentContext), "The tournament context should be destroyed once back on the menu.");
            m_driver.AssertNoErrors();
        }

        #region Shared steps

        private IEnumerator GoToMainMenuHome()
        {
            m_driver.Step("Boot to the main menu");
            if (!IsOnMainMenuHome)
            {
                // A scenario that failed mid-game never reached EndState: release its level like EndState does,
                // otherwise its Addressables handles stay loaded and the next game on that level fails to load.
                if (Manager(k_GameplayContext) is { } gameplay)
                {
                    var loadingManager = Get(gameplay, "LoadingManager");
                    loadingManager.GetType().GetMethod("Unload").Invoke(loadingManager, new object[] { null });
                }
                // A leftover tournament would keep the game from returning to the menu.
                if (Manager(k_TournamentContext) is Component tournament)
                    UnityEngine.Object.Destroy(tournament.gameObject);
                SceneManager.LoadScene(0);
            }
            yield return m_driver.WaitUntil("the main menu home screen", () => IsOnMainMenuHome && CurrentSelection);
        }

        private IEnumerator JoinFourPlayersAndOpenHub()
        {
            m_driver.Step("Home: press Play");
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the character selection screen", () => MenuState == "CharacterSelectionState");

            m_driver.Step("Character selection: 4 players join");
            for (int i = 0; i < m_driver.Gamepads.Count; i++)
            {
                var expectedPlayers = i + 1;
                yield return m_driver.Press(m_driver.Gamepads[i].buttonSouth);
                yield return m_driver.WaitUntil($"player {expectedPlayers} to join with '{m_driver.Gamepads[i].name}'",
                    () => PlayerCount == expectedPlayers, 10f);
            }

            m_driver.Step("Character selection: start");
            var startButton = (Button)Get(FindActive("Game.MainMenu.CharacterSelection.CharacterSelectionScreen"), "m_startGameButton");
            yield return m_driver.WaitUntil("the start button to be selected", () => CurrentSelection == startButton.gameObject, 10f);
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the hub", () => MenuState == "HubState");
        }

        /// <summary>From the game scene loading to the end of its results screen.</summary>
        private IEnumerator PlayOneGame(System.Random random, List<string> played)
        {
            m_driver.Step("Game: loading");
            yield return m_driver.WaitUntil("the game to load",
                () => GameplayState is "TutorialState" or "GameplayState", 180f);
            var level = CurrentLevelName;
            played.Add(level);

            if (GameplayState == "TutorialState")
            {
                m_driver.Step($"{level}: everyone presses Ready");
                yield return Frames(5);
                foreach (var pad in m_driver.Gamepads)
                    yield return m_driver.Press(pad.buttonSouth);
                yield return m_driver.WaitUntil("the tutorial to end once everyone is ready", () => GameplayState != "TutorialState", 30f);
            }

            m_driver.Step($"{level}: playing");
            yield return m_driver.MashButtonsUntil(() => GameplayState is "ResultsState" or "EndState" or null, random, 300f);
            Debug.Log($"[Scenario] {level} finished. Scores: {Scores}");

            // The main player's Submit is only listened to once the close-ups are over.
            m_driver.Step($"{level}: results");
            while (GameplayState == "ResultsState")
            {
                yield return m_driver.Submit();
                yield return RealSeconds(0.5f);
            }
        }

        private static Selectable HubButton(string field) =>
            (Selectable)Get(FindActive("Game.MainMenu.Hub.HubScreen"), field);

        private static IEnumerable<Selectable> LevelWidgets(Component levelSelection) =>
            ((IEnumerable)Get(levelSelection, "LevelWidgets")).Cast<Selectable>();

        private static string LevelNameOf(Selectable widget) => ((UnityEngine.Object)Get(widget, "LevelDataAsset")).name;

        #endregion
    }
}
