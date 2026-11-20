using System;
using System.Collections.Generic;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.Cells.FrozenCell;
using Game.Gameplay.Cells.RandomTeleportationCell;
using Game.Gameplay.Cells.ToEndPointTeleportationCell;
using Game.Gameplay.CharactersManagement.Actions;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem.SniperRifle;
using Game.Gameplay.GameModes.GameplayModifiers.Meteorites;
using Game.Gameplay.GameModes.ObjectiveManagement.PointsRushObjectiveManagement;
using Game.Gameplay.GridSystem.GenericComponents;
using UnityEngine;
using Random = UnityEngine.Random;
using EDirection = Game.Gameplay.CharactersManagement.Movement.CharacterMovementController.EDirection;

namespace Game.Gameplay.CharactersManagement.Bots
{
    /// <summary>
    /// Plays a robot for the computer, in local games only. It decides at a human pace and gets confused when the
    /// buttons shuffle: a perfect robot would be no fun against players who look for their buttons.
    /// </summary>
    public class BotController : MonoBehaviour
    {
        private const float k_MinActionDelay = 0.45f;
        private const float k_MaxActionDelay = 0.95f;
        // After the buttons shuffle, players look for theirs: the robot slows down and makes more mistakes.
        private const float k_ConfusionDuration = 4f;
        private const float k_ConfusionSlowdown = 1.6f;
        private const float k_ConfusedMistakeChance = 0.4f;
        private const float k_MistakeChance = 0.1f;
        private const float k_MeteoriteNoticeChance = 0.8f;
        private const float k_DodgeChance = 0.25f;
        private const float k_FireChance = 0.4f;
        private const float k_StuckDurationBeforeBomb = 4f;

        private static readonly EDirection[] s_directions = { EDirection.Z, EDirection.X, EDirection.MinusZ, EDirection.MinusX };

        private CharacterPlayerController m_controller;
        private CharacterReferencesHolder m_references;
        private ECharacterControlMode m_controlMode;
        private SniperBulletShooter m_shooter;
        private PointsRushObjectiveManager m_pointsRush;
        private MeteoritesModifier m_meteorites;
        private bool m_isSetUp;

        private float m_pace;
        private float m_nextActionDelay;
        private float m_confusionLeft;
        private float m_stuckDuration;
        private CharacterActionsController.ActionKeysConfiguration m_lastConfiguration;

        private readonly Queue<(Cell cell, EDirection firstStep)> m_cellsToVisit = new();
        private readonly HashSet<Cell> m_visitedCells = new();

        private Cell CurrentCell => m_references.GridWalker.CurrentCell;
        private bool CanUseSpecialAction => m_controlMode != ECharacterControlMode.Simplified;

        public void SetUp(CharacterPlayerController controller)
        {
            m_controller = controller;
            // Each robot has its own pace.
            m_pace = Random.Range(0.85f, 1.2f);
        }

        private void Update()
        {
            if (!m_controller.IsActive)
                return;
            if (!m_isSetUp)
                SetUpFromTheGame();

            var configuration = m_references.ActionsController.KeysConfiguration;
            if (configuration != m_lastConfiguration)
            {
                // The first configuration of a game is a shuffle too: players discover their buttons.
                m_lastConfiguration = configuration;
                m_confusionLeft = k_ConfusionDuration;
            }
            m_confusionLeft -= Time.deltaTime;

            m_nextActionDelay -= Time.deltaTime;
            if (m_nextActionDelay > 0f)
                return;
            var isConfused = m_confusionLeft > 0f;
            m_nextActionDelay = Random.Range(k_MinActionDelay, k_MaxActionDelay) * m_pace * (isConfused ? k_ConfusionSlowdown : 1f);

            if (!m_references.DeathController.IsAlive || m_references.MovementController.IsBlocked || !CurrentCell)
                return;
            if (Random.value < (isConfused ? k_ConfusedMistakeChance : k_MistakeChance))
                MakeAMistake();
            else
                Decide();
        }

        private void SetUpFromTheGame()
        {
            m_isSetUp = true;
            m_references = m_controller.Pawn.ReferencesHolder;
            var gameplayContext = GameplayContext.Instance;
            m_controlMode = gameplayContext.ControlMode;
            m_pointsRush = gameplayContext.ObjectiveManager as PointsRushObjectiveManager;
            m_meteorites = gameplayContext.GetGameplayModifier<MeteoritesModifier>();
            m_shooter = m_references.SpecialAction.GetComponent<SniperBulletShooter>();
        }

        private void Decide()
        {
            if (FleeTheMeteorite() || DodgeABullet())
                return;
            if (m_pointsRush)
                GoToThePoint();
            else if (m_shooter && CanUseSpecialAction)
                HuntARobot();
            else
                Wander();
        }

        // Any button, as a player who presses the wrong one: except the bomb, which players learn not to press.
        private void MakeAMistake()
        {
            var movement = m_references.MovementController;
            if (m_controlMode == ECharacterControlMode.Advanced)
            {
                switch (Random.Range(0, 5))
                {
                    case 0: movement.WalkForward(); break;
                    case 1: movement.WalkLeft(); break;
                    case 2: movement.WalkRight(); break;
                    case 3: movement.TurnLeft(); break;
                    default: movement.TurnRight(); break;
                }
                return;
            }
            GoTowards(s_directions[Random.Range(0, s_directions.Length)]);
        }

        #region Dangers
        private bool IsTargetedByAMeteorite(Cell cell)
        {
            return m_meteorites && m_meteorites.IsCellTargeted(cell);
        }

        private bool FleeTheMeteorite()
        {
            if (!IsTargetedByAMeteorite(CurrentCell) || Random.value > k_MeteoriteNoticeChance)
                return false;
            // Cells under a meteorite aren't walkable for the search: the first cell it reaches is safe.
            if (TryFindFirstStep(_ => true, true, out var step))
                GoTowards(step);
            return true;
        }

        // A robot facing this one with a clear line, for the modes with a rifle.
        private bool DodgeABullet()
        {
            if (!m_shooter || Random.value > k_DodgeChance)
                return false;
            foreach (var direction in s_directions)
            {
                var robot = FirstRobotInLine(CurrentCell, direction);
                if (!robot || !IsThreat(robot) || robot.MovementController.CurrentDirection != OppositeOf(direction))
                    continue;
                // Out of the line: to a side of it.
                var side = Random.value < 0.5f ? LeftOf(direction) : RightOf(direction);
                if (IsSafeToWalkOn(NeighborOf(CurrentCell, side), true))
                    GoTowards(side);
                else if (IsSafeToWalkOn(NeighborOf(CurrentCell, OppositeOf(side)), true))
                    GoTowards(OppositeOf(side));
                else
                    continue;
                return true;
            }
            return false;
        }

        private static bool IsThreat(CharacterReferencesHolder robot)
        {
            return robot.DeathController.IsAlive;
        }

        private bool IsSafeToWalkOn(Cell cell, bool allowTrickyCells)
        {
            if (!cell || !cell.TryGetComponent(out CanBeWalkedOnCellComponent walkable) || walkable.MovementControllerOnCell)
                return false;
            if (cell.GetComponent<KillingCellComponent>() || IsTargetedByAMeteorite(cell))
                return false;
            // An ice sliding who knows where, a teleporter that spoils the path: only when there's no other way.
            return allowTrickyCells || !(cell.GetComponent<FrozenCellEffectHandler>() || cell.GetComponent<RandomTeleporter>()
                                         || cell.GetComponent<ToEndPointTeleporter>());
        }
        #endregion

        #region Objectives
        private void GoToThePoint()
        {
            var objective = m_pointsRush.CurrentObjectiveCell;
            if (!objective)
            {
                Wander();
                return;
            }
            if (TryFindFirstStep(cell => cell == objective, false, out var step)
                || TryFindFirstStep(cell => cell == objective, true, out step))
            {
                m_stuckDuration = 0f;
                GoTowards(step);
                return;
            }

            // Walled in by the other robots: the bomb brings it back elsewhere.
            m_stuckDuration += k_MaxActionDelay;
            if (m_stuckDuration < k_StuckDurationBeforeBomb || !CanUseSpecialAction)
                return;
            m_stuckDuration = 0f;
            m_references.SpecialAction.PerformAction();
        }

        private void HuntARobot()
        {
            var facing = m_references.MovementController.CurrentDirection;
            if (IsTarget(FirstRobotInLine(CurrentCell, facing)))
            {
                // Aiming takes a moment.
                if (Random.value < k_FireChance)
                    m_references.SpecialAction.PerformAction();
                return;
            }
            foreach (var direction in s_directions)
            {
                if (!IsTarget(FirstRobotInLine(CurrentCell, direction)))
                    continue;
                Face(direction);
                return;
            }
            if (TryFindFirstStep(HasATargetInLine, false, out var step))
                GoTowards(step);
            else
                Wander();
        }

        private bool HasATargetInLine(Cell cell)
        {
            foreach (var direction in s_directions)
            {
                if (IsTarget(FirstRobotInLine(cell, direction)))
                    return true;
            }
            return false;
        }

        private static bool IsTarget(CharacterReferencesHolder robot)
        {
            return robot && robot.DeathController.IsAlive && !robot.DeathController.IsInvincible;
        }

        private void Wander()
        {
            var start = Random.Range(0, s_directions.Length);
            for (int i = 0; i < s_directions.Length; i++)
            {
                var direction = s_directions[(start + i) % s_directions.Length];
                if (!IsSafeToWalkOn(NeighborOf(CurrentCell, direction), false))
                    continue;
                GoTowards(direction);
                return;
            }
        }
        #endregion

        #region Grid
        /// <summary>The closest robot along a line of cells, up to the first thing that stops a bullet.</summary>
        private CharacterReferencesHolder FirstRobotInLine(Cell from, EDirection direction)
        {
            var cell = NeighborOf(from, direction);
            while (cell)
            {
                if (!cell.TryGetComponent(out CanBeWalkedOnCellComponent walkable))
                    return null;
                var robot = walkable.MovementControllerOnCell;
                if (robot && robot.ReferencesHolder != m_references)
                    return robot.ReferencesHolder;
                cell = NeighborOf(cell, direction);
            }
            return null;
        }

        /// <summary>The first step towards the closest safe cell matching the condition.</summary>
        private bool TryFindFirstStep(Func<Cell, bool> isGoal, bool allowTrickyCells, out EDirection firstStep)
        {
            m_cellsToVisit.Clear();
            m_visitedCells.Clear();
            m_visitedCells.Add(CurrentCell);

            // Not always the same way around, for two equal paths.
            var start = Random.Range(0, s_directions.Length);
            for (int i = 0; i < s_directions.Length; i++)
            {
                var direction = s_directions[(start + i) % s_directions.Length];
                var neighbor = NeighborOf(CurrentCell, direction);
                if (IsSafeToWalkOn(neighbor, allowTrickyCells) && m_visitedCells.Add(neighbor))
                    m_cellsToVisit.Enqueue((neighbor, direction));
            }

            while (m_cellsToVisit.Count > 0)
            {
                var (cell, step) = m_cellsToVisit.Dequeue();
                if (isGoal(cell))
                {
                    firstStep = step;
                    return true;
                }
                foreach (var direction in s_directions)
                {
                    var neighbor = NeighborOf(cell, direction);
                    if (IsSafeToWalkOn(neighbor, allowTrickyCells) && m_visitedCells.Add(neighbor))
                        m_cellsToVisit.Enqueue((neighbor, step));
                }
            }

            firstStep = default;
            return false;
        }

        private static Cell NeighborOf(Cell cell, EDirection direction)
        {
            return direction switch
            {
                EDirection.Z => cell.ForwardCell,
                EDirection.MinusZ => cell.BackwardCell,
                EDirection.X => cell.RightCell,
                _ => cell.LeftCell,
            };
        }

        private static EDirection LeftOf(EDirection direction)
        {
            return direction switch
            {
                EDirection.Z => EDirection.MinusX,
                EDirection.MinusX => EDirection.MinusZ,
                EDirection.MinusZ => EDirection.X,
                _ => EDirection.Z,
            };
        }

        private static EDirection RightOf(EDirection direction)
        {
            return LeftOf(OppositeOf(direction));
        }

        private static EDirection OppositeOf(EDirection direction)
        {
            return LeftOf(LeftOf(direction));
        }
        #endregion

        #region Actions
        private void GoTowards(EDirection direction)
        {
            var movement = m_references.MovementController;
            if (m_controlMode != ECharacterControlMode.Advanced)
            {
                switch (direction)
                {
                    case EDirection.Z: movement.GoUp(); break;
                    case EDirection.MinusZ: movement.GoDown(); break;
                    case EDirection.X: movement.GoRight(); break;
                    default: movement.GoLeft(); break;
                }
                return;
            }

            // The advanced controls have no way back: a turn first, then a step aside.
            var facing = movement.CurrentDirection;
            if (direction == facing)
                movement.WalkForward();
            else if (direction == LeftOf(facing))
                movement.WalkLeft();
            else if (direction == RightOf(facing))
                movement.WalkRight();
            else
                movement.TurnRight();
        }

        // The simplified controls turn only by walking that way.
        private void Face(EDirection direction)
        {
            var movement = m_references.MovementController;
            if (m_controlMode != ECharacterControlMode.Advanced)
                GoTowards(direction);
            else if (direction == LeftOf(movement.CurrentDirection))
                movement.TurnLeft();
            else
                movement.TurnRight();
        }
        #endregion
    }
}
