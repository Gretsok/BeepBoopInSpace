using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Game.Tests.PlayMode.ScenarioDriver;
using Debug = UnityEngine.Debug;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// Online rooms, on a direct connection (no Unity services): one player alone reaches the online modes and opens
    /// a room; then a training game against a guest played by a build of the game (OnlineGuestBot).
    /// </summary>
    public partial class GameModesScenarioTests
    {
        private const string k_OnlineManager = "Game.Online.OnlineManager";
        private const string k_OnlineScreen = "Game.MainMenu.Online.OnlineScreen";
        private const string k_OnlineLobbyScreen = "Game.MainMenu.OnlineLobby.OnlineLobbyScreen";
        // The build the guest plays: unity_cli.py build --output Builds/OnlineGuest.
        private const string k_GuestBuildFolder = "Builds/OnlineGuest";
        private const string k_GuestExecutable = "Beep Boop dans l'espace.exe";

        [UnityTest, Order(5), Timeout(5 * 60 * 1000)]
        public IEnumerator OnlineMenu_OnePlayerOpensARoom()
        {
            LogAssert.ignoreFailingMessages = true;
            const string address = "127.0.0.1:7791";
            SetDirectAddress(address);

            yield return JoinAloneAndOpenHub();

            m_driver.Step("Hub alone: the local modes are greyed, En ligne is focused");
            Assert.IsFalse(HubButton("m_tournamentButton").enabledSelf, "Tournoi is open to a player alone.");
            Assert.IsFalse(HubButton("m_trainingButton").enabledSelf, "Entraînement is open to a player alone.");
            var online = HubButton("m_onlineButton");
            yield return m_driver.WaitUntil("En ligne to be focused", () => FocusedElement == online, 5f);
            yield return CaptureScreen("16-hub-alone");

            m_driver.Step("Online: the d-pad and the keyboard type the code");
            m_driver.AddKeyboardAndMouse();
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the online screen", () => MenuState == "OnlineState");
            var firstSlot = (Button)ScreenElement(k_OnlineScreen, "code-slot-1");
            yield return m_driver.Select(firstSlot);
            yield return m_driver.Press(m_driver.DpadButton(MoveDirection.Down));
            Assert.AreEqual("A", firstSlot.text, "Down didn't give the first character.");
            yield return m_driver.Press(m_driver.DpadButton(MoveDirection.Up));
            Assert.AreEqual("9", firstSlot.text, "Up didn't go back round to the last character.");
            InputSystem.QueueTextEvent(m_driver.Keyboard, 'q');
            yield return Frames(3);
            Assert.AreEqual("Q", firstSlot.text, "A typed letter didn't fill the selected box.");
            var secondSlot = (Button)ScreenElement(k_OnlineScreen, "code-slot-2");
            Assert.AreEqual(secondSlot, FocusedElement, "Typing didn't move to the next box.");
            yield return RealSeconds(0.5f);
            yield return CaptureScreen("17-online");

            m_driver.Step("Online: create a room");
            // Out of a room, the game pauses when its window loses the focus, as the Player Settings say. The test runner
            // keeps it running: not for the time of the test (TearDown gives it back).
            m_runInBackgroundBeforeTest = Application.runInBackground;
            Application.runInBackground = false;
            yield return m_driver.Select(ScreenElement(k_OnlineScreen, "create-button"));
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the room", () => MenuState == "OnlineLobbyState", 15f);
            Assert.AreEqual(1, RoomPlayersCount, "The room doesn't have its host's player.");
            Assert.IsTrue(Application.runInBackground, "In a room, the game would stop with its window out of focus, and the room with it.");
            Assert.AreEqual(address, ((Label)ScreenElement(k_OnlineLobbyScreen, "code")).text, "The room shows another code.");
            Assert.IsFalse(LobbyButton("m_trainingButton").enabledSelf, "A room of one player can launch a game.");
            yield return RealSeconds(0.5f);
            yield return CaptureScreen("18-online-room-alone");

            yield return LeaveRoom();
            yield return m_driver.WaitUntil("the game to pause out of focus again after the room", () => !Application.runInBackground, 10f);
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(6), Timeout(15 * 60 * 1000)]
        public IEnumerator Online_PlaysATrainingWithAGuestBuild()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(11);
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7792");
            yield return CaptureScreen("19-online-room");

            {
                yield return LaunchPointsRushForTheRoom();

                var played = new List<string>();
                var hostScores = "-";
                yield return PlayOneGame(random, played, () => hostScores = Scores);
                Assert.That(played[0], Does.Contain(" - Points Rush - "), "The room played another level.");

                m_driver.Step("Online: back in the room with the guest");
                yield return m_driver.WaitUntil("the room after the game", () => MenuState == "OnlineLobbyState" && FocusedElement != null, 60f);
                Assert.AreEqual(2, RoomPlayersCount, "The guest isn't in the room anymore.");
                yield return CheckTheGuestSawTheGame(HostGameNumber, hostScores);

                yield return CloseTheRoomAndWaitForTheGuest(1);
            }
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(7), Timeout(30 * 60 * 1000)]
        public IEnumerator Online_PlaysATournamentWithAGuestBuild()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(43);
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7793");

            m_driver.Step("Online: the host launches a tournament for the room");
            yield return m_driver.Select(LobbyButton("m_tournamentButton"));
            yield return m_driver.Submit();
            yield return ConfirmLaunch();

            // The host draws the levels and leaves the results for both machines, until a champion.
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

                yield return m_driver.WaitUntil("the next game or the room",
                    () => MenuState == "OnlineLobbyState" || GameplayState != null, 180f);
                if (MenuState == "OnlineLobbyState")
                    break;
            }

            Debug.Log($"[Scenario] Online tournament over after {played.Count} games: {string.Join(", ", played)}");
            Assert.IsNull(Manager(k_TournamentContext), "The tournament context should be destroyed once back in the room.");
            yield return m_driver.WaitUntil("the room after the tournament", () => FocusedElement != null, 30f);
            Assert.AreEqual(2, RoomPlayersCount, "The guest isn't in the room anymore.");
            yield return CloseTheRoomAndWaitForTheGuest(played.Count);
            m_driver.AssertNoErrors();
        }

        private Process m_guest;
        private bool? m_runInBackgroundBeforeTest;
        private static string GuestLog => Path.GetFullPath("Logs/online-guest.log");
        // The guest bot's lines, written as they happen (its log reaches the disk by blocks).
        private static string GuestReport => Path.GetFullPath("Logs/online-guest-report.txt");

        // The hub alone, then a room on a direct connection, which a build of the game joins (OnlineGuestBot), with
        // what goes wrong on its side (OnlineGuestBot.ReadScenario).
        private IEnumerator OpenARoomWithAGuestBuild(string address, string guestScenario = null)
        {
            var guestExecutable = Path.GetFullPath(Path.Combine(k_GuestBuildFolder, k_GuestExecutable));
            if (!File.Exists(guestExecutable))
                Assert.Ignore($"No guest build at {guestExecutable}: build it first (unity_cli.py build --output {k_GuestBuildFolder}).");
            SetDirectAddress(address);

            yield return JoinAloneAndOpenHub();
            m_driver.Step("Online: create a room");
            yield return m_driver.Select(HubButton("m_onlineButton"));
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the online screen", () => MenuState == "OnlineState");
            yield return m_driver.Select(ScreenElement(k_OnlineScreen, "create-button"));
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the room", () => MenuState == "OnlineLobbyState", 15f);

            m_driver.Step("Online: a guest joins (a build of the game)");
            var scenario = string.IsNullOrEmpty(guestScenario) ? "" : $" -online-test-guest-scenario {guestScenario}";
            File.Delete(GuestReport);
            m_guest = Process.Start(new ProcessStartInfo(guestExecutable,
                $"-batchmode -nographics -no-analytics -online-direct {address} -online-test-guest{scenario} " +
                $"-online-test-guest-report \"{GuestReport}\" -logFile \"{GuestLog}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            Assert.IsNotNull(m_guest, "The guest build didn't start.");
            yield return m_driver.WaitUntil("the guest in the room", () => RoomPlayersCount == 2, 90f);
            yield return RealSeconds(0.5f);
        }

        // The host's training: Points Rush, from the room's Entraînement.
        private IEnumerator LaunchPointsRushForTheRoom()
        {
            m_driver.Step("Online: the host launches a training for the room");
            yield return OpenTheRoomsLevelSelection();
            yield return PickPointsRush();
        }

        private IEnumerator OpenTheRoomsLevelSelection()
        {
            yield return m_driver.Select(LobbyButton("m_trainingButton"));
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the level selection screen", () => MenuState == "LevelSelectionState");
        }

        private IEnumerator PickPointsRush()
        {
            var levelSelection = FindActive("Game.MainMenu.LevelSelection.LevelSelectionScreen");
            yield return m_driver.WaitUntil("the level widgets", () => LevelWidgets(levelSelection).Any());
            var widget = LevelWidgets(levelSelection).First(w => LevelNameOf(w).Contains(" - Points Rush - "));
            yield return m_driver.Select((VisualElement)Get(widget, "Element"));
            yield return m_driver.Submit();
            yield return ConfirmLaunch();
        }

        // The host leaves: the guest, back in its menu, quits on its own once it has played every game.
        private IEnumerator CloseTheRoomAndWaitForTheGuest(int gamesPlayed)
        {
            // It may still be showing the last results the host left.
            int GuestResults() => Regex.Matches(ReadShared(GuestReport), @"\[GuestBot\] game \d+: results").Count;
            yield return WaitForRealSeconds(() => GuestResults() >= gamesPlayed, 10f);
            var guestResults = GuestResults();
            Assert.AreEqual(gamesPlayed, guestResults, $"The guest saw the results of {guestResults} game(s) out of {gamesPlayed}:\n{GuestLogTail()}");
            yield return LeaveRoom();
            m_driver.Step("Online: the guest quits once the room is closed");
            yield return m_driver.WaitUntil("the guest to quit", () => m_guest.HasExited, 60f);
            Assert.AreEqual(0, m_guest.ExitCode, $"The guest failed (code {m_guest.ExitCode}). Its log, {GuestLog}:\n{GuestLogTail()}");
        }

        private void StopGuestBuild()
        {
            if (m_guest == null)
                return;
            if (!m_guest.HasExited)
                m_guest.Kill();
            m_guest.Dispose();
            m_guest = null;
            // The next test's guest writes over them: kept as Logs/online-guest-<test>.log and -report.txt.
            try
            {
                var testName = Regex.Replace(TestContext.CurrentContext.Test.Name, @"[^\w-]", "");
                var folder = Path.GetDirectoryName(GuestLog);
                File.Copy(GuestLog, Path.Combine(folder, $"online-guest-{testName}.log"), true);
                if (File.Exists(GuestReport))
                    File.Copy(GuestReport, Path.Combine(folder, $"online-guest-{testName}-report.txt"), true);
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[Scenario] The guest's log couldn't be kept: {e.Message}");
            }
        }

        private IEnumerator JoinAloneAndOpenHub()
        {
            StartRenderingToTextures();
            // A Submit right after the rendering starts can get lost.
            yield return RealSeconds(1f);
            m_driver.Step("Home: press Play");
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the character selection screen", () => MenuState == "CharacterSelectionState");
            m_driver.Step("Character selection: one player alone gets ready");
            yield return Join(0);
            yield return GetReady(0);
            yield return m_driver.WaitUntil("the hub", () => MenuState == "HubState", 10f);
        }

        // Retour, then the confirmation's Quitter: back on the hub, out of the room.
        private IEnumerator LeaveRoom()
        {
            m_driver.Step("Online: leave the room");
            // A press right as the room opens after a game can get lost.
            yield return RealSeconds(0.5f);
            yield return m_driver.Press(m_driver.MainPad.buttonEast);
            yield return WaitForConfirmation();
            yield return RealSeconds(0.5f);
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the hub after leaving the room", () => MenuState == "HubState", 10f);
            Assert.IsFalse((bool)Get(Manager(k_OnlineManager), "IsInRoom"), "Still in the room after leaving it.");
        }

        // Counts the games of every room the editor hosted: the guest logs the same number.
        private static int HostGameNumber => (int)Get(Manager(k_OnlineManager), "GameNumber");

        private static int RoomPlayersCount => ((ICollection)Get(Manager(k_OnlineManager), "Roster")).Count;

        private static VisualElement LobbyButton(string field) => (VisualElement)Get(FindActive(k_OnlineLobbyScreen), field);

        private static void SetDirectAddress(string address) =>
            GameType("Game.Online.OnlineConnection").GetProperty("DirectAddress", BindingFlags.Public | BindingFlags.Static)
                .SetValue(null, address);

        /// <summary>Leaves a room a failed scenario left open, and goes back to the Unity services.</summary>
        private static void LeaveOnlineRoom()
        {
            if (Manager(k_OnlineManager) is { } onlineManager && (bool)Get(onlineManager, "IsInRoom"))
                onlineManager.GetType().GetMethod("LeaveRoom").Invoke(onlineManager, null);
            SetDirectAddress(null);
        }

        /// <summary>Gives the test runner back its run in background, once a room left open has closed.</summary>
        private IEnumerator RestoreRunInBackground()
        {
            if (m_runInBackgroundBeforeTest is not { } runInBackground)
                yield break;
            var timeout = Time.realtimeSinceStartup + 10f;
            while (Manager(k_OnlineManager) is { } onlineManager && (bool)Get(onlineManager, "m_isKeepingRunningInBackground")
                                                                && Time.realtimeSinceStartup < timeout)
                yield return null;
            Application.runInBackground = runInBackground;
            m_runInBackgroundBeforeTest = null;
        }

        // The guest still writes its log.
        private static string ReadShared(string path)
        {
            if (!File.Exists(path))
                return string.Empty;
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static string GuestLogTail()
        {
            var lines = ReadShared(GuestLog).Split('\n');
            var interesting = lines.Where(line => line.Contains("[GuestBot]") || line.Contains("[Online]") || line.Contains("Exception")
                                                  || line.Contains("Error")).TakeLast(40);
            return $"Its report:\n{ReadShared(GuestReport)}\nIts log (as far as written):\n{string.Join("\n", interesting)}";
        }

        private static IEnumerator WaitForRealSeconds(System.Func<bool> condition, float timeout)
        {
            var end = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < end)
                yield return null;
        }
    }
}
