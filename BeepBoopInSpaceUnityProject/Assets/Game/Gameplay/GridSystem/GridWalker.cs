using NaughtyAttributes;
using UnityEngine;

namespace Game.Gameplay.GridSystem
{
    public class GridWalker : MonoBehaviour
    {
        [field: SerializeField]
        public Cell CurrentCell { get; private set; }

        public void MoveToCell(Cell cell)
        {
            CurrentCell = cell;
            transform.position = cell.transform.position;
        }

        [Button]
        public void MoveForward()
        {
            if (CurrentCell && CurrentCell.ForwardCell
                            && CurrentCell.ForwardCell.TryGetComponent(out CanBeWalkedOnCellComponent comp))
                MoveToCell(CurrentCell.ForwardCell);
        }

        [Button]
        public void MoveBackward()
        {
            if (CurrentCell && CurrentCell.BackwardCell 
                            && CurrentCell.BackwardCell.TryGetComponent(out CanBeWalkedOnCellComponent comp))
                MoveToCell(CurrentCell.BackwardCell);
        }

        [Button]
        public void MoveRight()
        {
            if (CurrentCell && CurrentCell.RightCell
                            && CurrentCell.RightCell.TryGetComponent(out CanBeWalkedOnCellComponent comp))
                MoveToCell(CurrentCell.RightCell);
        }

        [Button]
        public void MoveLeft()
        {
            if (CurrentCell && CurrentCell.LeftCell
                            && CurrentCell.LeftCell.TryGetComponent(out CanBeWalkedOnCellComponent comp))
                MoveToCell(CurrentCell.LeftCell);
        }
    }
}