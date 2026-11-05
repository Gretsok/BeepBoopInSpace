using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Game.Tests.PlayMode.ScenarioDriver;
using Debug = UnityEngine.Debug;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// Online rooms lost or lagging during a transition: a game launching, ending, unloading, the tournament between
    /// two games, the pause open as the game ends (the cases of the online-multiplayer skill's references/transitions.md).
    /// The editor hosts, a build of the game plays the guest (OnlineGuestBot), which fails when one of its games comes
    /// back to life while it unloads, or when its menu doesn't open on the hub saying why after losing the room.
    /// </summary>
    public partial class GameModesScenarioTests
    {
        // A lag spike: the guest's machine falls silent this long, under the transport's timeout (10 s).
        private const float k_LagSeconds = 8f;
        private const string k_HostClosedTheRoom = "L'hôte a fermé la salle.";
        // What a host's room says when its transport fails (OnlineConnection).
        private const string k_HostConnectionCut = "La connexion a été coupée.";

        [UnityTest, Order(17), Timeout(15 * 60 * 1000)]
        public IEnumerator OnlineTransitions_GuestLagsAsTheHostRelaunches()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(41);
            SetGuestLoadingTimeout(30f);
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7803", $"freeze-at-results:{k_LagSeconds}");
            yield return LaunchPointsRushForTheRoom();
            yield return HostGetsReadyAndPlays();
            yield return m_driver.MashButtonsUntil(() => GameplayState is "ResultsState" or "EndState" or null, random, 300f);

            m_driver.Step("Results: the guest lags, meanwhile the host leaves them and launches the next game");
            yield return WaitForTheGuestToFreeze();
            yield return LeaveTheResults();
            yield return m_driver.WaitUntil("the room after the game", () => MenuState == "OnlineLobbyState" && FocusedElement != null, 60f);
            yield return LaunchPointsRushForTheRoom();
            AssertTheGuestIsStillFrozen("the host launched the next game");

            m_driver.Step("Game 2: the guest catches up, the next game starts with it");
            var hostScores = "-";
            yield return PlayOneGame(random, new List<string>(), () => hostScores = Scores, () =>
                Assert.AreEqual(2, RoomPlayersCount, $"The guest that lagged was sent away from the next game:\n{GuestLogTail()}"));
            yield return m_driver.WaitUntil("the room after the game", () => MenuState == "OnlineLobbyState" && FocusedElement != null, 60f);
            yield return CheckTheGuestSawTheGame(HostGameNumber, hostScores);
            yield return CloseTheRoomAndWaitForTheGuest(2);
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(18), Timeout(15 * 60 * 1000)]
        public IEnumerator OnlineTransitions_GuestLagsAsATournamentGameEnds()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(43);
            SetGuestLoadingTimeout(30f);
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7804", $"freeze-at-results:{k_LagSeconds}");
            yield return LaunchTheTournamentForTheRoom();
            yield return HostGetsReadyAndPlays();
            yield return m_driver.MashButtonsUntil(() => GameplayState is "ResultsState" or "EndState" or null, random, 300f);

            m_driver.Step("Results: the guest lags, meanwhile the host leaves them, then the tournament's results");
            yield return WaitForTheGuestToFreeze();
            yield return LeaveTheResults();
            yield return m_driver.WaitUntil("the tournament results screen", () => IsInitialized(Manager(k_TournamentResultsContext)), 60f);
            yield return RealSeconds(0.5f);
            while (Manager(k_TournamentResultsContext) != null)
            {
                yield return m_driver.Submit();
                yield return RealSeconds(0.5f);
            }
            AssertTheGuestIsStillFrozen("the host chose the tournament's next game");

            m_driver.Step("Game 2: the guest catches up, the tournament's next game starts with it");
            yield return m_driver.WaitUntil("the next game to load", () => GameplayState is "TutorialState" or "GameplayState", 120f);
            Assert.AreEqual(2, RoomPlayersCount, $"The guest that lagged as the game ended was sent away from the next one:\n{GuestLogTail()}");
            Assert.IsFalse((bool)Get(Pawn(1), "HasLeft"), "The robot of the guest that lagged is out of the next game.");
            yield return HostGetsReadyAndPlays();
            yield return QuitTheGameFromThePause();
            yield return WaitForTheGuestToQuit(k_HostClosedTheRoom, 60f);
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(19), Timeout(10 * 60 * 1000)]
        public IEnumerator OnlineTransitions_HostLosesTheRoomAsTheGuestLaunches()
        {
            LogAssert.ignoreFailingMessages = true;
            // It freezes 3 s as it launches the game (its scene loads in a fraction of a second otherwise).
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7805", "freeze-at-launch:3,expect-room-lost");
            yield return LaunchPointsRushForTheRoom();

            m_driver.Step("Online: the host's connection fails as the guest launches the game");
            yield return WaitForTheGuestToFreeze();
            LoseTheHostsConnection();
            AssertTheGuestIsStillFrozen("the host's connection failed");
            yield return WaitForTheGuestToQuit(k_ConnectionLost, 90f);
            Assert.That(ReadShared(GuestReport), Does.Contain("room lost while launching a game from the menu"),
                $"The room wasn't lost while the guest was launching the game: the case wasn't reproduced.\n{GuestLogTail()}");

            m_driver.Step("Game: the host's game gives way to the hub, which says why");
            yield return m_driver.WaitUntil("the host's hub", () => MenuState == "HubState" && Manager(k_GameplayContext) == null, 90f);
            yield return CheckTheHubSaysWhy(k_HostConnectionCut);
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(20), Timeout(10 * 60 * 1000)]
        public IEnumerator OnlineTransitions_HostLosesTheRoomAsTheGameLaunches()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7806", "expect-room-lost");

            // The editor loads the game in a few frames: the connection fails as its scene loads, before it starts.
            m_driver.Step("Online: the host's connection fails as the game's scene loads");
            DoWhenASceneLoads(LoseTheHostsConnection);
            yield return LaunchPointsRushForTheRoom();
            m_driver.Step("Game: once loaded, the host's game gives way to the hub, which says why");
            yield return WaitForTheHostsGameToComeAndGo("the host's hub", () => MenuState == "HubState");
            yield return CheckTheHubSaysWhy(k_HostConnectionCut);
            yield return WaitForTheGuestToQuit(k_ConnectionLost, 90f);
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(21), Timeout(10 * 60 * 1000)]
        public IEnumerator OnlineTransitions_HostClosesTheRoomAsTheGuestsGameEnds()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(47);
            // Its end of game lasts 4 s, then asks for its results (ignored: the host drives it, and its results came
            // long before at ×4). It freezes just before, the room closes meanwhile: once back, it loses the room, then
            // its end of game's timer runs out, in the same frame.
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7807", "freeze-as-finish-ends:3,expect-room-lost");
            yield return LaunchPointsRushForTheRoom();
            yield return HostGetsReadyAndPlays();
            yield return m_driver.MashButtonsUntil(() => GameplayState is "FinishState" or "ResultsState" or "EndState" or null, random, 300f);

            m_driver.Step("Results: the room closes as the guest's end of game is about to ask for its results");
            yield return WaitForTheGuestToFreeze();
            Call(Manager(k_OnlineManager), "LeaveRoom");
            AssertTheGuestIsStillFrozen("the room closed");
            yield return WaitForTheGuestToQuit(k_HostClosedTheRoom, 60f);
            Assert.That(ReadShared(GuestReport), Does.Contain("room lost while in game state ResultsState"),
                $"The room wasn't lost in the guest's results: the case wasn't reproduced.\n{GuestLogTail()}");

            m_driver.Step("Results: the host, out of its room, leaves them");
            yield return LeaveTheResults();
            yield return m_driver.WaitUntil("the character selection", () => IsOnCharacterSelection, 60f);
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(22), Timeout(10 * 60 * 1000)]
        public IEnumerator OnlineTransitions_PauseOpenAsTheGameEnds()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(53);
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7808");
            yield return LaunchPointsRushForTheRoom();
            yield return HostGetsReadyAndPlays();

            m_driver.Step("Pause: the host opens it, online the game goes on behind it");
            m_driver.AddKeyboardAndMouse();
            yield return m_driver.Press(m_driver.Keyboard.pKey);
            yield return m_driver.WaitUntil("the pause", () => IsPaused, 5f);
            // A new configuration is announced every 10 s of the game (2.5 s here): the pause stays over them.
            yield return RealSeconds(6f);
            Assert.IsTrue(IsPaused, "The pause closed on its own during the game.");

            m_driver.Step("Pause: the game ends behind it, which closes it");
            yield return m_driver.WaitUntil("the end of the game", () => GameplayState is "FinishState" or "ResultsState" or "EndState" or null, 120f);
            yield return Frames(2);
            Assert.IsFalse(IsPaused, $"The pause stayed open over the end of the game ({GameplayState}).");
            m_driver.RemoveKeyboardAndMouse();

            yield return FinishTheGame(random);
            yield return CloseTheRoomAndWaitForTheGuest(1);
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(23), Timeout(15 * 60 * 1000)]
        public IEnumerator OnlineTransitions_HostLosesTheRoomAsATournamentGameEnds()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(59);
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7809", "expect-room-lost");
            yield return LaunchTheTournamentForTheRoom();
            yield return HostGetsReadyAndPlays();
            yield return m_driver.MashButtonsUntil(() => GameplayState is "ResultsState" or "EndState" or null, random, 300f);

            m_driver.Step("Results: the host leaves them, its connection fails as the game unloads");
            // The end state lasts a few frames in the editor: the connection fails as it enters.
            var hasFailed = false;
            DoWhenTheGameEnters("EndState", () =>
            {
                LoseTheHostsConnection();
                hasFailed = true;
            });
            yield return LeaveTheResults();
            Assert.IsTrue(hasFailed, "The host's connection didn't fail as the game ended.");

            m_driver.Step("Tournament: the host's tournament ends, the hub says why");
            // Going on, it would stay on the tournament's results, or play the next game.
            yield return WaitForRealSeconds(() => MenuState == "HubState" || GameplayState != null, 60f);
            Assert.AreEqual("HubState", MenuState, "Its room lost, the host went on with the tournament.");
            Assert.IsNull(Manager(k_TournamentContext), "The tournament context is still there in the menu.");
            yield return CheckTheHubSaysWhy(k_HostConnectionCut);
            yield return WaitForTheGuestToQuit(k_ConnectionLost, 90f);
            m_driver.AssertNoErrors();
        }

        [UnityTest, Order(24), Timeout(10 * 60 * 1000)]
        public IEnumerator OnlineTransitions_HostLosesTheRoomAsAConfigurationChanges()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return OpenARoomWithAGuestBuild("127.0.0.1:7810", "expect-room-lost");
            yield return LaunchPointsRushForTheRoom();
            yield return HostGetsReadyAndPlays();
            yield return RealSeconds(1f);

            // The room lost leaves the game (its end state requested), and in the same frame the configuration's timer
            // asks for its announcement (or the game's timer for its end, the last round of Last Man Standing...).
            m_driver.Step("Game: the host's connection fails, a new configuration comes in the same frame");
            LoseTheHostsConnection();
            var configurations = Manager("Game.Gameplay.ConfigurationsManagement.ConfigurationsManager");
            configurations.GetType().GetMethod("PerformChange").Invoke(configurations, new object[] { false });

            yield return m_driver.WaitUntil("the host's hub", () => MenuState == "HubState" && Manager(k_GameplayContext) == null, 90f);
            yield return CheckTheHubSaysWhy(k_HostConnectionCut);
            yield return WaitForTheGuestToQuit(k_ConnectionLost, 90f);
            m_driver.AssertNoErrors();
        }

        private IEnumerator LaunchTheTournamentForTheRoom()
        {
            m_driver.Step("Online: the host launches a tournament for the room");
            yield return m_driver.Select(LobbyButton("m_tournamentButton"));
            yield return m_driver.Submit();
            yield return ConfirmLaunch();
        }

        // The main player's Submit leaves the results once their close-ups are over.
        private IEnumerator LeaveTheResults()
        {
            while (GameplayState == "ResultsState")
            {
                yield return m_driver.Submit();
                var next = Time.realtimeSinceStartup + 0.5f;
                while (Time.realtimeSinceStartup < next && GameplayState == "ResultsState")
                    yield return null;
            }
        }

        // The host's game was launching: it loads, then gives way to the menu (and not before it loads: the menu would
        // still be there).
        private IEnumerator WaitForTheHostsGameToComeAndGo(string what, Func<bool> inTheMenu)
        {
            yield return m_driver.WaitUntil("the host's game to load", () => Manager(k_GameplayContext) != null, 60f);
            yield return m_driver.WaitUntil(what, () => Manager(k_GameplayContext) == null && inTheMenu(), 90f);
        }

        // As the next scene loads (its objects awake, not started yet), once.
        private static void DoWhenASceneLoads(Action action)
        {
            UnityAction<Scene, LoadSceneMode> handler = null;
            handler = (scene, mode) =>
            {
                SceneManager.sceneLoaded -= handler;
                Debug.Log($"[Scenario] {scene.name} loaded.");
                action();
            };
            SceneManager.sceneLoaded += handler;
        }

        // As the game's flow machine enters that state (FlowMachine.OnStateEntering), once.
        private void DoWhenTheGameEnters(string stateName, Action action)
        {
            var flowMachine = Get(Manager(k_GameplayContext), "FlowMachine");
            var onStateEntering = flowMachine.GetType().GetEvent("OnStateEntering");
            Delegate handler = null;
            Action<object> onEntering = state =>
            {
                if (((Component)state).name != stateName)
                    return;
                onStateEntering.RemoveEventHandler(flowMachine, handler);
                action();
            };
            handler = Delegate.CreateDelegate(onStateEntering.EventHandlerType, onEntering.Target, onEntering.Method);
            onStateEntering.AddEventHandler(flowMachine, handler);
        }

        private IEnumerator WaitForTheGuestToFreeze()
        {
            yield return WaitForRealSeconds(() => ReadShared(GuestReport).Contains("[GuestBot] freezing"), 15f);
            Assert.That(ReadShared(GuestReport), Does.Contain("[GuestBot] freezing"), $"The guest didn't freeze:\n{GuestLogTail()}");
        }

        // What the host did happened while the guest was frozen, or the case wasn't reproduced.
        private static void AssertTheGuestIsStillFrozen(string what)
        {
            if (ReadShared(GuestReport).Contains("[GuestBot] unfrozen"))
                Assert.Inconclusive($"The guest was back before {what}: the case wasn't reproduced.");
        }

        // The host's transport fails (a Relay allocation lost, the host's network down): the room ends on its side, its
        // guests are disconnected.
        private static void LoseTheHostsConnection()
        {
            Debug.Log("[Scenario] The host's connection fails.");
            var onlineManager = Manager(k_OnlineManager);
            onlineManager.GetType().GetMethod("HandleConnectionLost", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(onlineManager, new object[] { k_HostConnectionCut });
        }

        // The hub's notice, with the reason the room ended, taken from the online manager.
        private IEnumerator CheckTheHubSaysWhy(string reason)
        {
            yield return m_driver.WaitUntil("the hub's notice", () => HubNotice == reason, 10f);
            Assert.IsFalse((bool)Get(Manager(k_OnlineManager), "HasLostRoomReason"), "The reason the room ended is still to be told.");
        }

        private static string HubNotice
        {
            get
            {
                try
                {
                    return ScreenElement("Game.MainMenu.Hub.HubScreen", "notice")?.Q<Label>("message")?.text;
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
        }
    }
}
