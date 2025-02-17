using System;
using UnityEngine;

namespace Game.Gameplay.GridSystem
{
    public class Cell : MonoBehaviour
    {
        private void Start()
        {
            SetForwardCell(ForwardCell);
            SetBackwardCell(BackwardCell);
            SetRightCell(RightCell);
            SetLeftCell(LeftCell);
        }

        public event Action<Cell> OnCellStateUpdated;

        public void NotifyStateChanged()
        {
            OnCellStateUpdated?.Invoke(this);
        }
        
        public event Action<Cell, Cell> OnNeighborsUpdated;
        
        [field: SerializeField]
        public Cell ForwardCell { get; private set; }

        public void SetForwardCell(Cell cell)
        {
            if (ForwardCell)
                ForwardCell.OnCellStateUpdated -= HandleForwardCellUpdated;
            ForwardCell = cell;
            if (ForwardCell)
                ForwardCell.OnCellStateUpdated += HandleForwardCellUpdated;
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public Action<Cell, Cell> OnForwardCellUpdated;
        private void HandleForwardCellUpdated(Cell obj)
        {
            OnNeighborsUpdated?.Invoke(this, obj);
            OnForwardCellUpdated?.Invoke(this, obj);
        }

        [field: SerializeField]
        public Cell BackwardCell { get; private set; }

        public void SetBackwardCell(Cell cell)
        {
            if (BackwardCell)
                BackwardCell.OnCellStateUpdated -= HandleBackwardCellUpdated;
            BackwardCell = cell;
            if (BackwardCell)
                BackwardCell.OnCellStateUpdated += HandleBackwardCellUpdated;
            
                        
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        public Action<Cell, Cell> OnBackwardCellUpdated;
        private void HandleBackwardCellUpdated(Cell obj)
        {
            OnNeighborsUpdated?.Invoke(this, obj);
            OnBackwardCellUpdated?.Invoke(this, obj);
        }
        
        [field: SerializeField]
        public Cell LeftCell { get; private set; }

        public void SetLeftCell(Cell cell)
        {
            if (LeftCell)
                LeftCell.OnCellStateUpdated -= HandleLeftCellUpdated;
            LeftCell = cell;
            if (LeftCell)
                LeftCell.OnCellStateUpdated += HandleLeftCellUpdated;
            
                        
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        public Action<Cell, Cell> OnLeftCellUpdated;
        private void HandleLeftCellUpdated(Cell obj)
        {
            OnNeighborsUpdated?.Invoke(this, obj);
            OnLeftCellUpdated?.Invoke(this, obj);
        }
        
        [field: SerializeField]
        public Cell RightCell { get; private set; }

        public void SetRightCell(Cell cell)
        {
            if (RightCell)
                RightCell.OnCellStateUpdated -= HandleRightCellUpdated;
            RightCell = cell;
            if (RightCell)
                RightCell.OnCellStateUpdated += HandleRightCellUpdated;
            
                        
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        public Action<Cell, Cell> OnRightCellUpdated;
        private void HandleRightCellUpdated(Cell obj)
        {
            OnNeighborsUpdated?.Invoke(this, obj);
            OnRightCellUpdated?.Invoke(this, obj);
        }
    }
}