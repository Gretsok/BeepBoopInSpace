using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.Gameplay.GridSystem;
using Game.Gameplay.GridSystem.GenericComponents;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement.Movement
{
    public class CharacterMovementController : MonoBehaviour
    {
        private CharacterReferencesHolder m_referencesHolder;
        public CharacterReferencesHolder ReferencesHolder => m_referencesHolder;

        public void InjectDependencies(CharacterReferencesHolder referencesHolder)
        {
            m_referencesHolder = referencesHolder;
        }
        
        #region Blocking
        private List<ICharacterMovementControllerBlocker> m_blockers = new();
        public bool IsBlocked => m_blockers.Count > 0;
        public void RegisterBlocker(ICharacterMovementControllerBlocker blocker)
        {
            if (!m_blockers.Contains(blocker))
                m_blockers.Add(blocker);
        }

        public void UnregisterBlocker(ICharacterMovementControllerBlocker blocker)
        {
            m_blockers.RemoveAll(b => b == blocker);
        }
        #endregion
        
        public enum EDirection
        {
            Z,
            MinusZ,
            X,
            MinusX
        }
        
        public EDirection CurrentDirection { get; private set; } = EDirection.Z;
        

        public Action<CharacterMovementController, Cell> OnPositionUpdated;
        public Action<CharacterMovementController, Cell> OnMoveAnimationDone;
        public void MoveToCell(Cell cell)
        {
            if (IsBlocked)
                return;
            
            if (cell == null || !cell.TryGetComponent(out CanBeWalkedOnCellComponent comp) || comp.MovementControllerOnCell)
            {
                m_referencesHolder.AnimationsHandler.Move();
                m_referencesHolder.Root.transform.DOJump(m_targetPosition, 0.5f, 1, 0.2f);
                m_referencesHolder.RumbleHandler.PlayMoveRumble();
                m_referencesHolder.SFXsHandler.PlayCannotMoveAudio();

                return;
            }
            
            m_referencesHolder.GridWalker.MoveToCell(cell, this);
            
            var groundPosition = new Vector3(m_referencesHolder.GridWalker.transform.position.x, 0f, m_referencesHolder.GridWalker.transform.position.z);
            m_targetPosition = groundPosition;
            
            m_referencesHolder.Root.transform.DOJump(m_targetPosition, 0.5f, 1, 0.2f).onComplete += () => OnMoveAnimationDone?.Invoke(this, cell);
            
            m_referencesHolder.AnimationsHandler.Move();
            m_referencesHolder.SFXsHandler.PlayMoveAudio();
            m_referencesHolder.RumbleHandler.PlayMoveRumble();
            OnPositionUpdated?.Invoke(this, cell);
        }

        private Vector3 m_targetPosition;

        public void TeleportToCell(Cell cell)
        {
            if (IsBlocked)
                return;

            if (cell == null || !cell.TryGetComponent(out CanBeWalkedOnCellComponent comp) ||
                comp.MovementControllerOnCell)
            {
                Debug.LogError($"Cannot teleport to cell.");
                return;
            }
            
            m_referencesHolder.GridWalker.MoveToCell(cell, this);
            m_referencesHolder.Root.transform.position = m_referencesHolder.GridWalker.transform.position;
            m_targetPosition = transform.position;
            OnPositionUpdated?.Invoke(this, cell);
            OnMoveAnimationDone?.Invoke(this, cell);
        }

        public void ChangeDirection(EDirection direction)
        {
            if (IsBlocked)
                return;
            
            var newForward = GetWorldDirectionFrom(direction);

            m_referencesHolder.Root.transform.DOJump(m_targetPosition, 0.5f, 1, 0.2f);
            m_referencesHolder.Root.transform.DORotateQuaternion(Quaternion.LookRotation(newForward, Vector3.up), 0.2f);
            
            m_referencesHolder.RumbleHandler.PlayTurnRumble();
            m_referencesHolder.SFXsHandler.PlayTurnAudio();
            m_referencesHolder.AnimationsHandler.Squash();
            
            CurrentDirection = direction;
        }

        public Vector3 GetWorldDirection()
        {
            return GetWorldDirectionFrom(CurrentDirection);
        }
        
        public static Vector3 GetWorldDirectionFrom(EDirection direction)
        {
            Vector3 newForward;
            switch (direction)
            {
                case EDirection.Z:
                default:
                    newForward = Vector3.forward;
                    break;
                case EDirection.X:
                    newForward = Vector3.right;
                    break;
                case EDirection.MinusX:
                    newForward = Vector3.left;
                    break;
                case EDirection.MinusZ:
                    newForward = Vector3.back;
                    break;
            }

            return newForward;
        }

        public void WalkForward()
        {
            switch (CurrentDirection)
            {
                case EDirection.Z:
                {
                    MoveToCell(m_referencesHolder.GridWalker.CurrentCell.ForwardCell);
                }
                    break;
                case EDirection.MinusZ:
                {
                    MoveToCell(m_referencesHolder.GridWalker.CurrentCell.BackwardCell);
                }
                    break;
                case EDirection.X:
                {
                    MoveToCell(m_referencesHolder.GridWalker.CurrentCell.RightCell);
                }
                    break;
                case EDirection.MinusX:
                {
                    MoveToCell(m_referencesHolder.GridWalker.CurrentCell.LeftCell);
                }
                    break;
            }
        }

        public void WalkLeft()
        {
            switch (CurrentDirection)
            {
                case EDirection.Z:
                {
                    MoveToCell(m_referencesHolder.GridWalker.CurrentCell.LeftCell);
                }
                    break;
                case EDirection.MinusZ:
                {
                    MoveToCell(m_referencesHolder.GridWalker.CurrentCell.RightCell);
                }
                    break;
                case EDirection.X:
                {
                    MoveToCell(m_referencesHolder.GridWalker.CurrentCell.ForwardCell);
                }
                    break;
                case EDirection.MinusX:
                {
                    MoveToCell(m_referencesHolder.GridWalker.CurrentCell.BackwardCell);
                }
                    break;
            }
        }

        public void WalkRight()
        {
            switch (CurrentDirection)
            {
                case EDirection.Z:
                {
                    MoveToCell(m_referencesHolder.GridWalker.CurrentCell.RightCell);
                }
                    break;
                case EDirection.MinusZ:
                {
                    MoveToCell(m_referencesHolder.GridWalker.CurrentCell.LeftCell);
                }
                    break;
                case EDirection.X:
                {
                    MoveToCell(m_referencesHolder.GridWalker.CurrentCell.BackwardCell);
                }
                    break;
                case EDirection.MinusX:
                {
                    MoveToCell(m_referencesHolder.GridWalker.CurrentCell.ForwardCell);
                }
                    break;
            }
        }

        public void TurnLeft()
        {
            switch (CurrentDirection)
            {
                case EDirection.Z:
                {
                    ChangeDirection(EDirection.MinusX);
                }
                    break;
                case EDirection.MinusZ:
                {
                    ChangeDirection(EDirection.X);
                }
                    break;
                case EDirection.X:
                {
                    ChangeDirection(EDirection.Z);
                }
                    break;
                case EDirection.MinusX:
                {
                    ChangeDirection(EDirection.MinusZ);
                }
                    break;
            }
        }

        public void TurnRight()
        {
            switch (CurrentDirection)
            {
                case EDirection.Z:
                {
                    ChangeDirection(EDirection.X);
                }
                    break;
                case EDirection.MinusZ:
                {
                    ChangeDirection(EDirection.MinusX);
                }
                    break;
                case EDirection.X:
                {
                    ChangeDirection(EDirection.MinusZ);
                }
                    break;
                case EDirection.MinusX:
                {
                    ChangeDirection(EDirection.Z);
                }
                    break;
            }
        }
    }
}
