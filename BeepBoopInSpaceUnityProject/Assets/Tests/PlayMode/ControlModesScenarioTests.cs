using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        private const string k_CharactersManager = "Game.Gameplay.CharactersManagement.CharactersManager";
        private const string k_TutorialPanel = "Game.Gameplay.Flows.Tutorial.TutorialPanel";
        private const string k_SniperBulletShooter = "Game.Gameplay.CharactersManagement.SpecialActionsSystem.SniperRifle.SniperBulletShooter";
        private const string k_KillingCell = "Game.Gameplay.GridSystem.GenericComponents.KillingCellComponent";
        private const int k_ActionButtonsCount = 6;
        private const int k_PadPlayersWithAKeyboard = 3;

        // The direction each simplified action faces, in the world, and on the screen, and the neighbor cell it walks to.
        private static readonly Dictionary<string, (string Direction, Vector3 World, Vector2 Screen, string Cell)> s_simplifiedActions = new()
        {
            { "GoUp", ("Z", Vector3.forward, Vector2.up, "ForwardCell") },
            { "GoDown", ("MinusZ", Vector3.back, Vector2.down, "BackwardCell") },
            { "GoRight", ("X", Vector3.right, Vector2.right, "RightCell") },
            { "GoLeft", ("MinusX", Vector3.left, Vector2.left, "LeftCell") },
        };

        // Cells that do something to the robot walking on them.
        private static readonly string[] s_cellEffects =
        {
            k_KillingCell,
            "Game.Gameplay.Cells.FrozenCell.FrozenCellEffectHandler",
            "Game.Gameplay.Cells.RandomTeleportationCell.RandomTeleporter",
            "Game.Gameplay.Cells.ToEndPointTeleportationCell.ToEndPointTeleporter",
        };

        // "<Training mode>: <control mode>". Shooters scores with the sniper rifle: only with the special action.
        public static readonly string[] SimplifiedControlsCases =
        {
            "Points Rush: Simplified",
            "Points Rush: SimplifiedWithSpecialAction",
            "Shooters: SimplifiedWithSpecialAction",
        };

        private Action m_restoreControlMode;

        /// <summary>
        /// A level in a simplified control mode, played by three pads and a keyboard: the tutorial shows the mode's
        /// buttons, only those have an action, each direction turns the robot towards it, towards that side of the
        /// screen, and walks there (checked with a pad and with the keyboard), the unused buttons do nothing, the special
        /// action's button plays it, and a new configuration shuffles the actions again on the same buttons. Then the
        /// game is played to its end.
        /// </summary>
        [UnityTest, Order(1), Timeout(15 * 60 * 1000)]
        public IEnumerator ControlModes_SimplifiedButtonsGoWhereTheySay([ValueSource(nameof(SimplifiedControlsCases))] string levelAndControls)
        {
            LogAssert.ignoreFailingMessages = true;
            var parts = levelAndControls.Split(new[] { ": " }, StringSplitOptions.None);
            yield return PlaySimplifiedControls(parts[0], parts[1], switchLevel: true);
        }

        /// <summary>
        /// A game of <paramref name="mode"/> in <paramref name="controlMode"/>: the level is switched to it for the test
        /// with <paramref name="switchLevel"/>, otherwise it must already be in that mode.
        /// </summary>
        private IEnumerator PlaySimplifiedControls(string mode, string controlMode, bool switchLevel)
        {
            var random = new System.Random(30);
            var withSpecialAction = controlMode == "SimplifiedWithSpecialAction";
            var buttonsCount = withSpecialAction ? 5 : 4;

            yield return JoinPadsAndAKeyboardAndOpenHub();
            var characters = PlayerCharacters;
            var keyboard = m_driver.Keyboard;
            yield return StartTraining(mode, switchLevel ? level => SetControlMode(level, controlMode) : null);

            m_driver.Step("Tutorial: the controller shows the mode's buttons");
            yield return m_driver.WaitUntil("the tutorial", () => GameplayState == "TutorialState", 180f);
            yield return RealSeconds(0.5f);
            Assert.AreEqual(controlMode, Get(Manager(k_GameplayContext), "ControlMode").ToString(),
                "The game doesn't play the level's control mode.");
            var controlsClass = withSpecialAction ? "tutorial__controls--five-buttons" : "tutorial__controls--four-buttons";
            Assert.IsTrue(ScreenElement(k_TutorialPanel, "controls").ClassListContains(controlsClass),
                $"The tutorial doesn't show the controller of the {controlMode} mode.");
            Assert.AreEqual(withSpecialAction, ScreenElement(k_TutorialPanel, "special-action-label").resolvedStyle.display == DisplayStyle.Flex,
                withSpecialAction ? "The tutorial hides the special action." : "The tutorial shows a special action no button plays.");
            yield return CaptureGameScreen($"40-tutorial-{mode.Replace(" ", "")}-{controlMode}");
            foreach (var pad in m_driver.Gamepads.Take(k_PadPlayersWithAKeyboard))
                yield return m_driver.Press(pad.buttonSouth);
            yield return m_driver.Press(keyboard.spaceKey);
            yield return m_driver.WaitUntil("the game", () => GameplayState == "GameplayState", 60f);

            // At the real pace: a new configuration comes every 10s of game time.
            Time.timeScale = 1f;
            m_driver.Step("Game: only the mode's buttons have an action, the same for every robot");
            var configuration = AssertSimplifiedConfiguration(buttonsCount, withSpecialAction);

            var checkedDirections = new HashSet<string>();
            foreach (var buttons in new[] { ActionButtons(m_driver.Gamepads[0]), ActionButtons(keyboard) })
            {
                var device = buttons[0].device;
                m_driver.Step($"Game: the buttons of '{device.name}' the mode doesn't use do nothing");
                for (int button = buttonsCount; button < buttons.Length; button++)
                    yield return PressAndCheckNothingHappens(buttons[button]);

                m_driver.Step($"Game: each direction turns the robot of '{device.name}' towards it and walks there");
                var directions = Enumerable.Range(0, buttonsCount).Where(button => configuration[button] != "PlaySpecialAction").ToList();
                while (directions.Count > 0)
                {
                    var robot = RobotOf(device);
                    if (!CanAct(robot))
                    {
                        Debug.Log($"[Scenario] The robot of '{device.name}' can't act any more (lava, ice...): " +
                                  $"{string.Join(", ", directions.Select(button => configuration[button]))} not checked with it.");
                        break;
                    }
                    // Lava kills, ice blocks, a teleporter sends anywhere: the cells with an effect come last.
                    var next = directions.OrderBy(button => HasEffect(TargetOf(robot, configuration[button]))).First();
                    directions.Remove(next);
                    yield return PressAndCheckDirection(buttons[next], configuration[next]);
                    checkedDirections.Add(configuration[next]);
                }
            }
            CollectionAssert.IsSubsetOf(s_simplifiedActions.Keys, checkedDirections, "A direction was never checked.");

            if (withSpecialAction)
            {
                // A robot that hasn't moved yet: it can't be on lava or ice.
                m_driver.Step("Game: the special action's button plays it with the robot of the second pad");
                yield return PressAndCheckSpecialAction(ActionButtons(m_driver.Gamepads[1])[Array.IndexOf(configuration, "PlaySpecialAction")]);
            }

            m_driver.Step("Game: a new configuration shuffles the actions on the same buttons");
            // The robots get a new configuration once the announcement is over.
            var previousConfiguration = ButtonActions(Robots().First());
            yield return m_driver.WaitUntil("a new configuration", () => GameplayState == "AnnouncementState", 15f);
            yield return m_driver.WaitUntil("the game after the announcement", () => GameplayState == "GameplayState", 15f);
            var newConfiguration = AssertSimplifiedConfiguration(buttonsCount, withSpecialAction);
            CollectionAssert.AreNotEqual(previousConfiguration, newConfiguration, "The new configuration gave the buttons the same actions.");

            Time.timeScale = k_TimeScale;
            m_driver.Step("Game: until the results");
            yield return m_driver.MashButtonsUntil(() => GameplayState is "ResultsState" or "EndState" or null, random, 300f);
            while (GameplayState == "ResultsState")
            {
                yield return m_driver.Submit();
                yield return RealSeconds(0.5f);
            }
            yield return BackToCharacterSelectionWith(characters);
            // The players leave before the keyboard they play with is removed.
            yield return GoToMainMenuHome();

            m_driver.AssertNoErrors();
        }

        private IEnumerator JoinPadsAndAKeyboardAndOpenHub()
        {
            m_driver.Step("Home: press Play");
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the character selection screen", () => MenuState == "CharacterSelectionState");

            m_driver.Step($"Character selection: {k_PadPlayersWithAKeyboard} pads and a keyboard join and get ready");
            for (int i = 0; i < k_PadPlayersWithAKeyboard; i++)
                yield return Join(i);
            m_driver.AddKeyboardAndMouse();
            var keyboard = m_driver.Keyboard;
            var keyboardSlot = PlayerCount;
            yield return m_driver.Press(keyboard.spaceKey);
            yield return m_driver.WaitUntil("the keyboard player to join", () => PlayerCount == keyboardSlot + 1, 10f);
            yield return RealSeconds(0.5f);

            for (int i = 0; i < k_PadPlayersWithAKeyboard; i++)
                yield return GetReady(i);
            // Ready (Space), with Random (R) first while another player has the character.
            for (int attempt = 0; attempt < 8 && !IsReady(keyboardSlot); attempt++)
            {
                if (attempt > 0)
                    yield return m_driver.Press(keyboard.rKey);
                yield return m_driver.Press(keyboard.spaceKey);
                yield return Frames(2);
            }
            if (!IsReady(keyboardSlot))
                m_driver.Fail("The keyboard player never got ready.");
            yield return m_driver.WaitUntil("the hub once everybody is ready", () => MenuState == "HubState", 10f);
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

        /// <summary>The configuration of the robots, checked against the mode, and the same for all of them.</summary>
        private static string[] AssertSimplifiedConfiguration(int buttonsCount, bool withSpecialAction)
        {
            var expected = s_simplifiedActions.Keys.ToList();
            if (withSpecialAction)
                expected.Add("PlaySpecialAction");

            var configurations = Robots().Select(ButtonActions).ToList();
            var configuration = configurations[0];
            Debug.Log($"[Scenario] Configuration: {string.Join(", ", configuration.Select(action => action ?? "-"))}");
            CollectionAssert.AreEquivalent(expected, configuration.Take(buttonsCount), "The mode's buttons don't have its actions.");
            Assert.That(configuration.Skip(buttonsCount), Is.All.Null, "A button the mode doesn't use has an action.");
            foreach (var other in configurations.Skip(1))
                CollectionAssert.AreEqual(configuration, other, "The robots don't all have the same configuration.");
            return configuration;
        }

        private IEnumerator PressAndCheckDirection(ButtonControl button, string action)
        {
            var robot = RobotOf(button.device);
            var movement = Get(robot, "MovementController");
            var walker = Get(robot, "GridWalker");
            AssertCanAct(robot, movement, button.device);
            var (direction, world, screen, neighbor) = s_simplifiedActions[action];
            var cell = (Component)Get(walker, "CurrentCell");
            var target = Get(cell, neighbor) as Component;

            yield return m_driver.Press(button);

            Assert.AreEqual(direction, Get(movement, "CurrentDirection").ToString(),
                $"{button.displayName} ({action}) didn't turn the robot towards {direction}.");
            // Unchanged when the cell there is missing or taken. Read before the move ends: a teleporter acts then.
            var newCell = Get(walker, "CurrentCell");
            Assert.IsTrue(ReferenceEquals(newCell, target) || ReferenceEquals(newCell, cell),
                $"{button.displayName} ({action}) took the robot elsewhere than its {neighbor}.");
            if (target != null)
            {
                var camera = Camera.main;
                var onScreen = (Vector2)(camera.WorldToScreenPoint(target.transform.position) - camera.WorldToScreenPoint(cell.transform.position));
                Assert.Greater(Vector2.Dot(onScreen.normalized, screen), 0.7f,
                    $"{button.displayName} ({action}) leads towards {onScreen} on the screen.");
            }
            // The model turns in 0.2s, unless the robot dies on lava at the end of its jump.
            if (target != null && target.GetComponentInChildren(GameType(k_KillingCell)) != null)
                yield break;
            yield return RealSeconds(0.3f);
            var forward = ((Transform)Get(robot, "Root")).forward;
            Assert.Greater(Vector3.Dot(forward, world), 0.9f, $"{button.displayName} ({action}): the robot's model faces {forward}.");
        }

        private static Component TargetOf(object robot, string action) =>
            Get(Get(Get(robot, "GridWalker"), "CurrentCell"), s_simplifiedActions[action].Cell) as Component;

        private static bool HasEffect(Component cell) =>
            cell != null && s_cellEffects.Any(effect => cell.GetComponentInChildren(GameType(effect)) != null);

        private static bool CanAct(object robot) =>
            GameplayState == "GameplayState" && (bool)Get(Get(robot, "DeathController"), "IsAlive")
            && !(bool)Get(Get(robot, "MovementController"), "IsBlocked");

        private IEnumerator PressAndCheckNothingHappens(ButtonControl button)
        {
            var robot = RobotOf(button.device);
            var movement = Get(robot, "MovementController");
            var walker = Get(robot, "GridWalker");
            AssertCanAct(robot, movement, button.device);
            var direction = Get(movement, "CurrentDirection");
            var cell = Get(walker, "CurrentCell");

            yield return m_driver.Press(button);

            Assert.AreEqual(direction, Get(movement, "CurrentDirection"), $"{button.displayName} turned the robot.");
            Assert.IsTrue(ReferenceEquals(cell, Get(walker, "CurrentCell")), $"{button.displayName} moved the robot.");
        }

        // The bomb blows its robot up; the sniper rifle shoots where the robot faces.
        private IEnumerator PressAndCheckSpecialAction(ButtonControl button)
        {
            var robot = RobotOf(button.device);
            var movement = Get(robot, "MovementController");
            var death = Get(robot, "DeathController");
            AssertCanAct(robot, movement, button.device);
            var shooter = ((Component)Get(robot, "SpecialAction")).GetComponent(GameType(k_SniperBulletShooter));
            if (shooter == null)
            {
                Assert.IsFalse((bool)Get(death, "IsInvincible"), $"The robot of '{button.device.name}' is invincible: the bomb can't blow it up.");
                yield return m_driver.Press(button);
                yield return m_driver.WaitUntil("the bomb to blow the robot up", () => !(bool)Get(death, "IsAlive"), 5f);
                yield break;
            }

            // OnShot(shooter, bullet), once the bullet is aimed.
            var shots = new List<Vector3>();
            var onShot = shooter.GetType().GetEvent("OnShot");
            var parameters = onShot.EventHandlerType.GetMethod("Invoke").GetParameters()
                .Select(parameter => Expression.Parameter(parameter.ParameterType)).ToArray();
            Action<Component> record = bullet => shots.Add(bullet.transform.forward);
            var handler = Expression.Lambda(onShot.EventHandlerType,
                Expression.Invoke(Expression.Constant(record), Expression.Convert(parameters[1], typeof(Component))), parameters).Compile();
            onShot.AddEventHandler(shooter, handler);
            yield return m_driver.Press(button);
            onShot.RemoveEventHandler(shooter, handler);

            Assert.AreEqual(1, shots.Count, $"{button.displayName} didn't shoot once.");
            var facing = (Vector3)movement.GetType().GetMethod("GetWorldDirection").Invoke(movement, null);
            Assert.Greater(Vector3.Dot(shots[0], facing), 0.99f, $"{button.displayName} shot towards {shots[0]}, not where the robot faces ({facing}).");
        }

        private static void AssertCanAct(object robot, object movement, InputDevice device)
        {
            Assert.AreEqual("GameplayState", GameplayState, "The game was interrupted while checking the buttons.");
            Assert.IsTrue((bool)Get(Get(robot, "DeathController"), "IsAlive"), $"The robot of '{device.name}' died before the check.");
            Assert.IsFalse((bool)Get(movement, "IsBlocked"), $"The robot of '{device.name}' can't move.");
        }

        // The buttons of the Gameplay action map, from Action_1 to Action_6.
        private static ButtonControl[] ActionButtons(Gamepad pad) =>
            new[] { pad.buttonSouth, pad.buttonNorth, pad.buttonEast, pad.buttonWest, pad.rightShoulder, pad.leftShoulder };

        private static ButtonControl[] ActionButtons(Keyboard keyboard) =>
            new ButtonControl[] { keyboard.sKey, keyboard.wKey, keyboard.dKey, keyboard.aKey, keyboard.eKey, keyboard.qKey };

        /// <summary>The method each button calls, by index of the Gameplay action map's Action_N - 1.</summary>
        private static string[] ButtonActions(object robot)
        {
            var actions = (Array)Get(Get(Get(robot, "ActionsController"), "KeysConfiguration"), "Action");
            return actions.Cast<Delegate>().Take(k_ActionButtonsCount).Select(action => action?.Method.Name).ToArray();
        }

        /// <summary>The references holder of each robot, with the input of the player who plays it.</summary>
        private static IEnumerable<(object Robot, PlayerInput Input)> RobotsAndInputs()
        {
            var association = (IEnumerable)Get(Manager(k_CharactersManager), "CharacterPlayerControllersAssociation");
            foreach (var pair in association)
                yield return (Get(Get(pair, "Value"), "ReferencesHolder"), (PlayerInput)Get(Get(Get(pair, "Key"), "Player"), "PlayerInput"));
        }

        private static IEnumerable<object> Robots() => RobotsAndInputs().Select(robot => robot.Robot);

        /// <summary>The references holder of the robot played with this device.</summary>
        private static object RobotOf(InputDevice device) =>
            RobotsAndInputs().FirstOrDefault(robot => robot.Input.devices.Contains(device)).Robot
            ?? throw new InvalidOperationException($"No robot is played with '{device.name}'.");
    }
}
