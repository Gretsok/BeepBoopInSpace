using System;
using System.Collections.Generic;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.GlobalGameplayData;
using Game.Gameplay.GridSystem;
using Game.Gameplay.GridSystem.GenericComponents;
using UnityEngine;
using Random = System.Random;

namespace Game.Gameplay.CharactersManagement.Death
{
    [RequireComponent(typeof(DeathController))]
    public class PlacementOnResurrectionHandler : MonoBehaviour
    {
        private DeathController m_deathController;
        private GridBuilder m_gridBuilder;
        private GlobalGameplayDataManager m_globalGameplayDataManager;
        private void Awake()
        {
            m_deathController = GetComponent<DeathController>();

            m_deathController.OnResurrection += HandleResurrection;
            
            GridBuilder.RegisterPostInitializationCallback(builder => m_gridBuilder = builder);
            GlobalGameplayDataManager.RegisterPostInitializationCallback(manager => m_globalGameplayDataManager = manager);
        }

        private void HandleResurrection(DeathController obj)
        {
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
