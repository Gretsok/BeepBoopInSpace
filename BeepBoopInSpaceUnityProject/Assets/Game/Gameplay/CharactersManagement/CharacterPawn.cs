using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Characters;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.Gameplay.CharactersManagement.Rumble;
using Game.Gameplay.GridSystem;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement
{
    public class CharacterPawn : MonoBehaviour
    {
        [field: SerializeField]
        public CharacterReferencesHolder ReferencesHolder { get; private set; }
        

        
        public int Score { get; private set; }

        public void IncreaseScore()
        {
            ++Score;
        }
        
        private void Start()
        {
            ReferencesHolder.GridWalker.transform.SetParent(null);
            ReferencesHolder.SpecialAction.InjectDependencies(ReferencesHolder);
            ReferencesHolder.DeathController.InjectDependencies(ReferencesHolder);
            
            ReferencesHolder.VFXsHandler.PlaySpawnEffect();
            
            ReferencesHolder.RumbleHandler.SetDependencies(this);
        }

        private void OnDestroy()
        {
            if (ReferencesHolder.GridWalker)
                Destroy(ReferencesHolder.GridWalker.gameObject);
        }
        

        public Action<CharacterPawn, Cell> OnMove;
        public void MoveToCell(Cell cell)
        {
            if (cell == null || !cell.TryGetComponent(out CanBeWalkedOnCellComponent comp) || comp.PawnOnCell)
            {
                ReferencesHolder.AnimationsHandler.Move();
                transform.DOJump(m_targetPosition, 0.5f, 1, 0.2f);
                ReferencesHolder.RumbleHandler.PlayMoveRumble();
                ReferencesHolder.SFXsHandler.PlayCannotMoveAudio();

                return;
            }
            
            ReferencesHolder.GridWalker.MoveToCell(cell, this);
            
            var groundPosition = new Vector3(ReferencesHolder.GridWalker.transform.position.x, 0f, ReferencesHolder.GridWalker.transform.position.z);
            m_targetPosition = groundPosition;
            
            transform.DOJump(m_targetPosition, 0.5f, 1, 0.2f);
            
            ReferencesHolder.AnimationsHandler.Move();
            ReferencesHolder.SFXsHandler.PlayMoveAudio();
            ReferencesHolder.RumbleHandler.PlayMoveRumble();
            OnMove?.Invoke(this, cell);
        }

        private Vector3 m_targetPosition;

        public void TeleportToCell(Cell cell)
        {
            ReferencesHolder.GridWalker.MoveToCell(cell, this);
            transform.position = ReferencesHolder.GridWalker.transform.position;
            m_targetPosition = transform.position;
        }

        public void ChangeDirection(EDirection direction)
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
            
            transform.DOJump(m_targetPosition, 0.5f, 1, 0.2f);
            transform.DORotateQuaternion(Quaternion.LookRotation(newForward, Vector3.up), 0.2f);
            
            ReferencesHolder.RumbleHandler.PlayTurnRumble();
            ReferencesHolder.SFXsHandler.PlayTurnAudio();
            ReferencesHolder.AnimationsHandler.Squash();
            
            CurrentDirection = direction;
        }


        public delegate void FActionKeyDelegate();

        public class ActionKeysConfiguration
        {
            public FActionKeyDelegate[] Action = new FActionKeyDelegate[8];
        }

        public ActionKeysConfiguration KeysConfiguration { get; private set; }
        public void SetConfiguration(List<int> config)
        {
            var newConfig = new ActionKeysConfiguration();
            for (int i = 0; i < 8; i++)
            {
                if (i == config[0])
                {
                    newConfig.Action[i] = WalkForward;
                }
                else if (i == config[1])
                {
                    newConfig.Action[i] = WalkLeft;
                }
                else if (i == config[2])
                {
                    newConfig.Action[i] = WalkRight;
                }
                else if (i == config[3])
                {
                    newConfig.Action[i] = TurnRight;
                }
                else if (i == config[4])
                {
                    newConfig.Action[i] = TurnLeft;
                }
                else if (i == config[5])
                {
                    newConfig.Action[i] = PlaySpecialAction;
                }
                else
                {
                    newConfig.Action[i] = null;
                }
            }
            
            
            KeysConfiguration = newConfig;
        }

        public void TryToPerformAction(int index)
        {
            if (!ReferencesHolder.DeathController.IsAlive)
                return;
            KeysConfiguration.Action[index]?.Invoke();
        }
        
        public enum EDirection
        {
            Z,
            MinusZ,
            X,
            MinusX
        }
        
        public EDirection CurrentDirection { get; private set; } = EDirection.Z;
        
        private void WalkForward()
        {
            switch (CurrentDirection)
            {
                case EDirection.Z:
                {
                    MoveToCell(ReferencesHolder.GridWalker.CurrentCell.ForwardCell);
                }
                    break;
                case EDirection.MinusZ:
                {
                    MoveToCell(ReferencesHolder.GridWalker.CurrentCell.BackwardCell);
                }
                    break;
                case EDirection.X:
                {
                    MoveToCell(ReferencesHolder.GridWalker.CurrentCell.RightCell);
                }
                    break;
                case EDirection.MinusX:
                {
                    MoveToCell(ReferencesHolder.GridWalker.CurrentCell.LeftCell);
                }
                    break;
            }
        }

        private void WalkLeft()
        {
            switch (CurrentDirection)
            {
                case EDirection.Z:
                {
                    MoveToCell(ReferencesHolder.GridWalker.CurrentCell.LeftCell);
                }
                    break;
                case EDirection.MinusZ:
                {
                    MoveToCell(ReferencesHolder.GridWalker.CurrentCell.RightCell);
                }
                    break;
                case EDirection.X:
                {
                    MoveToCell(ReferencesHolder.GridWalker.CurrentCell.ForwardCell);
                }
                    break;
                case EDirection.MinusX:
                {
                    MoveToCell(ReferencesHolder.GridWalker.CurrentCell.BackwardCell);
                }
                    break;
            }
        }

        private void WalkRight()
        {
            switch (CurrentDirection)
            {
                case EDirection.Z:
                {
                    MoveToCell(ReferencesHolder.GridWalker.CurrentCell.RightCell);
                }
                    break;
                case EDirection.MinusZ:
                {
                    MoveToCell(ReferencesHolder.GridWalker.CurrentCell.LeftCell);
                }
                    break;
                case EDirection.X:
                {
                    MoveToCell(ReferencesHolder.GridWalker.CurrentCell.BackwardCell);
                }
                    break;
                case EDirection.MinusX:
                {
                    MoveToCell(ReferencesHolder.GridWalker.CurrentCell.ForwardCell);
                }
                    break;
            }
        }

        private void TurnLeft()
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

        private void TurnRight()
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

        private void PlaySpecialAction()
        {
            ReferencesHolder.SpecialAction.PerformAction();
        }
    }
}