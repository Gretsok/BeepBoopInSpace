using System.Collections;
using DG.Tweening;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement.Movement;
using Game.Gameplay.GridSystem.GenericComponents;
using Game.Online;
using Game.Online.Gameplay;
using UnityEngine;

namespace Game.Gameplay.Cells.FrozenCell
{
    /// <summary>
    /// Ice: a robot stepping onto it slides on to the next cell in its direction when that cell is free, and stops on
    /// the ice otherwise. It can't move during its slide, and holds the ice on the grid until it reaches the next cell.
    /// </summary>
    public class FrozenCellEffectHandler : MonoBehaviour, ICharacterMovementControllerBlocker
    {
        private const float k_SlideDuration = 0.2f;

        private CanBeWalkedOnCellComponent m_component;
        private Cell m_cell;

        // The robot sliding off this cell, and its slide. Each slide keeps its own robot: a robot that leaves the ice
        // during its slide (dead) must not hand its end over to the next robot stepping onto the ice.
        private CharacterMovementController m_slidingController;
        private Coroutine m_slide;
        private Tween m_slideTween;

        private void Start()
        {
            m_component = GetComponent<CanBeWalkedOnCellComponent>();
            m_cell = GetComponent<Cell>();
            m_component.OnMovementControllerOnCellChanged += HandleMovementControllerOnCellChanged;
        }

        private void OnDestroy()
        {
            StopSlide();
        }

        private void HandleMovementControllerOnCellChanged(CanBeWalkedOnCellComponent obj)
        {
            // Online guest: the host makes the robots slide (GameplaySync.NotifySlide, then a teleport).
            if (!Netplay.HasAuthority)
                return;
            var controller = obj.MovementControllerOnCell;
            if (controller == m_slidingController)
                return;
            // Off the ice before the end of its slide (dead, or placed for a new round): its slide stops there.
            StopSlide();
            if (!controller)
                return;

            // Only a step from a neighbor slides on, in the same direction (not a teleport or a respawn), and only
            // onto a free cell: otherwise the robot stops on the ice.
            var next = NextCellAfter(controller.ReferencesHolder.GridWalker.PreviousCell);
            if (!IsFree(next, null))
                return;

            m_slidingController = controller;
            controller.RegisterBlocker(this);
            m_slide = StartCoroutine(Slide(controller, next));
        }

        private Cell NextCellAfter(Cell previousCell)
        {
            if (!previousCell)
                return null;
            if (previousCell == m_cell.ForwardCell)
                return m_cell.BackwardCell;
            if (previousCell == m_cell.BackwardCell)
                return m_cell.ForwardCell;
            if (previousCell == m_cell.RightCell)
                return m_cell.LeftCell;
            if (previousCell == m_cell.LeftCell)
                return m_cell.RightCell;
            return null;
        }

        // Walkable and without another robot.
        private static bool IsFree(Cell cell, CharacterMovementController controller)
        {
            return cell && cell.TryGetComponent(out CanBeWalkedOnCellComponent walkable)
                        && (!walkable.MovementControllerOnCell || walkable.MovementControllerOnCell == controller);
        }

        private IEnumerator Slide(CharacterMovementController controller, Cell next)
        {
            var root = controller.ReferencesHolder.Root;

            // From a cell that isn't ice, the robot first lands on the ice.
            var previousCell = controller.ReferencesHolder.GridWalker.PreviousCell;
            if (!previousCell.GetComponent<FrozenCellEffectHandler>())
            {
                root.DOKill(false);
                m_slideTween = root.DOJump(transform.position, 0.5f, 1, k_SlideDuration);
                yield return m_slideTween.WaitForCompletion();
            }

            GameplaySync.NotifySlide(controller, next);
            m_slideTween = root.DOMove(next.transform.position, k_SlideDuration)
                .SetEase(next.GetComponent<FrozenCellEffectHandler>() ? Ease.Linear : Ease.OutCubic);
            yield return m_slideTween.WaitForCompletion();

            // Another robot stepped onto the next cell during the slide: back onto the ice, which the robot still holds.
            if (!IsFree(next, controller))
            {
                GameplaySync.NotifySlide(controller, m_cell);
                m_slideTween = root.DOMove(transform.position, k_SlideDuration).SetEase(Ease.OutCubic);
                yield return m_slideTween.WaitForCompletion();
                EndSlide();
                yield break;
            }

            // Over before the teleport: leaving the ice must not stop the slide, and the next ice starts its own.
            EndSlide();
            controller.TeleportToCell(next);
        }

        private void StopSlide()
        {
            if (m_slide != null)
                StopCoroutine(m_slide);
            if (m_slideTween != null && m_slideTween.IsActive())
                m_slideTween.Kill();
            EndSlide();
        }

        private void EndSlide()
        {
            m_slide = null;
            m_slideTween = null;
            if (m_slidingController)
                m_slidingController.UnregisterBlocker(this);
            m_slidingController = null;
        }
    }
}
