using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.Gameplay.GridSystem.GenericComponents;
using Game.Online.Gameplay;
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
                GameplaySync.NotifyBump(this);
                PlayBump();
                return;
            }

            // Told before the move happens: what the move causes (a teleporter...) comes after it.
            GameplaySync.NotifyMove(this, cell);
            PerformMove(cell);
        }

        /// <summary>Online guest: the host's robot moved to the cell.</summary>
        public void ReplicateMove(Cell cell)
        {
            PerformMove(cell);
        }

        /// <summary>Online guest: the host's robot couldn't move.</summary>
        public void ReplicateBump()
        {
            PlayBump();
        }

        private void PlayBump()
        {
            m_referencesHolder.AnimationsHandler.Move();
            m_referencesHolder.Root.transform.DOJump(m_targetPosition, 0.5f, 1, 0.2f);
            m_referencesHolder.RumbleHandler.PlayMoveRumble();
            m_referencesHolder.SFXsHandler.PlayCannotMoveAudio();
        }

        private void PerformMove(Cell cell)
        {
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

        public void DistachFromCurrentCell()
        {
            if (m_referencesHolder.GridWalker.CurrentCell != null)
            {
                GameplaySync.NotifyDetach(this);
                m_referencesHolder.GridWalker.MoveToCell(null, this);
            }
        }

        public void TeleportToCell(Cell cell)
        {
            if (IsBlocked)
                return;

            if (cell == null || !cell.TryGetComponent(out CanBeWalkedOnCellComponent comp) ||
                (comp.MovementControllerOnCell && comp.MovementControllerOnCell != this))
            {
                Debug.LogError($"Cannot teleport to cell.");
                return;
            }

            GameplaySync.NotifyTeleport(this, cell);
            PerformTeleport(cell);
        }

        /// <summary>Online guest: the host's robot was teleported to the cell.</summary>
        public void ReplicateTeleport(Cell cell)
        {
            PerformTeleport(cell);
        }

        /// <summary>Online guest: the host's robot slides on ice towards the cell, which it reaches by a teleport.</summary>
        public void ReplicateSlide(Cell cell)
        {
            m_referencesHolder.Root.DOKill();
            m_referencesHolder.Root.DOMove(cell.transform.position, 0.2f).SetEase(Ease.OutCubic);
        }

        private void PerformTeleport(Cell cell)
        {
            m_referencesHolder.Root.transform.DOKill();
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

            GameplaySync.NotifyDirection(this, direction, true);
            PerformChangeDirection(direction);
        }

        /// <summary>Online guest: the host's robot turned (<paramref name="turn"/>, advanced controls) or faced a direction.</summary>
        public void ReplicateDirection(EDirection direction, bool turn)
        {
            if (turn)
                PerformChangeDirection(direction);
            else
                Face(direction);
        }

        private void PerformChangeDirection(EDirection direction)
        {
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

        // Simplified controls, relative to the screen: the gameplay camera looks along Z.
        public void GoUp()
        {
            GoTowards(EDirection.Z);
        }

        public void GoDown()
        {
            GoTowards(EDirection.MinusZ);
        }

        public void GoRight()
        {
            GoTowards(EDirection.X);
        }

        public void GoLeft()
        {
            GoTowards(EDirection.MinusX);
        }

        private void GoTowards(EDirection direction)
        {
            if (IsBlocked)
                return;

            if (direction != CurrentDirection)
            {
                GameplaySync.NotifyDirection(this, direction, false);
                Face(direction);
            }

            WalkForward();
        }

        private void Face(EDirection direction)
        {
            m_referencesHolder.Root.transform.DORotateQuaternion(Quaternion.LookRotation(GetWorldDirectionFrom(direction), Vector3.up), 0.2f);
            CurrentDirection = direction;
        }
    }
}
