using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Game.Tests.PlayMode.ScenarioDriver;

namespace Game.Tests.PlayMode
{
    /// <summary>A player alone against the computer's robots: the hub's choice, a game of each objective, a tournament.</summary>
    public partial class GameModesScenarioTests
    {
        private const string k_HubScreen = "Game.MainMenu.Hub.HubScreen";

        /// <summary>
        /// The player leaves the pad alone for the whole game: everything that happens comes from the bots. Each objective
        /// must be played for real: points collected, robots shot, a last robot standing.
        /// </summary>
        [UnityTest, Order(3), Timeout(10 * 60 * 1000)]
        public IEnumerator Bots_PlayAGameWithAPlayerAlone(
            [ValueSource(nameof(TrainingModes))] string mode)
        {
            LogAssert.ignoreFailingMessages = true;

            yield return JoinAloneAndOpenHub();
            var characters = PlayerCharacters;
            var botsButton = (Button)ScreenElement(k_HubScreen, "bots-button");
            StringAssert.Contains("3", botsButton.text, "A player alone should play against 3 bots by default.");
            yield return StartTraining(mode);

            m_driver.Step("Tutorial: the bots are ready, the player gets ready");
            yield return m_driver.WaitUntil("the tutorial", () => GameplayState == "TutorialState", 180f);
            yield return RealSeconds(0.5f);
            if (mode == "Points Rush")
                yield return CaptureGameScreen("32-tutorial-bots");
            Assert.AreEqual("TutorialState", GameplayState, "The bots started without the player.");
            yield return m_driver.Press(m_driver.Gamepads[0].buttonSouth);

            m_driver.Step($"{mode}: the bots play");
            yield return m_driver.WaitUntil("the game", () => GameplayState == "GameplayState", 60f);
            yield return RealSeconds(4f);
            yield return CaptureGameScreen($"50-game-{mode.Replace(' ', '-')}");
            yield return m_driver.WaitUntil("the results", () => GameplayState is "ResultsState" or "EndState" or null, 300f);
            var scores = Scores;
            Debug.Log($"[Scenario] {mode} with bots finished. Scores: {scores}");
            var values = scores.Split(',').Select(score => int.Parse(score.Trim())).ToList();
            Assert.AreEqual(4, values.Count, $"Expected a player and 3 bots: {scores}.");
            Assert.Greater(values.Skip(1).Sum(), 0, $"The bots did nothing in {mode}: {scores}.");

            // The main player's Submit is only listened to once the close-ups are over.
            m_driver.Step($"{mode}: results");
            while (GameplayState == "ResultsState")
            {
                yield return m_driver.Submit();
                yield return RealSeconds(0.5f);
            }
            yield return BackToCharacterSelectionWith(characters);
            m_driver.AssertNoErrors();
        }

        /// <summary>The bots' number is chosen in the hub, and a tournament goes to its end against them.</summary>
        [UnityTest, Order(3), Timeout(30 * 60 * 1000)]
        public IEnumerator Bots_PlayATournamentWithAPlayerAlone()
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(7);

            yield return JoinAloneAndOpenHub();
            var characters = PlayerCharacters;

            m_driver.Step("Hub: from 3 bots to 1, back to 3");
            var botsButton = (Button)ScreenElement(k_HubScreen, "bots-button");
            yield return m_driver.Select(botsButton);
            yield return RealSeconds(0.2f);
            yield return CaptureScreen("33-hub-bots");
            StringAssert.Contains("3", botsButton.text);
            yield return m_driver.Submit();
            StringAssert.Contains("1", botsButton.text, "A player alone needs a bot at least: the count goes round to 1.");
            yield return m_driver.Submit();
            StringAssert.Contains("2", botsButton.text);
            yield return m_driver.Submit();
            StringAssert.Contains("3", botsButton.text);

            yield return StartTournament();

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
                    () => IsOnCharacterSelection || IsOnMainMenuHome || GameplayState != null, 180f);
                if (IsOnCharacterSelection || IsOnMainMenuHome)
                    break;
            }

            Debug.Log($"[Scenario] Tournament with bots over after {played.Count} games: {string.Join(", ", played)}");
            yield return BackToCharacterSelectionWith(characters);
            m_driver.AssertNoErrors();
        }
    }
}
