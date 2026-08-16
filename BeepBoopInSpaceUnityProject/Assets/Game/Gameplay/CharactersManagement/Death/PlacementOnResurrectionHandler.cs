using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.GlobalGameplayData;
using Game.Gameplay.GridSystem;
using Game.Gameplay.GridSystem.GenericComponents;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement.Death
{
    [RequireComponent(typeof(DeathController))]
    public class PlacementOnResurrectionHandler : MonoBehaviour
    {
        private enum EReplacementTime
        {
            OnResurrection = 0,
            OnDeath = 1
        }

        [SerializeField]
        private DeathPlacementFX m_deathPlacementFXPrefab;
        [SerializeField]
        private EReplacementTime m_replacementTime = EReplacementTime.OnDeath;
        private DeathController m_deathController;
        private GridBuilder m_gridBuilder;
        private GlobalGameplayDataManager m_globalGameplayDataManager;
        private void Awake()
        {
            m_deathController = GetComponent<DeathController>();

            m_deathController.OnResurrection += HandleResurrection;
            m_deathController.OnDeath += HandleDeath;
            
            GridBuilder.RegisterPostInitializationCallback(builder => m_gridBuilder = builder);
            GlobalGameplayDataManager.RegisterPostInitializationCallback(manager => m_globalGameplayDataManager = manager);
        }

        private void HandleDeath(DeathController obj)
        {
            if (m_replacementTime != EReplacementTime.OnDeath)
                return;

            // Death feedback need death position, so we wait a frame before moving.
            UniTask.WaitForEndOfFrame().ContinueWith(Replace);
        }

        private void HandleResurrection(DeathController obj)
        {
            if (m_replacementTime != EReplacementTime.OnResurrection)
                return;
            Replace();
        }

        private void Replace()
        {
            var deathPlacementFX = Instantiate(m_deathPlacementFXPrefab, m_deathController.CharacterReferencesHolder.ModelSource.position, Quaternion.identity);
            // Source position must be gathered before teleportation.
            var sourcePosition = m_deathController.CharacterReferencesHolder.ModelSource.position;
            var dataAsset = m_globalGameplayDataManager.Data;
            // On invalid cell
            if (!CellIsValid(m_deathController.CharacterReferencesHolder.GridWalker.CurrentCell)) 
            {
                Debug.Log($"Current cell is invalid.");
                switch (dataAsset.ResurrectionPlacementOnInvalidCell)
                {
                    case GlobalGameplayDataAsset.EResurrectionPlacement.Closest:
                        TeleportToClosestCell();
                        break;
                    case GlobalGameplayDataAsset.EResurrectionPlacement.Random:
                        TeleportToRandomCell();
                        break;
                    case GlobalGameplayDataAsset.EResurrectionPlacement.Checkpoint:
                        throw new NotImplementedException("Checkpoints not implemented.");
                }
            }
            
            // On valid cell
            else 
            {
                Debug.Log($"Current cell is valid.");
                switch (dataAsset.ResurrectionPlacementOnValidCell)
                {
                    case GlobalGameplayDataAsset.EResurrectionPlacement.Closest:
                        // We stay on place
                        Debug.Log($"We stay on place.");
                        break;
                    case GlobalGameplayDataAsset.EResurrectionPlacement.Random:
                        TeleportToRandomCell();
                        break;
                    case GlobalGameplayDataAsset.EResurrectionPlacement.Checkpoint:
                        throw new NotImplementedException("Checkpoints not implemented.");
                }
            }
            deathPlacementFX.SetUp(m_deathController.CharacterReferencesHolder.CharacterDataAsset, 
                sourcePosition,
                m_deathController.CharacterReferencesHolder.GridWalker.CurrentCell.transform.position,
                m_deathController.WaitDurationToResurrect);
        }

        private void TeleportToRandomCell()
        {
            Debug.Log($"Teleport to a random cell.");
            m_deathController.CharacterReferencesHolder.MovementController.TeleportToCell(
                m_gridBuilder.GetRandomAvailableWalkableCell(cell => !cell.TryGetComponent<KillingCellComponent>(out _))
            );
        }

        /// <summary>
        /// Naive approach: Randomly teleporting to one of the valid neighbour cells.
        /// </summary>
        private void TeleportToClosestCell()
        {
            Debug.Log($"Teleport to the closest cell.");
            var currentCell = m_deathController.CharacterReferencesHolder.GridWalker.CurrentCell;
            List<Cell> validNeighbourCells = new();
            if (CellIsValid(currentCell.ForwardCell))
                validNeighbourCells.Add(currentCell.ForwardCell);
            if (CellIsValid(currentCell.BackwardCell))
                validNeighbourCells.Add(currentCell.BackwardCell);
            if (CellIsValid(currentCell.RightCell))
                validNeighbourCells.Add(currentCell.RightCell);
            if (CellIsValid(currentCell.LeftCell))
                validNeighbourCells.Add(currentCell.LeftCell);

            if (validNeighbourCells.Count == 0)
            {
                Debug.LogError($"Could not find a neighbour valid cell. Respawning at a random position.");
                TeleportToRandomCell();
                return;
            }
            
            m_deathController.CharacterReferencesHolder.MovementController.TeleportToCell(
                validNeighbourCells[UnityEngine.Random.Range(0, validNeighbourCells.Count)]
            );
        }

        public bool CellIsValid(Cell cell)
        {
            return cell && !cell.GetComponent<KillingCellComponent>() &&
                   cell.GetComponent<CanBeWalkedOnCellComponent>();
        }
    }
}
