using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.Movement;
using NaughtyAttributes;
using UnityEngine;

namespace Game.Gameplay.GridSystem
{
    public class GridWalker : MonoBehaviour
    {
        [field: SerializeField]
        public Cell CurrentCell { get; private set; }
        public Cell PreviousCell { get; private set; }

        public void MoveToCell(Cell cell, CharacterMovementController movementController = null)
        {
            PreviousCell = CurrentCell;
            if (CurrentCell != null)
            {
                CurrentCell.GetComponent<CanBeWalkedOnCellComponent>().MovementControllerOnCell = null;
            }
            CurrentCell = cell;
            transform.position = cell.transform.position;
            
            if (CurrentCell != null)
            {
                CurrentCell.GetComponent<CanBeWalkedOnCellComponent>().MovementControllerOnCell = movementController;
            }
        }

        [Button]
        public void MoveForward()
        {
            if (CurrentCell && CurrentCell.ForwardCell
                            && CurrentCell.ForwardCell.TryGetComponent(out CanBeWalkedOnCellComponent comp) && !comp.MovementControllerOnCell)
                MoveToCell(CurrentCell.ForwardCell);
        }

        [Button]
        public void MoveBackward()
        {
            if (CurrentCell && CurrentCell.BackwardCell 
                            && CurrentCell.BackwardCell.TryGetComponent(out CanBeWalkedOnCellComponent comp) && !comp.MovementControllerOnCell)
                MoveToCell(CurrentCell.BackwardCell);
        }

        [Button]
        public void MoveRight()
        {
            if (CurrentCell && CurrentCell.RightCell
                            && CurrentCell.RightCell.TryGetComponent(out CanBeWalkedOnCellComponent comp) && !comp.MovementControllerOnCell)
                MoveToCell(CurrentCell.RightCell);
        }

        [Button]
        public void MoveLeft()
        {
            if (CurrentCell && CurrentCell.LeftCell
                            && CurrentCell.LeftCell.TryGetComponent(out CanBeWalkedOnCellComponent comp) && !comp.MovementControllerOnCell)
                MoveToCell(CurrentCell.LeftCell);
        }
    }
}