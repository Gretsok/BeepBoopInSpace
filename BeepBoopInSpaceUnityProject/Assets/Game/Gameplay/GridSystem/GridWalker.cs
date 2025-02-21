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
            if (CurrentCell != null)
            {
                CurrentCell.GetComponent<CanBeWalkedOnCellComponent>().IsLocked = false;
            }
            CurrentCell = cell;
            transform.position = cell.transform.position;
            
            if (CurrentCell != null)
            {
                CurrentCell.GetComponent<CanBeWalkedOnCellComponent>().IsLocked = true;
            }
        }

        [Button]
        public void MoveForward()
        {
            if (CurrentCell && CurrentCell.ForwardCell
                            && CurrentCell.ForwardCell.TryGetComponent(out CanBeWalkedOnCellComponent comp) && !comp.IsLocked)
                MoveToCell(CurrentCell.ForwardCell);
        }

        [Button]
        public void MoveBackward()
        {
            if (CurrentCell && CurrentCell.BackwardCell 
                            && CurrentCell.BackwardCell.TryGetComponent(out CanBeWalkedOnCellComponent comp) && !comp.IsLocked)
                MoveToCell(CurrentCell.BackwardCell);
        }

        [Button]
        public void MoveRight()
        {
            if (CurrentCell && CurrentCell.RightCell
                            && CurrentCell.RightCell.TryGetComponent(out CanBeWalkedOnCellComponent comp) && !comp.IsLocked)
                MoveToCell(CurrentCell.RightCell);
        }

        [Button]
        public void MoveLeft()
        {
            if (CurrentCell && CurrentCell.LeftCell
                            && CurrentCell.LeftCell.TryGetComponent(out CanBeWalkedOnCellComponent comp) && !comp.IsLocked)
                MoveToCell(CurrentCell.LeftCell);
        }
    }
}