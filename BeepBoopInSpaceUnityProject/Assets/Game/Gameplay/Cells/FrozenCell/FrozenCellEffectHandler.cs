using System.Collections;
using DG.Tweening;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement.Movement;
using Game.Gameplay.GridSystem;
using Game.Gameplay.GridSystem.GenericComponents;
using UnityEngine;

namespace Game.Gameplay.Cells.FrozenCell
{
    public class FrozenCellEffectHandler : MonoBehaviour, ICharacterMovementControllerBlocker
    {
        private CanBeWalkedOnCellComponent m_component;
        
        private void Start()
        {
            m_component = GetComponent<CanBeWalkedOnCellComponent>();
            m_component.OnMovementControllerOnCellChanged += HandleMovementControllerOnCellChanged;
        }

        private void HandleMovementControllerOnCellChanged(CanBeWalkedOnCellComponent obj)
        {
            if (!obj.MovementControllerOnCell)
                return;

            var previousCell = obj.MovementControllerOnCell.ReferencesHolder.GridWalker.PreviousCell;
            if (!previousCell)
                return;
            
            var currentCell = obj.MovementControllerOnCell.ReferencesHolder.GridWalker.CurrentCell;
            if (!currentCell)
                return;

            var controller = obj.MovementControllerOnCell;
            
            BlockController(controller);
            
            
            if (previousCell == currentCell.ForwardCell)
                SlideTowardCell(currentCell.BackwardCell);
            if (previousCell == currentCell.BackwardCell)
                SlideTowardCell(currentCell.ForwardCell);
            if (previousCell == currentCell.RightCell)
                SlideTowardCell(currentCell.LeftCell);
            if (previousCell == currentCell.LeftCell)
                SlideTowardCell(currentCell.RightCell);
        }

        private CharacterMovementController m_blockedController;
        private void BlockController(CharacterMovementController movementController)
        {
            if (m_blockedController)
                UnblockController();
            movementController.RegisterBlocker(this);
            m_blockedController = movementController;
        }

        private void UnblockController()
        {
            if (!m_blockedController)
                return;
            
            m_blockedController.UnregisterBlocker(this);
            m_blockedController = null;
        }

        private void SlideTowardCell(Cell cell)
        {
            if (!cell.GetComponent<CanBeWalkedOnCellComponent>())
            {
                UnblockController();
                return;
            }
            
            StartCoroutine(SlidingTowardCellRoutine(cell));
        }

        private IEnumerator SlidingTowardCellRoutine(Cell cell)
        {
            var targetDestination = cell.transform.position;
            bool isTargetCellAFrozenCell = cell.GetComponent<FrozenCellEffectHandler>();
            var characterRoot = m_blockedController.ReferencesHolder.Root;
            bool isComplete = false;

            if (!m_blockedController.ReferencesHolder.GridWalker.PreviousCell.GetComponent<FrozenCellEffectHandler>())
            {
                characterRoot.DOKill(false);
                characterRoot.transform.DOJump(transform.position, 0.5f, 1, 0.2f).onComplete += () =>
                {
                    isComplete = true;
                };
                yield return new WaitUntil(() => isComplete);
            }


            isComplete = false;
            characterRoot.DOMove(targetDestination, 0.2f).SetEase(isTargetCellAFrozenCell ? Ease.Linear : Ease.OutCubic).onComplete += () => isComplete = true;
            yield return new WaitUntil(() => isComplete);
            var controller = m_blockedController;
            UnblockController();
            controller.TeleportToCell(cell);
        }
    }
}
