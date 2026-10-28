using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Game.Tests.PlayMode.ScenarioDriver;

namespace Game.Tests.PlayMode
{
    /// <summary>The UI Toolkit screens of the games: tutorial, timer, announcements, pause, results, loading screen.</summary>
    public partial class GameModesScenarioTests
    {
        private const string k_ScreensTestMode = "Points Rush";
        private const string k_PauseMenuManager = "Game.Gameplay.PauseMenu.PauseMenuManager";
        private const string k_PauseMenuPanel = "Game.Gameplay.PauseMenu.PauseMenuPanel";
        private const string k_LoadingScreenPanel = "Game.LoadingScreen.LoadingScreenPanel";

        // Screenshot of each announcement, by the type of its flow state.
        private static readonly Dictionary<string, string> s_announcementScreenshots = new()
        {
            { "AnnouncementState", "25-new-controls" },
            { "RoundAnnouncementState", "26-new-round" },
            { "FinishState", "27-finish" },
        };

        /// <summary>
        /// A game's screens, driven like players would: the tutorial's cards, the timer and the announcements, the pause
        /// menu (opened with P, then the main player's pad: a volume, Cancel), the results' button, the loading screen.
        /// Saves a screenshot of each in Logs/screenshots (20 and over).
        /// </summary>
        [UnityTest, Order(1), Timeout(15 * 60 * 1000)]
        public IEnumerator GameScreens_WorkWithGamepadsAndKeyboard()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(20);

            yield return JoinFourPlayersAndOpenHub();
            yield return StartTraining(k_ScreensTestMode);

            m_driver.Step("Tutorial: the players get ready one by one");
            yield return m_driver.WaitUntil("the tutorial", () => GameplayState == "TutorialState", 180f);
            yield return RealSeconds(0.5f);
            yield return CaptureGameScreen("20-tutorial");
            yield return m_driver.Press(m_driver.Gamepads[0].buttonSouth);
            yield return m_driver.Press(m_driver.Gamepads[1].buttonSouth);
            yield return RealSeconds(0.6f);
            yield return CaptureScreen("21-tutorial-two-ready");
            Assert.AreEqual("TutorialState", GameplayState, "The tutorial ended before everybody was ready.");
            foreach (var pad in m_driver.Gamepads.Skip(2))
                yield return m_driver.Press(pad.buttonSouth);
            yield return m_driver.WaitUntil("the tutorial to end once everyone is ready", () => GameplayState != "TutorialState", 30f);

            m_driver.Step("Game: the timer");
            yield return m_driver.WaitUntil("the game", () => GameplayState == "GameplayState", 60f);
            yield return RealSeconds(1f);
            yield return CaptureScreen("22-game");

            m_driver.Step("Pause: P opens it, the main player's pad changes a volume, Cancel resumes");
            m_driver.AddKeyboardAndMouse();
            yield return m_driver.Press(m_driver.Keyboard.pKey);
            yield return m_driver.WaitUntil("the pause", () => IsPaused, 5f);
            yield return m_driver.WaitUntil("the resume button to be focused", () => NameOf(FocusedElement) == "resume-button", 5f);
            yield return RealSeconds(0.3f);
            yield return CaptureScreen("23-pause");
            var masterVolume = ScreenElement(k_PauseMenuPanel, "master-volume").Q<SliderInt>();
            yield return SelectSlider(masterVolume);
            var volume = masterVolume.value;
            var (towards, back) = volume <= 95 ? (MoveDirection.Right, MoveDirection.Left) : (MoveDirection.Left, MoveDirection.Right);
            yield return m_driver.Press(m_driver.DpadButton(towards));
            Assert.AreEqual(volume + (towards == MoveDirection.Right ? 5 : -5), masterVolume.value, "The d-pad didn't change the master volume of the pause.");
            yield return RealSeconds(0.3f);
            yield return CaptureScreen("24-pause-volume");
            yield return m_driver.Press(m_driver.DpadButton(back));
            Assert.AreEqual(volume, masterVolume.value, "The master volume didn't come back to its value.");
            yield return m_driver.Press(m_driver.MainPad.buttonEast);
            yield return m_driver.WaitUntil("the game to resume after Cancel", () => !IsPaused, 5f);
            m_driver.RemoveKeyboardAndMouse();
            // Resuming puts the time scale back to 1.
            Time.timeScale = k_TimeScale;

            m_driver.Step("Game: until the results, with a screenshot of each announcement");
            bool IsOver() => GameplayState is "ResultsState" or "EndState" or null;
            var captured = new HashSet<string>();
            bool IsNewAnnouncement() => GameplayState is { } state && s_announcementScreenshots.ContainsKey(state) && !captured.Contains(state);
            while (!IsOver())
            {
                yield return m_driver.MashButtonsUntil(() => IsOver() || IsNewAnnouncement(), random, 300f);
                if (!IsNewAnnouncement())
                    continue;
                var announcement = GameplayState;
                captured.Add(announcement);
                // An announcement lasts a second or two of game time: at the real pace while its banner unrolls.
                Time.timeScale = 1f;
                yield return RealSeconds(0.6f);
                yield return CaptureScreen(s_announcementScreenshots[announcement]);
                Time.timeScale = k_TimeScale;
            }
            Debug.Log($"[Scenario] Announcements seen: {string.Join(", ", captured)}. Scores: {Scores}");

            m_driver.Step("Results: the button shows after the close-ups, Submit leaves");
            yield return m_driver.WaitUntil("the results' button to be focused", () => NameOf(FocusedElement) == "back-to-menu-button", 60f);
            yield return RealSeconds(0.5f);
            yield return CaptureScreen("28-results");
            yield return m_driver.Submit();

            m_driver.Step("Loading screen, then the main menu");
            yield return m_driver.WaitUntil("the loading screen", () => FindActive(k_LoadingScreenPanel) != null, 30f);
            yield return CaptureScreen("29-loading-screen");
            yield return m_driver.WaitUntil("the main menu home screen", () => IsOnMainMenuHome, 120f);

            m_driver.AssertNoErrors();
        }

        private IEnumerator StartTraining(string mode)
        {
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
        }

        /// <summary>
        /// A screenshot of a scene other than the main menu: its camera and the screens' panel render into the
        /// screenshot textures from now on.
        /// </summary>
        private static IEnumerator CaptureGameScreen(string fileName)
        {
            StopRenderingToTextures();
            StartRenderingToTextures();
            yield return CaptureScreen(fileName);
        }

        private static bool IsPaused => (bool)Get(FindActive(k_PauseMenuManager), "IsPaused");

        private static bool IsFocused(VisualElement element) =>
            FocusedElement == element || element.Contains(FocusedElement as VisualElement);

        // Left and right change a slider's value instead of moving the focus: once on a slider, only up and down move.
        private IEnumerator SelectSlider(SliderInt slider)
        {
            for (int presses = 0; presses < 6 && !IsFocused(slider); presses++)
            {
                var from = (VisualElement)FocusedElement;
                var delta = slider.worldBound.center - from.worldBound.center; // y grows downward in panel space
                var onASlider = from.GetFirstAncestorOfType<SliderInt>() != null || from is SliderInt;
                var direction = !onASlider && delta.x > 0f ? MoveDirection.Right
                    : delta.y < 0f ? MoveDirection.Up : MoveDirection.Down;
                yield return m_driver.Press(m_driver.DpadButton(direction));
            }
            if (!IsFocused(slider))
                m_driver.Fail($"The d-pad never reached '{slider.parent.name}' (focused: '{NameOf(FocusedElement)}').");
        }
    }
}
