using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Game.Tests.PlayMode.ScenarioDriver;
using Debug = UnityEngine.Debug;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// Online rooms whose machines leave, are cut off or come back (the cases of the online-multiplayer skill's
    /// references/disconnections.md). The editor hosts, a build of the game plays the guest with what goes wrong on its
    /// side (OnlineGuestBot's scenario). A cut is a frozen main thread: no heartbeat nor acknowledgement, as with a
    /// pulled cable or a crashed machine; shorter than the transport's timeout, it's a lag spike.
    /// </summary>
    public partial class GameModesScenarioTests
    {
        private const string k_ConnectionLost = "La connexion avec l'hôte a été perdue.";
        // The host's machine falls silent this long: beyond the transport's timeout (OnlineConnection), then the
        // margin it takes to notice.
        private const float k_CutSeconds = 25f;
        private const float k_NoticeMaxSeconds = 16f;

        private float? m_guestLoadingTimeoutBeforeTest;

        [UnityTest, Order(8), Timeout(5 * 60 * 1000)]
        public IEnumerator OnlineMenu_LeavesAndReopensRightAway()
        {
            LogAssert.ignoreFailingMessages = true;
            SetDirectAddress("127.0.0.1:7794");
            yield return JoinAloneAndOpenOnline();
            var onlineManager = Manager(k_OnlineManager);

            m_driver.Step("Online: open a room");
            yield return m_driver.Select(ScreenElement(k_OnlineScreen, "create-button"));
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the room", () => MenuState == "OnlineLobbyState", 15f);

            m_driver.Step("Online: leave the room and open another one at once");
            Call(onlineManager, "LeaveRoom");
            Call(onlineManager, "CreateRoom");
            yield return Frames(1);
            yield return m_driver.WaitUntil("the second room to open or fail", () => RoomStatus != "Connecting", 15f);
            Assert.AreEqual("InRoom", RoomStatus, "The room opened right after leaving another one failed.");
            // The first room's closing mustn't close the second one.
            yield return RealSeconds(1f);
            Assert.AreEqual("InRoom", RoomStatus, "The second room closed on its own.");
            Assert.IsTrue((bool)Get(Get(onlineManager, "m_connection"), "IsConnected"), "The second room isn't connected anymore.");

            m_driver.Step("Online: a launch that arrives after leaving the room launches nothing");
            Call(onlineManager, "LeaveRoom");
            var launch = OnlineMessageOf("LaunchTraining", writer =>
            {
                writer.Write(42);
                writer.Write((byte)0);
            });
            onlineManager.GetType().GetMethod("HandleMessageReceived", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(onlineManager, new object[] { 0UL, launch });
            yield return RealSeconds(2f);
            Assert.IsFalse((bool)Get(onlineManager, "IsInGame"), "A launch arrived after leaving the room started a game.");
            Assert.IsNull(Manager(k_GameplayContext), "A launch arrived after leaving the room loaded a game.");
            Assert.AreEqual("OnlineLobbyState", MenuState, "A launch arrived after leaving the room left the menu.");

            yield return LeaveRoom();
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(9), Timeout(15 * 60 * 1000)]
        public IEnumerator OnlineDrops_GuestLeavesTheRoomAndComesBack()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(17);
            // It leaves 6 s after joining, comes back 3 s later, then freezes 3 s in the game.
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7795", "rejoin,freeze-in-game:3");
            var firstGuestMachine = RoomMachineId(1);

            m_driver.Step("Online: the host picks a level while the guest leaves the room");
            yield return OpenTheRoomsLevelSelection();
            yield return m_driver.WaitUntil("the guest to leave", () => RoomPlayersCount == 1, 30f);
            yield return m_driver.WaitUntil("the host back in the room, where a game can't be launched anymore",
                () => MenuState == "OnlineLobbyState", 5f);
            Assert.IsFalse(LobbyButton("m_trainingButton").enabledSelf, "A room of one player can launch a game.");
            Assert.AreEqual(0, RemotePlayersCount, "The guest who left is still one of the room's players.");

            m_driver.Step("Online: the guest comes back with the code");
            yield return m_driver.WaitUntil("the guest back in the room", () => RoomPlayersCount == 2, 30f);
            Assert.AreNotEqual(firstGuestMachine, RoomMachineId(1), "The guest came back as the machine that left.");
            Assert.AreEqual(1, RemotePlayersCount, "The room keeps a player of the machine that left.");
            yield return m_driver.WaitUntil("the launch buttons", () => LobbyButton("m_trainingButton").enabledSelf, 5f);
            yield return RealSeconds(0.5f);

            yield return LaunchPointsRushForTheRoom();
            var played = new List<string>();
            var hostScores = "-";
            // A lag spike on each side: the host freezes 3 s as the game starts, the guest 3 s later.
            yield return PlayOneGame(random, played, () => hostScores = Scores, () => FreezeHost(3f));

            m_driver.Step("Online: back in the room with the guest");
            yield return m_driver.WaitUntil("the room after the game", () => MenuState == "OnlineLobbyState" && FocusedElement != null, 60f);
            Assert.AreEqual(2, RoomPlayersCount, "A lag spike of 3 s lost the guest.");
            Assert.That(ReadShared(GuestReport), Does.Contain("[GuestBot] unfrozen"), "The guest didn't freeze.");
            yield return CheckTheGuestSawTheGame(HostGameNumber, hostScores);
            yield return CloseTheRoomAndWaitForTheGuest(1);
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(10), Timeout(15 * 60 * 1000)]
        public IEnumerator OnlineDrops_GuestQuitsAGameWhileItsRobotIsDead()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(19);
            // It leaves the game once its robot is dead, then joins the room again until it's let in.
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7796", "quit-when-dead");
            yield return LaunchPointsRushForTheRoom();
            yield return HostGetsReadyAndPlays();

            m_driver.Step("Game: the guest's robot dies for 30 s, its player leaves meanwhile");
            yield return RealSeconds(1f);
            var guestRobot = Pawn(1);
            var death = Get(Get(guestRobot, "ReferencesHolder"), "DeathController");
            death.GetType().GetMethod("Kill").Invoke(death, new object[] { 30f, true });
            yield return m_driver.WaitUntil("the guest to leave", () => RoomPlayersCount == 1, 15f);
            yield return Frames(2);
            Assert.IsTrue((bool)Get(guestRobot, "HasLeft"), "The robot of the guest who left is still in the game.");
            Assert.IsFalse(((MonoBehaviour)death).IsInvoking("Resurrect"), "The robot of the guest who left will come back to life.");
            yield return RealSeconds(30f / k_TimeScale + 1f);
            Assert.IsFalse((bool)Get(death, "IsAlive"), "The robot of the guest who left came back to life.");
            Assert.IsFalse(IsAlive(Get(Get(Get(guestRobot, "ReferencesHolder"), "GridWalker"), "CurrentCell")),
                "The robot of the guest who left stands on the grid.");

            m_driver.Step("Game: the host plays on alone");
            yield return FinishTheGame(random);

            m_driver.Step("Online: refused during the game, the guest is let back in the room after it");
            Assert.That(ReadShared(GuestReport), Does.Contain("joining refused: Une partie est en cours"),
                $"The guest wasn't refused during the game:\n{GuestLogTail()}");
            yield return m_driver.WaitUntil("the guest back in the room", () => RoomPlayersCount == 2, 30f);
            yield return CloseTheRoomAndWaitForTheGuest(0);
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(11), Timeout(15 * 60 * 1000)]
        public IEnumerator OnlineDrops_GuestCutOffDuringAGame()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(23);
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7797", $"freeze-in-game:{k_CutSeconds},expect-room-lost");
            yield return LaunchPointsRushForTheRoom();
            yield return HostGetsReadyAndPlays();

            m_driver.Step($"Game: the guest falls silent {k_CutSeconds} s (3 s into the game), the host notices");
            var gameStart = Time.realtimeSinceStartup;
            yield return m_driver.WaitUntil("the host to notice the guest is gone", () => RoomPlayersCount == 1, 3f + k_CutSeconds);
            var noticed = Time.realtimeSinceStartup - gameStart - 3f;
            Debug.Log($"[Scenario] The host noticed the guest's cut after about {noticed:0.0} s.");
            Assert.Less(noticed, k_NoticeMaxSeconds, "The host took too long to notice the guest was cut off.");
            yield return Frames(2);
            Assert.IsTrue((bool)Get(Pawn(1), "HasLeft"), "The robot of the guest who was cut off is still in the game.");

            m_driver.Step("Game: the host plays on alone");
            yield return FinishTheGame(random);
            Assert.AreEqual(1, RoomPlayersCount, "The room has the guest who was cut off again.");

            m_driver.Step("Online: back from its freeze, the guest finds itself out of the room");
            yield return WaitForTheGuestToQuit(k_ConnectionLost, 90f);
            yield return LeaveRoom();
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(12), Timeout(15 * 60 * 1000)]
        public IEnumerator OnlineDrops_HostCutOffDuringAGame()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(29);
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7798", "expect-room-lost");
            yield return LaunchPointsRushForTheRoom();
            yield return HostGetsReadyAndPlays();
            yield return RealSeconds(2f);

            m_driver.Step($"Game: the host falls silent {k_CutSeconds} s");
            FreezeHost(k_CutSeconds);

            m_driver.Step("Online: meanwhile the guest went back to its menu, which says why");
            yield return WaitForTheGuestToQuit(k_ConnectionLost, 60f);

            m_driver.Step("Game: the host notices the guest is gone and plays on alone");
            yield return m_driver.WaitUntil("the host to notice the guest is gone", () => RoomPlayersCount == 1, k_NoticeMaxSeconds + 5f);
            yield return Frames(2);
            Assert.IsTrue((bool)Get(Pawn(1), "HasLeft"), "The robot of the guest who left is still in the game.");
            yield return FinishTheGame(random);
            yield return LeaveRoom();
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(13), Timeout(10 * 60 * 1000)]
        public IEnumerator OnlineDrops_HostLeavesDuringAGame()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7799", "expect-room-lost");
            yield return LaunchPointsRushForTheRoom();
            yield return HostGetsReadyAndPlays();
            yield return QuitTheGameFromThePause();
            Assert.IsFalse((bool)Get(Manager(k_OnlineManager), "IsInRoom"), "Leaving the game left the host in the room.");

            m_driver.Step("Online: the guest went back to its menu, which says why");
            yield return WaitForTheGuestToQuit("L'hôte a fermé la salle.", 60f);
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(14), Timeout(15 * 60 * 1000)]
        public IEnumerator OnlineDrops_GuestQuitsWhileLoading()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(31);
            // It leaves its game as it starts loading (as when the room is lost then), then joins the room again.
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7800", "quit-while-loading");
            yield return LaunchPointsRushForTheRoom();

            m_driver.Step("Game: the guest leaves as the game loads, the host's game starts without it");
            yield return m_driver.WaitUntil("the game to load", () => GameplayState is "TutorialState" or "GameplayState", 120f);
            Assert.AreEqual(1, RoomPlayersCount, "The guest who left is still in the room.");
            yield return m_driver.WaitUntil("the guest's robot out of the game", () => (bool)Get(Pawn(1), "HasLeft"), 5f);
            // Its player doesn't hold the tutorial up.
            yield return HostGetsReadyAndPlays();
            yield return FinishTheGame(random);

            m_driver.Step("Online: the guest comes back once the game is over");
            yield return m_driver.WaitUntil("the guest back in the room", () => RoomPlayersCount == 2, 30f);
            yield return m_driver.WaitUntil("the launch buttons", () => LobbyButton("m_trainingButton").enabledSelf, 5f);
            yield return RealSeconds(0.5f);

            // Nothing of the game it left while loading may get in the way of the next one, on either machine.
            m_driver.Step("Game 2: the room plays a whole game");
            yield return LaunchPointsRushForTheRoom();
            var hostScores = "-";
            yield return PlayOneGame(random, new List<string>(), () => hostScores = Scores);
            yield return m_driver.WaitUntil("the room after the game", () => MenuState == "OnlineLobbyState" && FocusedElement != null, 60f);
            Assert.AreEqual(2, RoomPlayersCount, "The guest isn't in the room anymore.");
            yield return CheckTheGuestSawTheGame(HostGameNumber, hostScores);
            yield return CloseTheRoomAndWaitForTheGuest(1);
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(15), Timeout(10 * 60 * 1000)]
        public IEnumerator OnlineDrops_GuestNeverLoads()
        {
            LogAssert.ignoreFailingMessages = true;
            const float loadingTimeout = 8f;
            SetGuestLoadingTimeout(loadingTimeout);
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7801", "stuck-loading,expect-room-lost");
            yield return LaunchPointsRushForTheRoom();

            m_driver.Step($"Game: the host waits {loadingTimeout} s for the guest to load, then sends it away");
            yield return m_driver.WaitUntil("the game to load without the guest", () => GameplayState is "TutorialState" or "GameplayState", 60f);
            Assert.AreEqual(1, RoomPlayersCount, "The guest that never loaded is still in the room.");
            yield return m_driver.WaitUntil("the guest's robot out of the game", () => (bool)Get(Pawn(1), "HasLeft"), 5f);
            yield return HostGetsReadyAndPlays();
            yield return QuitTheGameFromThePause();

            m_driver.Step("Online: the guest went back to its menu, which says why");
            yield return WaitForTheGuestToQuit("trop de temps à charger", 60f);
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(16), Timeout(20 * 60 * 1000)]
        public IEnumerator OnlineDrops_HostCutOffBetweenTournamentGames()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(37);
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7802", "expect-room-lost");
            m_driver.Step("Online: the host launches a tournament for the room");
            yield return m_driver.Select(LobbyButton("m_tournamentButton"));
            yield return m_driver.Submit();
            yield return ConfirmLaunch();
            yield return PlayOneGame(random, new List<string>());

            m_driver.Step("Tournament results: the host falls silent 15 s");
            yield return m_driver.WaitUntil("the tournament results screen", () => IsInitialized(Manager(k_TournamentResultsContext)), 60f);
            yield return RealSeconds(0.5f);
            FreezeHost(15f);

            m_driver.Step("Online: meanwhile the guest closed its tournament and went back to its menu");
            yield return WaitForTheGuestToQuit(k_ConnectionLost, 60f);
            Assert.That(ReadShared(GuestLog), Does.Contain("[Tournament] Room lost between two games"),
                $"The guest didn't close its tournament between two games:\n{GuestLogTail()}");

            m_driver.Step("Tournament: alone, the host's tournament ends with these results");
            yield return m_driver.WaitUntil("the host to notice the guest is gone", () => RoomPlayersCount == 1, k_NoticeMaxSeconds + 5f);
            while (Manager(k_TournamentResultsContext) != null)
            {
                yield return m_driver.Submit();
                yield return RealSeconds(0.5f);
            }
            yield return m_driver.WaitUntil("the room or another game", () => MenuState == "OnlineLobbyState" || GameplayState != null, 120f);
            Assert.AreEqual("OnlineLobbyState", MenuState, "Alone, the host went on with the tournament.");
            Assert.IsNull(Manager(k_TournamentContext), "The tournament context should be destroyed once back in the room.");
            yield return m_driver.WaitUntil("the room's focus", () => FocusedElement != null, 30f);
            yield return LeaveRoom();
            m_driver.AssertNoErrors();
        }

        // Loading, the host's player gets ready, the game is on.
        private IEnumerator HostGetsReadyAndPlays()
        {
            m_driver.Step("Game: loading, the host gets ready");
            yield return m_driver.WaitUntil("the game to load", () => GameplayState is "TutorialState" or "GameplayState", 120f);
            if (GameplayState == "TutorialState")
            {
                yield return Frames(5);
                yield return m_driver.Press(m_driver.MainPad.buttonSouth);
            }
            yield return m_driver.WaitUntil("the game", () => GameplayState == "GameplayState", 60f);
        }

        // To the end of the game's results, then the room.
        private IEnumerator FinishTheGame(System.Random random)
        {
            yield return m_driver.MashButtonsUntil(() => GameplayState is "ResultsState" or "EndState" or null, random, 300f);
            while (GameplayState == "ResultsState")
            {
                yield return m_driver.Submit();
                yield return RealSeconds(0.5f);
            }
            yield return m_driver.WaitUntil("the room after the game", () => MenuState == "OnlineLobbyState" && FocusedElement != null, 60f);
        }

        // Pause, Menu principal: out of the room, back to the character selection.
        private IEnumerator QuitTheGameFromThePause()
        {
            m_driver.Step("Pause: P opens it, Menu principal leaves the game and the room");
            m_driver.AddKeyboardAndMouse();
            yield return m_driver.Press(m_driver.Keyboard.pKey);
            yield return m_driver.WaitUntil("the pause", () => IsPaused, 5f);
            yield return m_driver.WaitUntil("the resume button to be focused", () => NameOf(FocusedElement) == "resume-button", 5f);
            yield return m_driver.Select(ScreenElement(k_PauseMenuPanel, "back-to-menu-button"));
            yield return m_driver.Submit();
            m_driver.RemoveKeyboardAndMouse();
            yield return m_driver.WaitUntil("the character selection", () => IsOnCharacterSelection, 120f);
        }

        // The guest, back in its menu with the room lost for that reason, quits on its own, successfully.
        private IEnumerator WaitForTheGuestToQuit(string roomLostReason, float timeout)
        {
            yield return m_driver.WaitUntil("the guest to quit", () => m_guest.HasExited, timeout);
            Assert.AreEqual(0, m_guest.ExitCode, $"The guest failed (code {m_guest.ExitCode}). Its log, {GuestLog}:\n{GuestLogTail()}");
            var lost = Regex.Match(ReadShared(GuestReport), @"\[GuestBot\] room lost: (.*)");
            Assert.IsTrue(lost.Success, $"The guest didn't lose the room:\n{GuestLogTail()}");
            Assert.That(lost.Groups[1].Value, Does.Contain(roomLostReason), "The guest's menu gives another reason.");
        }

        // The guest saw both robots move (its own moved on the host: its buttons got there), and the host's scores.
        private IEnumerator CheckTheGuestSawTheGame(int gameNumber, string hostScores)
        {
            var pattern = $@"\[GuestBot\] game {gameNumber}: results, moves (\d+), (\d+), scores (.*)";
            // It may still be showing the results the host left.
            yield return WaitForRealSeconds(() => Regex.IsMatch(ReadShared(GuestReport), pattern), 10f);
            var results = Regex.Match(ReadShared(GuestReport), pattern);
            Assert.IsTrue(results.Success, $"The guest didn't reach the results:\n{GuestLogTail()}");
            Debug.Log($"[Scenario] The guest saw {results.Groups[1].Value} moves of the host's robot, {results.Groups[2].Value} of its own, scores {results.Groups[3].Value.Trim()}.");
            Assert.Greater(int.Parse(results.Groups[1].Value), 0, "The guest didn't see the host's robot move.");
            Assert.Greater(int.Parse(results.Groups[2].Value), 0, "The guest's buttons didn't move its robot.");
            Assert.AreEqual(hostScores, results.Groups[3].Value.Trim(), "The guest's scores aren't the host's.");
        }

        // The main thread stops: the guests stop hearing from the host, as with a network cut.
        private static void FreezeHost(float seconds)
        {
            Debug.Log($"[Scenario] The host freezes {seconds:0.#} s.");
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
        }

        private static object Pawn(int slot) =>
            ((IEnumerable)Get(Manager(k_CharactersManager), "CharacterPawns")).Cast<object>().ElementAt(slot);

        private static string RoomStatus => Get(Manager(k_OnlineManager), "Status").ToString();

        private static int RemotePlayersCount => ((ICollection)Get(Manager(k_OnlineManager), "m_remotePlayers")).Count;

        private static ulong RoomMachineId(int slot) =>
            (ulong)Get(((IEnumerable)Get(Manager(k_OnlineManager), "Roster")).Cast<object>().ElementAt(slot), "MachineId");

        private static void Call(object target, string method) => target.GetType().GetMethod(method).Invoke(target, null);

        private static byte[] OnlineMessageOf(string type, Action<BinaryWriter> writeContent)
        {
            var messageType = GameType("Game.Online.EOnlineMessage");
            return (byte[])GameType("Game.Online.OnlineMessage").GetMethod("Write")
                .Invoke(null, new[] { Enum.Parse(messageType, type), writeContent });
        }

        private static PropertyInfo GuestLoadingTimeoutProperty =>
            GameType(k_OnlineManager).GetProperty("GuestLoadingTimeout", BindingFlags.Public | BindingFlags.Static);

        private void SetGuestLoadingTimeout(float seconds)
        {
            var property = GuestLoadingTimeoutProperty;
            if (property == null)
                Assert.Fail("The host has no loading timeout (OnlineManager.GuestLoadingTimeout): a guest that never loads blocks it.");
            m_guestLoadingTimeoutBeforeTest ??= (float)property.GetValue(null);
            property.SetValue(null, seconds);
        }

        /// <summary>Gives the host its loading timeout back after a test shortened it.</summary>
        private void RestoreGuestLoadingTimeout()
        {
            if (m_guestLoadingTimeoutBeforeTest is not { } timeout)
                return;
            GuestLoadingTimeoutProperty?.SetValue(null, timeout);
            m_guestLoadingTimeoutBeforeTest = null;
        }
    }
}
