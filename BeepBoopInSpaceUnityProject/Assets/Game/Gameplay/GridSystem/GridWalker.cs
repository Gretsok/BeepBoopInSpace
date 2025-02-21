using Game.Gameplay.CharactersManagement;
using NaughtyAttributes;
using UnityEngine;

namespace Game.Gameplay.GridSystem
{
    public class GridWalker : MonoBehaviour
    {
        [field: SerializeField]
        public Cell CurrentCell { get; private set; }

        public void MoveToCell(Cell cell, CharacterPawn pawn = null)
        {
            if (CurrentCell != null)
            {
                CurrentCell.GetComponent<CanBeWalkedOnCellComponent>().PawnOnCell = null;
            }
            CurrentCell = cell;
            transform.position = cell.transform.position;
            
            if (CurrentCell != null)
            {
                CurrentCell.GetComponent<CanBeWalkedOnCellComponent>().PawnOnCell = pawn;
            }
        }

        [Button]
        public void MoveForward()
        {
            if (CurrentCell && CurrentCell.ForwardCell
                            && CurrentCell.ForwardCell.TryGetComponent(out CanBeWalkedOnCellComponent comp) && !comp.PawnOnCell)
                MoveToCell(CurrentCell.ForwardCell);
        }

        [Button]
        public void MoveBackward()
        {
            if (CurrentCell && CurrentCell.BackwardCell 
                            && CurrentCell.BackwardCell.TryGetComponent(out CanBeWalkedOnCellComponent comp) && !comp.PawnOnCell)
                MoveToCell(CurrentCell.BackwardCell);
        }

        [Button]
        public void MoveRight()
        {
            if (CurrentCell && CurrentCell.RightCell
                            && CurrentCell.RightCell.TryGetComponent(out CanBeWalkedOnCellComponent comp) && !comp.PawnOnCell)
                MoveToCell(CurrentCell.RightCell);
        }

        [Button]
        public void MoveLeft()
        {
            if (CurrentCell && CurrentCell.LeftCell
                            && CurrentCell.LeftCell.TryGetComponent(out CanBeWalkedOnCellComponent comp) && !comp.PawnOnCell)
                MoveToCell(CurrentCell.LeftCell);
        }
    }
}