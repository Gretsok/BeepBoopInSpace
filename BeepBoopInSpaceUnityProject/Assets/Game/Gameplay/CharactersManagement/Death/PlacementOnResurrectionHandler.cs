using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement.Death.Invincibility;
using Game.Gameplay.GlobalGameplayData;
using Game.Gameplay.GridSystem;
using Game.Gameplay.GridSystem.GenericComponents;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement.Death
{
    [RequireComponent(typeof(DeathController))]
    public class PlacementOnResurrectionHandler : MonoBehaviour, IInvincibilityGiver
    {
        [SerializeField]
        private DeathPlacementFX m_deathPlacementFXPrefab;
        [SerializeField]
        private float m_invincibilityDurationAfterResurrection = 2f;

        private DeathController m_deathController;
        private GridBuilder m_gridBuilder;
        private GlobalGameplayDataManager m_globalGameplayDataManager;
        private Cell m_respawnCell;
        private Cell m_deathCell;

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
            var deathPosition = m_deathController.CharacterReferencesHolder.ModelSource.position;
            // Detaching clears the walker's current cell, so the death cell must be read first.
            m_deathCell = m_deathController.CharacterReferencesHolder.GridWalker.CurrentCell;
            m_deathController.CharacterReferencesHolder.MovementController.DistachFromCurrentCell();

            if (m_deathController.CanResurrect)
            {
                m_respawnCell = DecideAndReturnRespawnCell(m_deathCell);
                m_respawnCell.OnCellStateUpdated += HandleRespawnCellUpdated;

                var deathPlacementFX = Instantiate(m_deathPlacementFXPrefab, deathPosition, Quaternion.identity);
                deathPlacementFX.SetUp(m_deathController.CharacterReferencesHolder.CharacterDataAsset, 
                    deathPosition,
                    m_respawnCell.transform.position,
                    m_deathController.WaitDurationToResurrect);
            }
        }

        private void HandleRespawnCellUpdated(Cell cell)
        {
            m_respawnCell.OnCellStateUpdated -= HandleRespawnCellUpdated;

            if (!CellIsValid(m_respawnCell))
            {
                m_respawnCell = ReturnClosestCellFrom(m_deathCell);
            }

            m_respawnCell.OnCellStateUpdated += HandleRespawnCellUpdated;
        }

        private Cell DecideAndReturnRespawnCell(Cell sourceCell)
        {
            // Source position must be gathered before teleportation.
            var dataAsset = m_globalGameplayDataManager.Data;
            // On invalid cell
            if (!CellIsValid(sourceCell)) 
            {
                Debug.Log($"Current cell is invalid.");
                switch (dataAsset.ResurrectionPlacementOnInvalidCell)
                {
                    case GlobalGameplayDataAsset.EResurrectionPlacement.Closest:
                        return ReturnClosestCellFrom(sourceCell);
                    case GlobalGameplayDataAsset.EResurrectionPlacement.Random:
                        return ReturnRandomCell();
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
                        return sourceCell; 
                    case GlobalGameplayDataAsset.EResurrectionPlacement.Random:
                        return ReturnRandomCell();
                    case GlobalGameplayDataAsset.EResurrectionPlacement.Checkpoint:
                        throw new NotImplementedException("Checkpoints not implemented.");
                }
            }

            Debug.LogError($"Could not find strategy for respawn placement.");
            return ReturnRandomCell();
        }

        private void HandleResurrection(DeathController obj)
        {
            if (m_respawnCell)
            {
                m_respawnCell.OnCellStateUpdated -= HandleRespawnCellUpdated;
                while (!CellIsValid(m_respawnCell))
                {
                    m_respawnCell = ReturnClosestCellFrom(m_deathCell);
                }
                TeleportToRespawnCell();
                m_respawnCell = null;
            }

            StartCoroutine(HandleInvincibilityDuration());
        }

        private IEnumerator HandleInvincibilityDuration()
        {
            m_deathController.RegisterInvincibilityGiver(this);
            yield return new WaitForSeconds(m_invincibilityDurationAfterResurrection);
            m_deathController.UnregisterInvincibilityGiver(this);
        }

        private Cell ReturnRandomCell()
        {
            return m_gridBuilder.GetRandomAvailableWalkableCell(cell =>
                !cell.TryGetComponent<KillingCellComponent>(out _));
        }

        // Breadth-first search on the grid: returns one of the closest valid cells, walking through invalid ones.
        private Cell ReturnClosestCellFrom(Cell a_sourceCell)
        {
            if (!a_sourceCell)
                return ReturnRandomCell();

            var exploredCells = new HashSet<Cell> { a_sourceCell };
            var cellsToExplore = new List<Cell> { a_sourceCell };

            while (cellsToExplore.Count > 0)
            {
                var validNeighbourCells = new List<Cell>();
                var cellsToExploreNextRound = new List<Cell>();

                foreach (var cell in cellsToExplore)
                {
                    foreach (var neighbourCell in new[] { cell.ForwardCell, cell.BackwardCell, cell.RightCell, cell.LeftCell })
                    {
                        if (!neighbourCell || !exploredCells.Add(neighbourCell))
                            continue;

                        if (CellIsValid(neighbourCell))
                            validNeighbourCells.Add(neighbourCell);
                        else
                            cellsToExploreNextRound.Add(neighbourCell);
                    }
                }

                if (validNeighbourCells.Count > 0)
                    return validNeighbourCells[UnityEngine.Random.Range(0, validNeighbourCells.Count)];

                cellsToExplore = cellsToExploreNextRound;
            }

            Debug.LogError($"Could not find a neighbour valid cell. Respawning at a random position.");
            return ReturnRandomCell();
        }

        private void TeleportToRespawnCell()
        {
            m_deathController.CharacterReferencesHolder.MovementController.TeleportToCell(m_respawnCell);
        }

        public bool CellIsValid(Cell cell)
        {
            return cell && !cell.GetComponent<KillingCellComponent>() &&
                   cell.TryGetComponent(out CanBeWalkedOnCellComponent canBeWalkedOnComp) &&
                    (!canBeWalkedOnComp.MovementControllerOnCell || 
                     canBeWalkedOnComp.MovementControllerOnCell == m_deathController.CharacterReferencesHolder.MovementController);
        }
    }
}
