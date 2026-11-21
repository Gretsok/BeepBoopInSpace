using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    public partial class GameModesScenarioTests
    {
        // Speeds up the 120s game timer and the results close-ups. Set to 1 to watch a scenario in real time.
        private const float k_TimeScale = 4f;
        private const int k_MaxTournamentGames = 12;
        private const string k_MusicsManager = "Game.Gameplay.MusicsManagement.MusicsManager";
        private const string k_LocalOrOnlineScreen = "Game.MainMenu.LocalOrOnline.LocalOrOnlineScreen";
        private const string k_CharacterBankManager = "Game.MainMenu.CharacterManagement.CharacterBankManager";

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
            SetTestsLanguage();
            yield return GoToMainMenuHome();
            SetTestsLanguage();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            StopRenderingToTextures();
            RestoreControlMode();
            LeaveOnlineRoom();
            RestoreGuestLoadingTimeout();
            yield return RestoreRunInBackground();
            StopGuestBuild();
            RemoveOtherGamepad();
            m_driver.PlugGamepadsBackIn();
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
            var characters = PlayerCharacters;
            yield return StartTraining(mode);

            var played = new List<string>();
            yield return PlayOneGame(random, played, roundScreenshot: mode == "Last Man Standing" ? "26-new-round" : null);
            Assert.That(played[0], Does.Contain($" - {mode} - "), "The game loaded another level than the one selected.");

            yield return BackToCharacterSelectionWith(characters);

            m_driver.AssertNoErrors();
        }

        /// <summary>
        /// Back from a game, each player has their circle and number and picks another robot with the d-pad: the robot
        /// nobody had goes to player 1, player 1's to player 2, and so on. The next game gives each player that robot,
        /// and they come back with it.
        /// </summary>
        [UnityTest, Order(1), Timeout(15 * 60 * 1000)]
        public IEnumerator CharacterSelection_PlayersPickOtherRobotsForTheNextGame()
        {
            LogAssert.ignoreFailingMessages = true;
            const string mode = "Points Rush";
            var random = new System.Random(5);
            var played = new List<string>();

            yield return JoinFourPlayersAndOpenHub();
            var characters = PlayerCharacters;
            yield return StartTraining(mode);
            yield return PlayOneGame(random, played);
            yield return BackToCharacterSelectionWith(characters);
            yield return CaptureGameScreen("60-character-selection-after-a-game");

            m_driver.Step("Character selection: each player takes the robot the previous one leaves");
            var roster = ((IEnumerable)Get(Manager(k_CharacterBankManager), "Characters")).Cast<UnityEngine.Object>().ToList();
            var picks = new List<UnityEngine.Object> { roster.First(character => !characters.Contains(character)) };
            picks.AddRange(characters.Take(characters.Count - 1));
            for (int i = 0; i < picks.Count; i++)
            {
                for (int push = 0; push < roster.Count && !ReferenceEquals(CharacterOf(i), picks[i]); push++)
                    yield return PushStickRight(m_driver.Gamepads[i]);
                Assert.AreEqual(picks[i], CharacterOf(i), $"Player {i + 1}'s stick never reached {picks[i].name}.");
            }
            yield return CaptureGameScreen("61-character-selection-other-robots");
            StopRenderingToTextures();
            for (int i = 0; i < picks.Count; i++)
            {
                var slot = i;
                yield return m_driver.Press(m_driver.Gamepads[slot].buttonNorth);
                yield return m_driver.WaitUntil($"player {slot + 1} to be ready with {picks[slot].name}", () => IsReady(slot), 5f);
            }
            yield return m_driver.WaitUntil("the hub once everybody is ready", () => MenuState == "HubState", 10f);

            yield return StartTraining(mode);
            yield return PlayOneGame(random, played, onPlaying: () =>
                CollectionAssert.AreEquivalent(picks.Select(character => character.name),
                    Robots().Select(robot => ((UnityEngine.Object)Get(robot, "CharacterDataAsset")).name),
                    "The second game didn't give the players the robots they picked."));
            yield return BackToCharacterSelectionWith(picks);

            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(2), Timeout(40 * 60 * 1000)]
        public IEnumerator Tournament_PlaysUntilAChampionThenReturnsToMenu()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(42);

            yield return JoinFourPlayersAndOpenHub();
            var characters = PlayerCharacters;
            yield return StartTournament();

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
                if (played.Count == 1)
                    yield return CaptureGameScreen("30-tournament-results");
                while (Manager(k_TournamentResultsContext) != null)
                {
                    yield return m_driver.Submit();
                    yield return RealSeconds(0.5f);
                }

                yield return m_driver.WaitUntil("the next game or the main menu",
                    () => IsOnCharacterSelection || IsOnMainMenuHome || GameplayState != null, 180f);
                if (IsOnCharacterSelection || IsOnMainMenuHome)
                    break;
            }

            Debug.Log($"[Scenario] Tournament over after {played.Count} games: {string.Join(", ", played)}");
            Assert.IsNull(Manager(k_TournamentContext), "The tournament context should be destroyed once back on the menu.");
            yield return BackToCharacterSelectionWith(characters);
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
            const string home = "Game.MainMenu.Home.HomeScreen";
            const string settings = "Game.MainMenu.SettingsScreen.MainMenuSettingsScreen";
            var cancel = m_driver.MainPad.buttonEast;

            // The home screen ends the players' session: nobody has joined, so every device drives the menus.
            Assert.AreEqual(0, PlayerCount, "Players are still there on the home screen.");
            yield return KeyboardAndMouseOnHome();

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

            m_driver.Step("Local or online: Play opens the choice, Cancel goes back home");
            yield return m_driver.WaitUntil("the play button to be focused", () => NameOf(FocusedElement) == "play-button");
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the choice between local and online", () => IsOnLocalOrOnline && FocusedElement != null);
            Assert.AreEqual("local-button", NameOf(FocusedElement), "The choice doesn't start on En local.");
            yield return m_driver.Press(m_driver.DpadButton(MoveDirection.Right));
            yield return m_driver.WaitUntil("En ligne to be focused", () => NameOf(FocusedElement) == "online-button", 5f);
            yield return WaitForCameraBlend();
            // The selected tile's pop.
            yield return RealSeconds(0.5f);
            yield return CaptureScreen("03b-local-or-online");
            yield return m_driver.Press(cancel);
            yield return m_driver.WaitUntil("the home screen after Cancel", () => MenuState == "HomeState");

            m_driver.Step("Character selection: En local, join, get ready");
            yield return m_driver.WaitUntil("the play button to be focused", () => NameOf(FocusedElement) == "play-button");
            yield return ChooseFromHome(online: false);
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

            m_driver.Step("Hub: the d-pad stays in the row of the modes, Tournoi asks for a confirmation, Cancel closes it");
            var tournament = HubButton("m_tournamentButton");
            var training = HubButton("m_trainingButton");
            var hub = FindActive("Game.MainMenu.Hub.HubScreen").GetComponent<UIDocument>().rootVisualElement;
            Assert.IsNull(hub.Q("online-button"), "En ligne is still a mode of the hub, after choosing En local.");
            yield return m_driver.Select(training);
            yield return m_driver.Press(m_driver.DpadButton(MoveDirection.Right));
            yield return RealSeconds(0.3f);
            Assert.AreEqual(training, FocusedElement, "Right from Entraînement left the row of the modes.");
            yield return m_driver.Select(tournament);
            yield return m_driver.Press(m_driver.DpadButton(MoveDirection.Left));
            yield return RealSeconds(0.3f);
            Assert.AreEqual(tournament, FocusedElement, "Left from Tournoi left the row of the modes.");
            yield return m_driver.Submit();
            yield return WaitForConfirmation();
            yield return RealSeconds(0.5f);
            Assert.AreEqual("HubState", MenuState, "Tournoi left the hub without a confirmation.");
            yield return CaptureScreen("14-tournament-confirmation");
            yield return m_driver.Press(cancel);
            yield return m_driver.WaitUntil("the focus back on Tournoi", () => FocusedElement == tournament, 5f);

            m_driver.Step("Level selection: scroll to the last level");
            yield return m_driver.Select(training);
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the level selection screen", () => MenuState == "LevelSelectionState");
            var levelSelection = FindActive("Game.MainMenu.LevelSelection.LevelSelectionScreen");
            yield return m_driver.WaitUntil("the level widgets", () => LevelWidgets(levelSelection).Any());
            yield return CaptureScreen("12-level-selection");
            var lastLevel = (VisualElement)Get(LevelWidgets(levelSelection).Last(), "Element");
            yield return m_driver.Select(lastLevel);
            yield return RealSeconds(0.5f);
            yield return CaptureScreen("13-level-selection-last");

            m_driver.Step("Level selection: a double press only opens the confirmation, Cancel closes it, then goes back");
            yield return DoubleSubmit();
            yield return WaitForConfirmation();
            yield return RealSeconds(0.5f);
            Assert.AreEqual("LevelSelectionState", MenuState, "A double press launched the level.");
            yield return CaptureScreen("15-level-confirmation");
            yield return m_driver.Press(cancel);
            yield return m_driver.WaitUntil("the focus back on the level", () => FocusedElement == lastLevel, 5f);
            yield return m_driver.Press(cancel);
            yield return m_driver.WaitUntil("the hub after Cancel", () => MenuState == "HubState");

            m_driver.AssertNoErrors();
        }

        private IEnumerator KeyboardAndMouseOnHome()
        {
            const string home = "Game.MainMenu.Home.HomeScreen";
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
            // An online room lost in an earlier scenario would open the menu on the online screen, which tells why.
            if (Manager("Game.Online.OnlineManager") is { } onlineManager)
                onlineManager.GetType().GetMethod("TakeLostRoomReason").Invoke(onlineManager, null);
            var hadStrandedPlayers = RemovePlayersStrandedByAFailure();
            if (hadStrandedPlayers || (!IsOnMainMenuHome && !IsOnCharacterSelection && !IsOnLocalOrOnline))
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
            // With players left from an earlier scenario, the menu opens on the character selection: the main player's
            // Cancel goes back to the choice between local and online, after cancelling being ready, then home.
            yield return m_driver.WaitUntil("the main menu", () => (IsOnMainMenuHome || IsOnCharacterSelection || IsOnLocalOrOnline) && FocusedElement != null);
            for (int presses = 0; (IsOnCharacterSelection || IsOnLocalOrOnline) && presses < 4; presses++)
            {
                yield return m_driver.Press(m_driver.MainPad.buttonEast);
                yield return RealSeconds(0.3f);
            }
            yield return m_driver.WaitUntil("the main menu home screen", () => IsOnMainMenuHome && FocusedElement != null);
        }

        // A scenario can leave a player on a device it removed since (another gamepad, the keyboard: Controls_AnotherGamepad...
        // does it on purpose), or a failed one a main player other than the main pad: nothing could send them home, and every
        // following scenario would time out.
        private bool RemovePlayersStrandedByAFailure()
        {
            var players = PlayerInput.all;
            var isStranded = players.Count > 0
                && (!players[0].devices.Contains(m_driver.MainPad) || players.Any(player => player.devices.Count == 0 || player.hasMissingRequiredDevices));
            if (!isStranded)
                return false;

            Debug.LogWarning($"[Scenario] {players.Count} player(s) left by the previous scenario on a removed device, or by a failed one, can't be sent home: removing them.");
            var playerManager = Get(Manager("Game.Global.GlobalContext"), "PlayerManager");
            playerManager.GetType().GetMethod("RemoveAllPlayers").Invoke(playerManager, null);
            return true;
        }

        /// <summary>After a game, the character selection with every player and their character, nobody ready yet.</summary>
        private IEnumerator BackToCharacterSelectionWith(List<UnityEngine.Object> characters)
        {
            m_driver.Step("Back to the character selection with the same players");
            yield return m_driver.WaitUntil("the character selection", () => IsOnCharacterSelection && FocusedElement != null);
            Assert.AreEqual(characters.Count, PlayerCount, "Players were lost on the way back to the menu.");
            for (int i = 0; i < characters.Count; i++)
            {
                Assert.AreEqual(characters[i], CharacterOf(i), $"Player {i + 1} came back with another character.");
                Assert.IsFalse(IsReady(i), $"Player {i + 1} came back ready.");
            }

            // Each player's circle, with their number, once it has popped in.
            yield return RealSeconds(0.5f);
            var playerBadge = GameType("Game.Global.Localization.Loc").GetMethod("PlayerBadge");
            for (int i = 0; i < characters.Count; i++)
            {
                var view = Get(CharacterWidgets[i], "m_view") as VisualElement;
                var slot = Get(CharacterWidgets[i], "m_slot") as VisualElement;
                Assert.IsNotNull(view, $"Player {i + 1}'s circle has no element on the screen.");
                Assert.AreEqual(DisplayStyle.Flex, view.resolvedStyle.display, $"Player {i + 1}'s circle is hidden.");
                Assert.Greater(slot.resolvedStyle.opacity, 0.9f, $"Player {i + 1}'s circle is transparent.");
                Assert.Greater(slot.worldBound.width, 0f, $"Player {i + 1}'s circle has no size.");
                Assert.AreEqual(playerBadge.Invoke(null, new object[] { i + 1 }), ((Label)Get(CharacterWidgets[i], "m_badgeLabel")).text,
                    $"Player {i + 1}'s circle doesn't show their number.");
            }
        }

        // The left stick, like the d-pad: a DualShock 4's d-pad is a hat switch, whose directions can't be written one by one.
        private static IEnumerator PushStickRight(Gamepad pad)
        {
            SetLeftStick(pad, Vector2.right);
            yield return Frames(2);
            SetLeftStick(pad, Vector2.zero);
            yield return Frames(2);
        }

        private static void SetLeftStick(Gamepad pad, Vector2 value)
        {
            using (UnityEngine.InputSystem.LowLevel.StateEvent.From(pad, out var eventPtr))
            {
                pad.leftStick.WriteValueIntoEvent(value, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }

        private IEnumerator StartTournament()
        {
            m_driver.Step("Hub: start a Tournament");
            yield return m_driver.Select(HubButton("m_tournamentButton"));
            yield return m_driver.Submit();
            yield return ConfirmLaunch();
        }

        // A level or the tournament opens a confirmation, whose confirm button ignores presses for a moment.
        private IEnumerator ConfirmLaunch()
        {
            yield return WaitForConfirmation();
            yield return RealSeconds(0.5f);
            yield return m_driver.Submit();
        }

        private IEnumerator WaitForConfirmation()
        {
            yield return m_driver.WaitUntil("the launch confirmation", () => NameOf(FocusedElement) == "confirm-button", 5f);
        }

        // A then A again right away, like a player pressing too fast: one press per frame, the main pad's only event.
        private IEnumerator DoubleSubmit()
        {
            var button = m_driver.MainPad.buttonSouth;
            var start = Time.realtimeSinceStartup;
            foreach (var value in new[] { 1f, 0f, 1f, 0f })
            {
                Set(button, value);
                yield return null;
            }
            var duration = Time.realtimeSinceStartup - start;
            if (duration > 0.3f)
                Debug.LogWarning($"[Scenario] The double press took {duration:0.00}s: slower than a player's, it proves little.");
        }

        /// <summary>Home: Play, then En local or En ligne, which opens the character selection.</summary>
        private IEnumerator ChooseFromHome(bool online)
        {
            m_driver.Step($"Home: press Play, then {(online ? "En ligne" : "En local")}");
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the choice between local and online", () => IsOnLocalOrOnline && FocusedElement != null);
            yield return m_driver.Select(ScreenElement(k_LocalOrOnlineScreen, online ? "online-button" : "local-button"));
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the character selection screen", () => MenuState == "CharacterSelectionState");
        }

        private IEnumerator JoinFourPlayersAndOpenHub()
        {
            yield return ChooseFromHome(online: false);

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
        /// <param name="onResults">Called as the results show, before the close-ups.</param>
        /// <param name="onPlaying">Called once the game is on, before the buttons are pressed.</param>
        /// <param name="roundScreenshot">Screenshot of the first new round's banner where a robot has won a round (its score board), if any.</param>
        private IEnumerator PlayOneGame(System.Random random, List<string> played, Action onResults = null, Action onPlaying = null,
            string roundScreenshot = null)
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
            onPlaying?.Invoke();
            if (roundScreenshot != null)
            {
                bool IsRoundWon() => GameplayState == "RoundAnnouncementState" && Scores.Split(',').Any(score => score.Trim() != "0");
                yield return m_driver.MashButtonsUntil(() => IsRoundWon() || GameplayState is "ResultsState" or "EndState" or null, random, 300f);
                if (IsRoundWon())
                {
                    // At the real pace while the banner unrolls and the round won pops on the score board.
                    Time.timeScale = 1f;
                    yield return RealSeconds(0.9f);
                    yield return CaptureGameScreen(roundScreenshot);
                    Time.timeScale = k_TimeScale;
                }
            }
            yield return m_driver.MashButtonsUntil(() => GameplayState is "ResultsState" or "EndState" or null, random, 300f);
            Debug.Log($"[Scenario] {level} finished. Scores: {Scores}");
            // The game's music ends with it: the next clip mustn't start over its end and its results.
            if (GameplayState == "ResultsState" && Manager(k_MusicsManager) is { } musics)
                Assert.IsFalse((bool)Get(musics, "IsPlayingGameplayMusics"), $"{level}: the gameplay music goes on over the results.");
            onResults?.Invoke();

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
