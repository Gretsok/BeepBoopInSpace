using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Characters;
using Game.Gameplay.GridSystem;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement
{
    public class CharacterPawn : MonoBehaviour
    {
        [field: SerializeField] 
        public GridWalker GridWalker { get; private set; }

        [field: SerializeField]
        public Transform ModelSource { get; private set; }
        [field: SerializeField]
        public CharacterDestructionHandler DestructionHandler { get; private set; }
        [field: SerializeField]
        public CharacterVFXsHandler VFXsHandler { get; private set; }

        public CharacterData CharacterData { get; private set; }

        public void SetCharacterData(CharacterData characterData)
        {
            CharacterData = characterData;
            SetModel(CharacterData.CharacterPrefab.transform);
        }
        
        public int Score { get; private set; }

        public void IncreaseScore()
        {
            ++Score;
        }
        
        private void Start()
        {
            GridWalker.transform.SetParent(null);
            DestructionHandler.SetDependencies(ModelSource, VFXsHandler);
            
            VFXsHandler.PlaySpawnEffect();
        }

        private void OnDestroy()
        {
            if (GridWalker)
                Destroy(GridWalker.gameObject);
        }

        public void SetModel(Transform model)
        {
            while (ModelSource.childCount > 0)
            {
                var child = ModelSource.GetChild(0);
                Destroy(child.gameObject);
            }
            
            Instantiate(model, ModelSource);
        }

        public Action<CharacterPawn, Cell> OnMove;
        public void MoveToCell(Cell cell)
        {
            if (cell == null || !cell.TryGetComponent(out CanBeWalkedOnCellComponent comp) || comp.PawnOnCell)
                return;
            
            GridWalker.MoveToCell(cell, this);
            
            var groundPosition = new Vector3(GridWalker.transform.position.x, 0f, GridWalker.transform.position.z);
            m_targetPosition = groundPosition;
            
            transform.DOJump(m_targetPosition, 0.5f, 1, 0.2f);
            
            OnMove?.Invoke(this, cell);
        }

        private Vector3 m_targetPosition;

        public void TeleportToCell(Cell cell)
        {
            GridWalker.MoveToCell(cell, this);
            transform.position = GridWalker.transform.position;
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
                    newConfig.Action[i] = Explode;
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
            if (DestructionHandler.IsDestroyed)
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
                    MoveToCell(GridWalker.CurrentCell.ForwardCell);
                }
                    break;
                case EDirection.MinusZ:
                {
                    MoveToCell(GridWalker.CurrentCell.BackwardCell);
                }
                    break;
                case EDirection.X:
                {
                    MoveToCell(GridWalker.CurrentCell.RightCell);
                }
                    break;
                case EDirection.MinusX:
                {
                    MoveToCell(GridWalker.CurrentCell.LeftCell);
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
                    MoveToCell(GridWalker.CurrentCell.LeftCell);
                }
                    break;
                case EDirection.MinusZ:
                {
                    MoveToCell(GridWalker.CurrentCell.RightCell);
                }
                    break;
                case EDirection.X:
                {
                    MoveToCell(GridWalker.CurrentCell.ForwardCell);
                }
                    break;
                case EDirection.MinusX:
                {
                    MoveToCell(GridWalker.CurrentCell.BackwardCell);
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
                    MoveToCell(GridWalker.CurrentCell.RightCell);
                }
                    break;
                case EDirection.MinusZ:
                {
                    MoveToCell(GridWalker.CurrentCell.LeftCell);
                }
                    break;
                case EDirection.X:
                {
                    MoveToCell(GridWalker.CurrentCell.BackwardCell);
                }
                    break;
                case EDirection.MinusX:
                {
                    MoveToCell(GridWalker.CurrentCell.ForwardCell);
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

        private void Explode()
        {
            Debug.Log("BOOM");
            DestructionHandler.Destroy();
        }
    }
}