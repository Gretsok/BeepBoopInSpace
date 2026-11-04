using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Game.Tests.PlayMode.ScenarioDriver;
using Object = UnityEngine.Object;

namespace Game.Tests.PlayMode
{
    /// <summary>The control modes a level picks: the simplified ones, on a Training level switched to them for the test.</summary>
    public partial class GameModesScenarioTests
    {
        private const string k_ControlModesTestMode = "Points Rush";
        private const string k_CharactersManager = "Game.Gameplay.CharactersManagement.CharactersManager";
        private const string k_TutorialPanel = "Game.Gameplay.Flows.Tutorial.TutorialPanel";
        private const int k_ActionButtonsCount = 6;

        // The direction each simplified action faces, and the neighbor cell it walks to.
        private static readonly Dictionary<string, (string Direction, string Cell)> s_simplifiedActions = new()
        {
            { "GoUp", ("Z", "ForwardCell") },
            { "GoDown", ("MinusZ", "BackwardCell") },
            { "GoRight", ("X", "RightCell") },
            { "GoLeft", ("MinusX", "LeftCell") },
        };

        private Action m_restoreControlMode;

        /// <summary>
        /// A level in a simplified control mode: the tutorial shows its buttons, only those have an action, each
        /// direction turns the robot towards it and walks there, the unused buttons do nothing, and a new configuration
        /// shuffles the actions again on the same buttons. Then the game is played to its end.
        /// </summary>
        [UnityTest, Order(1), Timeout(15 * 60 * 1000)]
        public IEnumerator ControlModes_SimplifiedButtonsGoWhereTheySay(
            [Values("Simplified", "SimplifiedWithSpecialAction")] string controlMode)
        {
            LogAssert.ignoreFailingMessages = true;
            var random = new System.Random(30);
            var withSpecialAction = controlMode == "SimplifiedWithSpecialAction";
            var buttonsCount = withSpecialAction ? 5 : 4;

            yield return JoinFourPlayersAndOpenHub();
            var characters = PlayerCharacters;
            yield return StartTraining(k_ControlModesTestMode, level => SetControlMode(level, controlMode));

            m_driver.Step("Tutorial: the controller shows the mode's buttons");
            yield return m_driver.WaitUntil("the tutorial", () => GameplayState == "TutorialState", 180f);
            yield return RealSeconds(0.5f);
            var controlsClass = withSpecialAction ? "tutorial__controls--five-buttons" : "tutorial__controls--four-buttons";
            Assert.IsTrue(ScreenElement(k_TutorialPanel, "controls").ClassListContains(controlsClass),
                $"The tutorial doesn't show the controller of the {controlMode} mode.");
            Assert.AreEqual(withSpecialAction, ScreenElement(k_TutorialPanel, "special-action-label").resolvedStyle.display == DisplayStyle.Flex,
                withSpecialAction ? "The tutorial hides the special action." : "The tutorial shows a special action no button plays.");
            yield return CaptureGameScreen($"40-tutorial-{controlMode}");
            foreach (var pad in m_driver.Gamepads)
                yield return m_driver.Press(pad.buttonSouth);
            yield return m_driver.WaitUntil("the game", () => GameplayState == "GameplayState", 60f);

            // At the real pace: a new configuration comes every 10s of game time.
            Time.timeScale = 1f;
            m_driver.Step("Game: only the mode's buttons have an action, the same for every robot");
            var configuration = AssertSimplifiedConfiguration(buttonsCount, withSpecialAction);

            m_driver.Step("Game: each direction turns player 1's robot towards it and walks there");
            var pad1Buttons = ActionButtons(m_driver.Gamepads[0]);
            for (int button = 0; button < buttonsCount; button++)
            {
                var action = configuration[button];
                // The bomb would blow the robot up.
                if (action == "PlaySpecialAction")
                    continue;
                yield return PressAndCheckDirection(pad1Buttons[button], action);
            }

            m_driver.Step("Game: the buttons the mode doesn't use do nothing");
            for (int button = buttonsCount; button < pad1Buttons.Length; button++)
                yield return PressAndCheckNothingHappens(pad1Buttons[button]);

            m_driver.Step("Game: a new configuration shuffles the actions on the same buttons");
            yield return m_driver.WaitUntil("a new configuration", () => GameplayState == "AnnouncementState", 15f);
            yield return m_driver.WaitUntil("the game after the announcement", () => GameplayState == "GameplayState", 15f);
            var newConfiguration = AssertSimplifiedConfiguration(buttonsCount, withSpecialAction);
            CollectionAssert.AreNotEqual(configuration, newConfiguration, "The new configuration gave the buttons the same actions.");

            Time.timeScale = k_TimeScale;
            m_driver.Step("Game: until the results");
            yield return m_driver.MashButtonsUntil(() => GameplayState is "ResultsState" or "EndState" or null, random, 300f);
            while (GameplayState == "ResultsState")
            {
                yield return m_driver.Submit();
                yield return RealSeconds(0.5f);
            }
            yield return BackToCharacterSelectionWith(characters);

            m_driver.AssertNoErrors();
        }

        // The level asset stays loaded between scenarios: switched for one test only.
        private void SetControlMode(Object level, string controlMode)
        {
            var field = level.GetType().GetField("<ControlMode>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? throw new MissingFieldException(level.GetType().Name, "ControlMode");
            var previous = field.GetValue(level);
            field.SetValue(level, Enum.Parse(field.FieldType, controlMode));
            m_restoreControlMode = () => field.SetValue(level, previous);
        }

        private void RestoreControlMode()
        {
            m_restoreControlMode?.Invoke();
            m_restoreControlMode = null;
        }

        /// <summary>The configuration of player 1's robot, checked against the mode and the other robots'.</summary>
        private string[] AssertSimplifiedConfiguration(int buttonsCount, bool withSpecialAction)
        {
            var expected = s_simplifiedActions.Keys.ToList();
            if (withSpecialAction)
                expected.Add("PlaySpecialAction");

            var configuration = ButtonActions(RobotOf(m_driver.Gamepads[0]));
            Debug.Log($"[Scenario] Configuration: {string.Join(", ", configuration.Select(action => action ?? "-"))}");
            CollectionAssert.AreEquivalent(expected, configuration.Take(buttonsCount), "The mode's buttons don't have its actions.");
            Assert.That(configuration.Skip(buttonsCount), Is.All.Null, "A button the mode doesn't use has an action.");
            foreach (var pad in m_driver.Gamepads.Skip(1))
                CollectionAssert.AreEqual(configuration, ButtonActions(RobotOf(pad)), $"{pad.name}'s robot has another configuration.");
            return configuration;
        }

        private IEnumerator PressAndCheckDirection(ButtonControl button, string action)
        {
            var robot = RobotOf((Gamepad)button.device);
            var movement = Get(robot, "MovementController");
            var walker = Get(robot, "GridWalker");
            AssertCanAct(robot, movement);
            var (direction, neighbor) = s_simplifiedActions[action];
            var cell = Get(walker, "CurrentCell");
            var target = Get(cell, neighbor);

            yield return m_driver.Press(button);

            Assert.AreEqual(direction, Get(movement, "CurrentDirection").ToString(),
                $"{button.displayName} ({action}) didn't turn the robot towards {direction}.");
            // Unchanged when the cell there is missing or taken. Read before the move ends: a teleporter acts then.
            var newCell = Get(walker, "CurrentCell");
            Assert.IsTrue(ReferenceEquals(newCell, target) || ReferenceEquals(newCell, cell),
                $"{button.displayName} ({action}) took the robot elsewhere than its {neighbor}.");
        }

        private IEnumerator PressAndCheckNothingHappens(ButtonControl button)
        {
            var robot = RobotOf((Gamepad)button.device);
            var movement = Get(robot, "MovementController");
            var walker = Get(robot, "GridWalker");
            AssertCanAct(robot, movement);
            var direction = Get(movement, "CurrentDirection");
            var cell = Get(walker, "CurrentCell");

            yield return m_driver.Press(button);

            Assert.AreEqual(direction, Get(movement, "CurrentDirection"), $"{button.displayName} turned the robot.");
            Assert.IsTrue(ReferenceEquals(cell, Get(walker, "CurrentCell")), $"{button.displayName} moved the robot.");
        }

        private static void AssertCanAct(object robot, object movement)
        {
            Assert.AreEqual("GameplayState", GameplayState, "The game was interrupted while checking the buttons.");
            Assert.IsTrue((bool)Get(Get(robot, "DeathController"), "IsAlive"), "Player 1's robot died before the check.");
            Assert.IsFalse((bool)Get(movement, "IsBlocked"), "Player 1's robot can't move.");
        }

        // The buttons of the Gameplay action map, from Action_1 to Action_6.
        private static ButtonControl[] ActionButtons(Gamepad pad) =>
            new[] { pad.buttonSouth, pad.buttonNorth, pad.buttonEast, pad.buttonWest, pad.rightShoulder, pad.leftShoulder };

        /// <summary>The method each button calls, by index of the Gameplay action map's Action_N - 1.</summary>
        private static string[] ButtonActions(object robot)
        {
            var actions = (Array)Get(Get(Get(robot, "ActionsController"), "KeysConfiguration"), "Action");
            return actions.Cast<Delegate>().Take(k_ActionButtonsCount).Select(action => action?.Method.Name).ToArray();
        }

        /// <summary>The references holder of the robot played with this pad.</summary>
        private static object RobotOf(Gamepad pad)
        {
            var association = (IEnumerable)Get(Manager(k_CharactersManager), "CharacterPlayerControllersAssociation");
            foreach (var pair in association)
            {
                var playerInput = (PlayerInput)Get(Get(Get(pair, "Key"), "Player"), "PlayerInput");
                if (playerInput.devices.Contains(pad))
                    return Get(Get(pair, "Value"), "ReferencesHolder");
            }
            throw new InvalidOperationException($"No robot is played with '{pad.name}'.");
        }
    }
}
