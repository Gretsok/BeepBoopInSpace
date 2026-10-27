using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
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
            StopRenderingToTextures();
            m_driver.RemoveKeyboardAndMouse();
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
            if (widget == null)
                m_driver.Fail($"No level for '{mode}'. Available: {string.Join(", ", LevelWidgets(levelSelection).Select(LevelNameOf))}");
            yield return m_driver.Select((VisualElement)Get(widget, "Element"));
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

        /// <summary>
        /// The menus with the keyboard and the mouse, then every main menu screen with the main player's gamepad: d-pad,
        /// Submit and Cancel, the volume sliders; the character selection with each pad: joining, getting ready, a
        /// character already taken, Cancel. Saves a screenshot of each screen in Logs/screenshots.
        /// </summary>
        [UnityTest, Order(0), Timeout(5 * 60 * 1000)]
        public IEnumerator MainMenu_EveryScreenWorksWithGamepadKeyboardAndMouse()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            const string home = "Game.MainMenu.HomeScreen";
            const string settings = "Game.MainMenu.SettingsScreen.MainMenuSettingsScreen";
            var cancel = m_driver.MainPad.buttonEast;

            // Once players have joined (an earlier scenario of the run), only the main player's devices drive the menus.
            if (PlayerCount == 0)
                yield return KeyboardAndMouseOnHome();
            else
                Debug.Log("[Scenario] Keyboard and mouse steps skipped: players have joined, the menus follow the main player's pad.");

            StartRenderingToTextures();

            m_driver.Step("Home");
            yield return WaitForCameraBlend();
            yield return CaptureScreen("01-home");

            m_driver.Step("Credits: open, then Cancel");
            yield return m_driver.Select(ScreenElement(home, "credits-button"));
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the credits", () => MenuState == "CreditsState");
            yield return CaptureScreen("02-credits");
            yield return m_driver.Press(cancel);
            yield return m_driver.WaitUntil("the home screen after Cancel", () => MenuState == "HomeState");

            m_driver.Step("Settings: change the master volume with the d-pad, then Cancel");
            yield return m_driver.WaitUntil("the home screen to focus its default button", () => FocusedElement != null);
            yield return m_driver.Select(ScreenElement(home, "settings-button"));
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the settings", () => MenuState == "MainMenuSettingsState");
            yield return WaitForCameraBlend();
            var masterVolume = ScreenElement(settings, "master-volume").Q<SliderInt>();
            yield return m_driver.WaitUntil("the master volume to be focused",
                () => FocusedElement == masterVolume || masterVolume.Contains(FocusedElement as VisualElement));
            var volume = masterVolume.value;
            var (towards, back) = volume <= 95 ? (MoveDirection.Right, MoveDirection.Left) : (MoveDirection.Left, MoveDirection.Right);
            yield return m_driver.Press(m_driver.DpadButton(towards));
            Assert.AreEqual(volume + (towards == MoveDirection.Right ? 5 : -5), masterVolume.value, "The d-pad didn't change the master volume.");
            yield return CaptureScreen("03-settings");
            yield return m_driver.Press(m_driver.DpadButton(back));
            Assert.AreEqual(volume, masterVolume.value, "The master volume didn't come back to its value.");
            yield return m_driver.Press(cancel);
            yield return m_driver.WaitUntil("the home screen after Cancel", () => MenuState == "HomeState");

            m_driver.Step("Character selection: join, get ready");
            yield return m_driver.WaitUntil("the play button to be focused", () => NameOf(FocusedElement) == "play-button");
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the character selection screen", () => MenuState == "CharacterSelectionState");
            yield return WaitForCameraBlend();
            yield return CaptureScreen("04-character-selection");
            var pads = m_driver.Gamepads;
            yield return Join(0);
            yield return CaptureScreen("05-character-selection-one-player");
            yield return m_driver.Press(pads[0].buttonNorth);
            yield return m_driver.WaitUntil("player 1 to be ready", () => IsReady(0), 5f);
            yield return RealSeconds(1f);
            yield return CaptureScreen("06-character-selection-ready");
            yield return Join(1);
            yield return CaptureScreen("07-character-selection-two-players");

            // With an XInput pad: the d-pad of the DualShock 4 HID can't be pressed button by button.
            m_driver.Step("Character selection: a character already chosen can't be taken");
            yield return Join(2);
            for (int i = 0; i < 10 && CharacterOf(2) != CharacterOf(0); i++)
                yield return m_driver.Press(pads[2].dpad.right);
            Assert.AreEqual(CharacterOf(0), CharacterOf(2), "Player 3's cursor never reached player 1's character.");
            yield return m_driver.Press(pads[2].buttonNorth);
            yield return RealSeconds(0.15f);
            Assert.IsFalse(IsReady(2), "Player 3 got ready with player 1's character.");
            yield return CaptureScreen("08-character-taken");
            yield return GetReady(2);
            yield return Join(3);
            yield return RealSeconds(1f);
            yield return CaptureScreen("09-character-selection-four-players");

            m_driver.Step("Character selection: Cancel makes the main player not ready, then everybody gets ready");
            yield return m_driver.Press(pads[0].buttonEast);
            yield return m_driver.WaitUntil("player 1 to cancel being ready", () => !IsReady(0), 5f);
            yield return RealSeconds(0.5f);
            Assert.AreEqual("CharacterSelectionState", MenuState, "Cancel left the character selection while player 1 was ready.");
            for (int i = 0; i < pads.Count; i++)
                yield return GetReady(i);
            yield return RealSeconds(0.5f);
            yield return CaptureScreen("10-character-selection-all-ready");
            yield return m_driver.WaitUntil("the hub once everybody is ready", () => MenuState == "HubState", 10f);
            yield return WaitForCameraBlend();
            yield return CaptureScreen("11-hub");

            m_driver.Step("Level selection: scroll to the last level, then Cancel");
            yield return m_driver.Select(HubButton("m_trainingButton"));
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the level selection screen", () => MenuState == "LevelSelectionState");
            var levelSelection = FindActive("Game.MainMenu.LevelSelection.LevelSelectionScreen");
            yield return m_driver.WaitUntil("the level widgets", () => LevelWidgets(levelSelection).Any());
            yield return CaptureScreen("12-level-selection");
            yield return m_driver.Select((VisualElement)Get(LevelWidgets(levelSelection).Last(), "Element"));
            yield return RealSeconds(0.5f);
            yield return CaptureScreen("13-level-selection-last");
            yield return m_driver.Press(cancel);
            yield return m_driver.WaitUntil("the hub after Cancel", () => MenuState == "HubState");

            m_driver.AssertNoErrors();
        }

        private IEnumerator KeyboardAndMouseOnHome()
        {
            const string home = "Game.MainMenu.HomeScreen";
            m_driver.AddKeyboardAndMouse();
            var keyboard = m_driver.Keyboard;

            m_driver.Step("Home: the arrow keys move the focus");
            yield return m_driver.Press(keyboard.downArrowKey);
            yield return m_driver.WaitUntil("the credits button to be focused", () => NameOf(FocusedElement) == "credits-button", 5f);
            yield return m_driver.Press(keyboard.upArrowKey);
            yield return m_driver.WaitUntil("the play button to be focused", () => NameOf(FocusedElement) == "play-button", 5f);

            m_driver.Step("Home: the mouse focuses what it hovers, clicks open the credits, Escape goes back");
            yield return m_driver.PointAt(ScreenElement(home, "quit-button"));
            yield return m_driver.WaitUntil("the hovered quit button to be focused", () => NameOf(FocusedElement) == "quit-button", 5f);
            yield return m_driver.PointAt(ScreenElement(home, "credits-button"));
            yield return m_driver.WaitUntil("the hovered credits button to be focused", () => NameOf(FocusedElement) == "credits-button", 5f);
            yield return m_driver.Click();
            yield return m_driver.WaitUntil("the credits after a click", () => MenuState == "CreditsState", 5f);
            // Away from the buttons of the home screen, which would get the focus back under the mouse.
            var creditsBack = ScreenElement("Game.MainMenu.Credits.CreditsScreen", "back-button");
            yield return m_driver.PointAt(creditsBack.panel, new Vector2(960f, 100f));
            yield return m_driver.Press(keyboard.escapeKey);
            yield return m_driver.WaitUntil("the home screen after Escape", () => MenuState == "HomeState");

            m_driver.RemoveKeyboardAndMouse();
            yield return m_driver.WaitUntil("the home screen to focus its default button", () => NameOf(FocusedElement) == "play-button");
        }

        #region Shared steps

        // Each menu screen moves the camera to its zone of the scene: screenshots wait for it to get there.
        private IEnumerator WaitForCameraBlend()
        {
            yield return Frames(2);
            var brain = Camera.main.GetComponent("CinemachineBrain");
            yield return m_driver.WaitUntil("the camera to reach its zone", () => !(bool)Get(brain, "IsBlending"), 10f);
        }

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
            yield return m_driver.WaitUntil("the main menu home screen", () => IsOnMainMenuHome && FocusedElement != null);
        }

        private IEnumerator JoinFourPlayersAndOpenHub()
        {
            m_driver.Step("Home: press Play");
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the character selection screen", () => MenuState == "CharacterSelectionState");

            m_driver.Step("Character selection: 4 players join and get ready");
            for (int i = 0; i < m_driver.Gamepads.Count; i++)
                yield return Join(i);
            for (int i = 0; i < m_driver.Gamepads.Count; i++)
                yield return GetReady(i);
            yield return m_driver.WaitUntil("the hub once everybody is ready", () => MenuState == "HubState", 10f);
        }

        private IEnumerator Join(int pad)
        {
            var expectedPlayers = PlayerCount + 1;
            yield return m_driver.Press(m_driver.Gamepads[pad].buttonSouth);
            yield return m_driver.WaitUntil($"player {expectedPlayers} to join with '{m_driver.Gamepads[pad].name}'",
                () => PlayerCount == expectedPlayers, 10f);
            // The circle pops in.
            yield return RealSeconds(0.5f);
        }

        // Ready (Y), with Random (X) first while another player has the character.
        private IEnumerator GetReady(int pad)
        {
            for (int attempt = 0; attempt < 8 && !IsReady(pad); attempt++)
            {
                if (attempt > 0)
                    yield return m_driver.Press(m_driver.Gamepads[pad].buttonWest);
                yield return m_driver.Press(m_driver.Gamepads[pad].buttonNorth);
                yield return Frames(2);
            }
            if (!IsReady(pad))
                m_driver.Fail($"Player {pad + 1} never got ready.");
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

        private static IList CharacterWidgets =>
            (IList)Get(FindActive("Game.MainMenu.CharacterSelection.CharacterSelectionScreen"), "m_widgets");

        private static bool IsReady(int slot) => (bool)Get(CharacterWidgets[slot], "IsReady");

        private static object CharacterOf(int slot) => Get(CharacterWidgets[slot], "CharacterDataAsset");

        private static VisualElement HubButton(string field) =>
            (VisualElement)Get(FindActive("Game.MainMenu.Hub.HubScreen"), field);

        private static IEnumerable<object> LevelWidgets(Component levelSelection) =>
            ((IEnumerable)Get(levelSelection, "LevelWidgets")).Cast<object>();

        private static string LevelNameOf(object widget) => ((UnityEngine.Object)Get(widget, "LevelDataAsset")).name;

        #endregion
    }
}
