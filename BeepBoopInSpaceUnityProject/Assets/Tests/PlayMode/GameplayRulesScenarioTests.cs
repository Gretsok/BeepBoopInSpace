using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Game.Tests.PlayMode.ScenarioDriver;
using Object = UnityEngine.Object;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// Rules a hurried player could slip through, in a game of Shooters: water and lava kill a robot that steps off them
    /// before landing, and the sniper rifle kills along the shooter's row of cells, even right after a step aside or a
    /// turn. The robots are placed and driven through their components.
    /// </summary>
    public partial class GameModesScenarioTests
    {
        private const string k_Cell = "Game.Gameplay.Cells.Default.Cell";
        private const string k_WalkableCell = "Game.Gameplay.GridSystem.GenericComponents.CanBeWalkedOnCellComponent";
        private const string k_CollisionsHandler = "Game.Gameplay.CharactersManagement.CollisionsHandling.CharacterCollisionsHandler";
        private const string k_Direction = "Game.Gameplay.CharactersManagement.Movement.CharacterMovementController+EDirection";
        private const string k_ShootingMode = "Shooters";
        private const int k_ShotAttempts = 3;

        // The grid's directions: the robot's direction, the neighbor cell that way, the one the other way, and the axis.
        private static readonly (string Direction, string Ahead, string Behind, int Axis)[] s_gridDirections =
        {
            ("Z", "ForwardCell", "BackwardCell", 0),
            ("MinusZ", "BackwardCell", "ForwardCell", 0),
            ("X", "RightCell", "LeftCell", 1),
            ("MinusX", "LeftCell", "RightCell", 1),
        };

        /// <summary>
        /// A robot steps onto water or lava and, a frame later, towards the next cell, like a player mashing the
        /// button: it stays on the water or lava, which kills it as it lands.
        /// </summary>
        [UnityTest, Order(1), Timeout(10 * 60 * 1000)]
        public IEnumerator Hazards_HurryingOverWaterOrLavaStillKills()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return JoinFourPlayersAndOpenHub();
            var characters = PlayerCharacters;
            yield return StartTraining(k_ShootingMode);
            yield return UntilTheGameIsOn();
            Time.timeScale = 1f;

            m_driver.Step("Game: a robot steps onto water or lava, then right away towards the next cell");
            var robot = Robots().First();
            var movement = Get(robot, "MovementController");
            var walker = Get(robot, "GridWalker");
            yield return m_driver.WaitUntil("the robot to be able to move", () => CanAct(robot), 20f);
            var (before, hazard, after) = FindHazardCrossing(robot);
            CallWith(movement, "TeleportToCell", before);
            CallWith(movement, "MoveToCell", hazard);
            yield return null;
            Assert.IsTrue(IsAliveRobot(robot) && ReferenceEquals(hazard, Get(walker, "CurrentCell")),
                "The robot isn't in its jump onto the water or lava a frame later: the check proves nothing.");
            CallWith(movement, "MoveToCell", after);
            Assert.IsTrue(ReferenceEquals(hazard, Get(walker, "CurrentCell")), "The robot left the water or lava before landing on it.");
            yield return m_driver.WaitUntil("the water or lava to kill the robot", () => !IsAliveRobot(robot), 3f);

            yield return FinishTheGame(characters);
        }

        /// <summary>
        /// A robot steps aside onto a row of cells and shoots along it at once, then turns towards it and shoots at once:
        /// each shot kills the robot three cells ahead, although the shooter's model is still beside the row or still
        /// turned away.
        /// </summary>
        [UnityTest, Order(1), Timeout(10 * 60 * 1000)]
        public IEnumerator SniperRifle_KillsAlongTheRowOfCells()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return JoinFourPlayersAndOpenHub();
            var characters = PlayerCharacters;
            yield return StartTraining(k_ShootingMode);
            yield return UntilTheGameIsOn();
            Time.timeScale = 1f;

            var robots = Robots().ToList();
            LogHitboxes(robots);
            var (shooter, target) = (robots[0], robots[1]);
            var movement = Get(shooter, "MovementController");
            var rifle = ((Component)Get(shooter, "SpecialAction")).GetComponent(GameType(k_SniperBulletShooter));
            yield return m_driver.WaitUntil("both robots to be able to act", () => CanAct(shooter) && CanAct(target), 20f);
            var row = FindShootingRow(shooter, target);

            m_driver.Step("Game: a robot steps aside onto a row and shoots along it at once");
            yield return ShootUntilAKill(shooter, row.Aside, row.Direction, target, row.Target, () =>
            {
                CallWith(movement, "MoveToCell", row.Start);
                CallWith(rifle, "Shoot");
            });

            m_driver.Step("Game: a robot turns towards a row and shoots along it at once");
            yield return ShootUntilAKill(shooter, row.Start, row.Across, target, row.Target, () =>
            {
                CallWith(movement, "ChangeDirection", Direction(row.Direction));
                CallWith(rifle, "Shoot");
            });

            yield return FinishTheGame(characters);
        }

        /// <summary>
        /// A robot stepping onto ice slides on to the next cell when it is free, and otherwise stops on the ice: the
        /// next cell taken before the step, taken during the slide, or past two ice cells in a row. Each time the robot
        /// ends on that cell on the grid and on the screen, and can move again.
        /// </summary>
        [UnityTest, Order(1), Timeout(10 * 60 * 1000)]
        public IEnumerator Ice_SlidesOnToAFreeCellElseStopsOnTheIce()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return JoinFourPlayersAndOpenHub();
            var characters = PlayerCharacters;
            yield return StartTraining(k_ShootingMode);
            yield return UntilTheGameIsOn();
            Time.timeScale = 1f;

            var robots = Robots().ToList();
            var (slider, blocker) = (robots[0], robots[1]);
            var (before, ice, after) = FindIceCrossing(slider, blocker, secondIce: false);
            var (rowStart, firstIce, secondIce) = FindIceCrossing(slider, blocker, secondIce: true);
            var beyond = Get(secondIce, NeighborTowards(firstIce, secondIce)) as Component;
            var parking = Cells().First(cell => IsFreeAndSafe(cell, slider, blocker)
                                                && !new[] { before, after, rowStart, beyond }.Contains(cell));

            m_driver.Step("Game: a robot steps onto ice, the next cell free: it slides on to it");
            yield return StepOntoIce(slider, before, ice, blocker, parking, null, 0f);
            AssertStandsOn(slider, after, "Onto ice, the next cell free");

            m_driver.Step("Game: a robot steps onto ice, the next cell taken: it stops on the ice");
            yield return StepOntoIce(slider, before, ice, blocker, parking, after, 0f);
            AssertStandsOn(slider, ice, "Onto ice, the next cell taken");

            m_driver.Step("Game: a robot steps onto ice, the next cell taken during the slide: it comes back onto the ice");
            yield return StepOntoIce(slider, before, ice, blocker, parking, after, 0.3f);
            AssertStandsOn(slider, ice, "Onto ice, the next cell taken during the slide");

            m_driver.Step("Game: a robot steps onto two ice cells in a row, the next cell taken: it stops on the second");
            yield return StepOntoIce(slider, rowStart, firstIce, blocker, parking, beyond, 0f);
            AssertStandsOn(slider, secondIce, "Onto two ice cells, the next cell taken");

            m_driver.Step("Game: a robot steps onto two ice cells in a row, the second one taken: it stops on the first");
            yield return StepOntoIce(slider, rowStart, firstIce, blocker, parking, secondIce, 0f);
            AssertStandsOn(slider, firstIce, "Onto two ice cells, the second taken");

            m_driver.Step("Game: a robot dies during its slide, another steps onto the ice from the side: it slides its own way");
            var across = s_gridDirections.First(direction => direction.Axis != AxisOf(ice, after) && IsFreeAndSafe(Get(ice, direction.Behind) as Component, slider, blocker));
            var side = (Component)Get(ice, across.Behind);
            yield return StepOntoIce(slider, before, ice, blocker, side, null, 0f);
            // StepOntoIce waited for the end of the slide: start this one again, and kill the robot mid-slide.
            CallWith(Get(slider, "MovementController"), "DistachFromCurrentCell");
            CallWith(Get(slider, "MovementController"), "TeleportToCell", before);
            yield return RealSeconds(0.3f);
            CallWith(Get(slider, "MovementController"), "MoveToCell", ice);
            yield return RealSeconds(0.3f);
            var sliderDeath = Get(slider, "DeathController");
            CallWith(sliderDeath, "Kill", 30f, true);
            Assert.IsFalse(IsAliveRobot(slider), "The robot didn't die.");
            var end = SlideEnd(ice, across.Ahead, blocker);
            CallWith(Get(blocker, "MovementController"), "MoveToCell", ice);
            yield return RealSeconds(2f);
            AssertStandsOn(blocker, end, "Onto ice from the side, as a robot dies sliding off it");
            Assert.IsFalse(IsAlive(Get(Get(slider, "GridWalker"), "CurrentCell")), "The dead robot's slide put it back on the grid.");
            CallWith(sliderDeath, "Resurrect");
            yield return RealSeconds(0.5f);
            var respawnCell = Get(Get(slider, "GridWalker"), "CurrentCell") as Component;
            Assert.IsTrue(respawnCell != null, "The robot came back off the grid.");
            AssertStandsOn(slider, respawnCell, "Back after dying on the ice");

            yield return FinishTheGame(characters);
        }

        // 0 along z (forward, backward), 1 along x (right, left).
        private static int AxisOf(Component from, Component to) =>
            s_gridDirections.First(direction => ReferenceEquals(Get(from, direction.Ahead), to)).Axis;

        /// <summary>Where a robot stepping onto <paramref name="ice"/> towards <paramref name="ahead"/> stops: the first free cell past the ice, else the last ice.</summary>
        private static Component SlideEnd(Component ice, string ahead, object robot)
        {
            var frozen = GameType("Game.Gameplay.Cells.FrozenCell.FrozenCellEffectHandler");
            var cell = ice;
            while (true)
            {
                var next = Get(cell, ahead) as Component;
                if (next == null || !IsFree(next, robot))
                    return cell;
                if (next.GetComponent(frozen) == null)
                    return next;
                cell = next;
            }
        }

        /// <summary>
        /// The slider steps from <paramref name="start"/> onto <paramref name="ice"/>, with the blocker on
        /// <paramref name="taken"/> (else on <paramref name="parking"/>), from the start or <paramref name="takenAfter"/>
        /// seconds later; then the moves and slides end.
        /// </summary>
        private IEnumerator StepOntoIce(object slider, Component start, Component ice, object blocker, Component parking, Component taken, float takenAfter)
        {
            yield return m_driver.WaitUntil("both robots to be alive and free to move",
                () => IsAliveRobot(slider) && IsAliveRobot(blocker) && !IsBlockedRobot(slider) && !IsBlockedRobot(blocker), 20f);
            var sliderMovement = Get(slider, "MovementController");
            var blockerMovement = Get(blocker, "MovementController");
            CallWith(sliderMovement, "DistachFromCurrentCell");
            CallWith(blockerMovement, "DistachFromCurrentCell");
            CallWith(sliderMovement, "TeleportToCell", start);
            CallWith(blockerMovement, "TeleportToCell", taken != null && takenAfter <= 0f ? taken : parking);
            yield return RealSeconds(0.3f);

            CallWith(sliderMovement, "MoveToCell", ice);
            if (taken != null && takenAfter > 0f)
            {
                // Mid-slide: the jump onto the ice is over, the slide on to the next cell isn't.
                yield return RealSeconds(takenAfter);
                CallWith(blockerMovement, "DistachFromCurrentCell");
                CallWith(blockerMovement, "TeleportToCell", taken);
            }
            yield return RealSeconds(1.5f);
        }

        private static void AssertStandsOn(object robot, Component cell, string situation)
        {
            var current = Get(Get(robot, "GridWalker"), "CurrentCell") as Component;
            Assert.IsTrue(ReferenceEquals(cell, current),
                $"{situation}: the robot is on the cell at {(current ? current.transform.position : Vector3.zero)} instead of the one at {cell.transform.position}.");
            var drawn = ((Transform)Get(robot, "Root")).position;
            var gap = Vector2.Distance(new Vector2(drawn.x, drawn.z), new Vector2(cell.transform.position.x, cell.transform.position.z));
            Assert.Less(gap, 0.05f, $"{situation}: the robot is drawn at {drawn}, {gap:0.00} away from its cell at {cell.transform.position}.");
            Assert.IsFalse(IsBlockedRobot(robot), $"{situation}: the robot can't move any more.");
        }

        /// <summary>
        /// Ice between two free cells without an effect, in a line (<paramref name="secondIce"/>: false); or a free
        /// cell, then two ice cells in a line followed by a free cell (true): the start, the first ice, the second.
        /// </summary>
        private static (Component Start, Component Ice, Component Next) FindIceCrossing(object slider, object blocker, bool secondIce)
        {
            var frozen = GameType("Game.Gameplay.Cells.FrozenCell.FrozenCellEffectHandler");
            bool IsIce(Component cell) => cell != null && cell.GetComponent(frozen) != null && IsFree(cell, slider, blocker);
            foreach (var ice in Cells().Where(IsIce))
            {
                foreach (var direction in s_gridDirections)
                {
                    var start = Get(ice, direction.Behind) as Component;
                    var next = Get(ice, direction.Ahead) as Component;
                    if (!IsFreeAndSafe(start, slider, blocker))
                        continue;
                    if (!secondIce && IsFreeAndSafe(next, slider, blocker))
                        return (start, ice, next);
                    if (secondIce && IsIce(next) && IsFreeAndSafe(Get(next, direction.Ahead) as Component, slider, blocker))
                        return (start, ice, next);
                }
            }
            Assert.Fail(secondIce ? "No two ice cells in a row between free cells in the arena." : "No ice between two free cells in the arena.");
            return default;
        }

        // The neighbor property of `to` that continues the line from `from` to `to`.
        private static string NeighborTowards(Component from, Component to) =>
            s_gridDirections.First(direction => ReferenceEquals(Get(from, direction.Ahead), to)).Ahead;

        private static bool IsBlockedRobot(object robot) => (bool)Get(Get(robot, "MovementController"), "IsBlocked");

        /// <summary>Through the tutorial, until the game is on.</summary>
        private IEnumerator UntilTheGameIsOn()
        {
            m_driver.Step("Game: loading, then everyone presses Ready");
            yield return m_driver.WaitUntil("the game to load", () => GameplayState is "TutorialState" or "GameplayState", 180f);
            if (GameplayState == "TutorialState")
            {
                yield return Frames(5);
                foreach (var pad in m_driver.Gamepads)
                    yield return m_driver.Press(pad.buttonSouth);
            }
            yield return m_driver.WaitUntil("the game", () => GameplayState == "GameplayState", 60f);
        }

        private IEnumerator FinishTheGame(List<Object> characters)
        {
            Time.timeScale = k_TimeScale;
            m_driver.Step("Game: until the results");
            yield return m_driver.MashButtonsUntil(() => GameplayState is "ResultsState" or "EndState" or null, new System.Random(3), 300f);
            while (GameplayState == "ResultsState")
            {
                yield return m_driver.Submit();
                yield return RealSeconds(0.5f);
            }
            yield return BackToCharacterSelectionWith(characters);
            m_driver.AssertNoErrors();
        }

        /// <summary>
        /// Places the shooter facing <paramref name="facing"/> and the target, then <paramref name="shoot"/>s until the
        /// target dies. Another attempt when an announcement made the target invincible meanwhile.
        /// </summary>
        private IEnumerator ShootUntilAKill(object shooter, Component shooterCell, string facing, object target, Component targetCell, Action shoot)
        {
            for (int attempt = 1; attempt <= k_ShotAttempts; attempt++)
            {
                // On their feet, not invincible (a shield stops a shot, the shooter's own too), the rifle reloaded.
                yield return m_driver.WaitUntil("both robots to be able to act, without a shield",
                    () => CanAct(shooter) && CanAct(target) && !IsInvincible(shooter) && !IsInvincible(target), 30f);
                CallWith(Get(shooter, "MovementController"), "DistachFromCurrentCell");
                CallWith(Get(target, "MovementController"), "DistachFromCurrentCell");
                CallWith(Get(shooter, "MovementController"), "TeleportToCell", shooterCell);
                CallWith(Get(target, "MovementController"), "TeleportToCell", targetCell);
                CallWith(Get(shooter, "MovementController"), "ChangeDirection", Direction(facing));
                // The model turns in 0.2s, the rifle shoots once a second.
                yield return RealSeconds(1.2f);
                if (!CanAct(shooter) || !CanAct(target) || IsInvincible(shooter) || IsInvincible(target))
                {
                    Debug.Log($"[Scenario] An announcement came before the shot (attempt {attempt}): again.");
                    continue;
                }

                shoot();
                var shielded = false;
                var start = Time.realtimeSinceStartup;
                while (IsAliveRobot(target) && Time.realtimeSinceStartup - start < 1.5f)
                {
                    shielded |= IsInvincible(target);
                    yield return null;
                }
                if (!IsAliveRobot(target))
                    yield break;
                if (!shielded && GameplayState == "GameplayState")
                    m_driver.Fail("The shot missed the robot three cells ahead of the shooter, on its row.");
                Debug.Log($"[Scenario] The target got a shield during the shot (attempt {attempt}): again.");
            }
            m_driver.Fail($"No shot without an announcement in {k_ShotAttempts} attempts.");
        }

        /// <summary>Water or lava between two free cells without an effect, in a line.</summary>
        private static (Component Before, Component Hazard, Component After) FindHazardCrossing(object robot)
        {
            // Across the water or lava onto a safe cell, else onto more of it: the robot must stay on the first cell.
            var hazards = Cells().Where(cell => cell.GetComponent(GameType(k_KillingCell)) != null && IsFree(cell, robot)).ToList();
            foreach (var afterIsSafe in new[] { true, false })
            {
                foreach (var hazard in hazards)
                {
                    foreach (var direction in s_gridDirections)
                    {
                        var before = Get(hazard, direction.Behind) as Component;
                        var after = Get(hazard, direction.Ahead) as Component;
                        if (IsFreeAndSafe(before, robot) && after != null && (afterIsSafe ? IsFreeAndSafe(after, robot) : IsFree(after, robot)))
                            return (before, hazard, after);
                    }
                }
            }
            Assert.Fail($"No water or lava next to a free cell, with a free cell beyond, among the {hazards.Count} of the arena.");
            return default;
        }

        /// <summary>
        /// Four free cells without an effect in a row, from <c>Start</c> to <c>Target</c>, and the cell <c>Aside</c>
        /// next to <c>Start</c>, from which a step <c>Across</c> leads onto it.
        /// </summary>
        private static (Component Start, Component Target, Component Aside, string Direction, string Across) FindShootingRow(object shooter, object target)
        {
            foreach (var start in Cells().Where(cell => IsFreeAndSafe(cell, shooter, target)))
            {
                foreach (var direction in s_gridDirections)
                {
                    var row = new List<Component> { start };
                    while (row.Count < 4 && Get(row[row.Count - 1], direction.Ahead) is Component next && IsFreeAndSafe(next, shooter, target))
                        row.Add(next);
                    if (row.Count < 4)
                        continue;
                    foreach (var across in s_gridDirections.Where(other => other.Axis != direction.Axis))
                    {
                        if (Get(start, across.Behind) is Component aside && IsFreeAndSafe(aside, shooter, target))
                            return (start, row[3], aside, direction.Direction, across.Direction);
                    }
                }
            }
            Assert.Fail("No row of four free cells with a free cell beside it in the arena.");
            return default;
        }

        // In the same order every run: row by row.
        private static IEnumerable<Component> Cells() =>
            Object.FindObjectsByType(GameType(k_Cell), FindObjectsInactive.Exclude).Cast<Component>()
                .OrderBy(cell => cell.transform.position.z).ThenBy(cell => cell.transform.position.x);

        // Walkable, free but for the given robots, and without an effect (water, lava, ice, teleporter).
        private static bool IsFreeAndSafe(Component cell, params object[] robots) => cell != null && !HasEffect(cell) && IsFree(cell, robots);

        private static bool IsFree(Component cell, params object[] robots)
        {
            var walkable = cell.GetComponent(GameType(k_WalkableCell));
            if (walkable == null)
                return false;
            var occupant = Get(walkable, "MovementControllerOnCell");
            return !IsAlive(occupant) || robots.Any(robot => ReferenceEquals(Get(robot, "MovementController"), occupant));
        }

        private static bool IsAliveRobot(object robot) => (bool)Get(Get(robot, "DeathController"), "IsAlive");

        private static bool IsInvincible(object robot) => (bool)Get(Get(robot, "DeathController"), "IsInvincible");

        private static object Direction(string name) => Enum.Parse(GameType(k_Direction), name);

        private static object CallWith(object target, string method, params object[] arguments) =>
            target.GetType().GetMethod(method).Invoke(target, arguments);

        // What a shot must touch to kill: the robot's hitbox, next to what is drawn of it.
        private static void LogHitboxes(IEnumerable<object> robots)
        {
            foreach (var robot in robots)
            {
                var root = (Transform)Get(robot, "Root");
                var hitbox = root.GetComponentInChildren(GameType(k_CollisionsHandler)).GetComponent<Collider>().bounds.size;
                var renderers = ((Transform)Get(robot, "ModelSource")).GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                    continue;
                var model = renderers.Select(renderer => renderer.bounds).Aggregate((bounds, other) => { bounds.Encapsulate(other); return bounds; }).size;
                Debug.Log($"[Scenario] {((Object)Get(robot, "CharacterDataAsset")).name}: hitbox {hitbox}, model {model}.");
            }
        }
    }
}
