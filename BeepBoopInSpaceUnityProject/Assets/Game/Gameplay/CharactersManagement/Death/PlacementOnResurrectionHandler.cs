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
            var deathPlacementFX = Instantiate(m_deathPlacementFXPrefab, m_deathController.CharacterReferencesHolder.ModelSource.position, Quaternion.identity);
            var sourcePosition = m_deathController.CharacterReferencesHolder.ModelSource.position;
            m_respawnCell = DecideAndReturnRespawnCell(m_deathController.CharacterReferencesHolder.GridWalker.CurrentCell);
            m_respawnCell.OnCellStateUpdated += HandleRespawnCellUpdated;
            deathPlacementFX.SetUp(m_deathController.CharacterReferencesHolder.CharacterDataAsset, 
                sourcePosition,
                m_respawnCell.transform.position,
                m_deathController.WaitDurationToResurrect);
            m_deathController.CharacterReferencesHolder.MovementController.DistachFromCurrentCell();
        }

        private void HandleRespawnCellUpdated(Cell cell)
        {
            m_respawnCell.OnCellStateUpdated -= HandleRespawnCellUpdated;

            if (!CellIsValid(m_respawnCell))
            {
                m_respawnCell = ReturnClosestCellFrom(m_deathController.CharacterReferencesHolder.GridWalker.CurrentCell);
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
            m_respawnCell.OnCellStateUpdated -= HandleRespawnCellUpdated;
            while (!CellIsValid(m_respawnCell))
            {
                m_respawnCell = ReturnClosestCellFrom(m_deathController.CharacterReferencesHolder.GridWalker.CurrentCell);
            }
            TeleportToRespawnCell();
            m_respawnCell = null;

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

        private Cell ReturnClosestCellFrom(Cell a_sourceCell, HashSet<Cell> exploredCells = null)
        {
            if (exploredCells == null)
                exploredCells = new HashSet<Cell>();
            
            HashSet<Cell> validNeighbourCells = new();
            HashSet<Cell> surroundingToExploreThisRoundCells = new HashSet<Cell>();
            surroundingToExploreThisRoundCells.Add(a_sourceCell);

            do
            {
                var cellsToExploreNextRound = new HashSet<Cell>();
                var enumerator = surroundingToExploreThisRoundCells.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var rootCellToExplore = enumerator.Current;

                    if (!rootCellToExplore)
                        continue;
                    
                    if (!exploredCells.Contains(rootCellToExplore.ForwardCell))
                    {
                        var neighbourCellToExplore = rootCellToExplore.ForwardCell;
                        if (CellIsValid(neighbourCellToExplore))
                        {
                            validNeighbourCells.Add(neighbourCellToExplore);
                        }
                        else if (neighbourCellToExplore)
                        {
                            if (neighbourCellToExplore.ForwardCell && !exploredCells.Contains(neighbourCellToExplore.ForwardCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.ForwardCell);
                            if (neighbourCellToExplore.BackwardCell && !exploredCells.Contains(neighbourCellToExplore.BackwardCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.BackwardCell);
                            if (neighbourCellToExplore.RightCell && !exploredCells.Contains(neighbourCellToExplore.RightCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.RightCell);
                            if (neighbourCellToExplore.LeftCell && !exploredCells.Contains(neighbourCellToExplore.LeftCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.LeftCell);
                        }
                        
                        if (neighbourCellToExplore)
                            exploredCells.Add(neighbourCellToExplore);
                    }
                    
                    if (!exploredCells.Contains(rootCellToExplore.BackwardCell))
                    {
                        var neighbourCellToExplore = rootCellToExplore.BackwardCell;
                        if (CellIsValid(neighbourCellToExplore))
                        {
                            validNeighbourCells.Add(neighbourCellToExplore);
                        }
                        else if (neighbourCellToExplore)
                        {
                            if (neighbourCellToExplore.ForwardCell && !exploredCells.Contains(neighbourCellToExplore.ForwardCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.ForwardCell);
                            if (neighbourCellToExplore.BackwardCell && !exploredCells.Contains(neighbourCellToExplore.BackwardCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.BackwardCell);
                            if (neighbourCellToExplore.RightCell && !exploredCells.Contains(neighbourCellToExplore.RightCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.RightCell);
                            if (neighbourCellToExplore.LeftCell && !exploredCells.Contains(neighbourCellToExplore.LeftCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.LeftCell);
                        }
                        
                        if (neighbourCellToExplore)
                            exploredCells.Add(neighbourCellToExplore);
                    }
                    
                    if (!exploredCells.Contains(rootCellToExplore.RightCell))
                    {
                        var neighbourCellToExplore = rootCellToExplore.RightCell;
                        if (CellIsValid(neighbourCellToExplore))
                        {
                            validNeighbourCells.Add(neighbourCellToExplore);
                        }
                        else if (neighbourCellToExplore)
                        {
                            if (neighbourCellToExplore.ForwardCell && !exploredCells.Contains(neighbourCellToExplore.ForwardCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.ForwardCell);
                            if (neighbourCellToExplore.BackwardCell && !exploredCells.Contains(neighbourCellToExplore.BackwardCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.BackwardCell);
                            if (neighbourCellToExplore.RightCell && !exploredCells.Contains(neighbourCellToExplore.RightCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.RightCell);
                            if (neighbourCellToExplore.LeftCell && !exploredCells.Contains(neighbourCellToExplore.LeftCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.LeftCell);
                        }
                        
                        if (neighbourCellToExplore)
                            exploredCells.Add(neighbourCellToExplore);
                    }
                    
                    if (!exploredCells.Contains(rootCellToExplore.LeftCell))
                    {
                        var neighbourCellToExplore = rootCellToExplore.LeftCell;
                        if (CellIsValid(neighbourCellToExplore))
                        {
                            validNeighbourCells.Add(neighbourCellToExplore);
                        }
                        else if (neighbourCellToExplore)
                        {
                            if (neighbourCellToExplore.ForwardCell && !exploredCells.Contains(neighbourCellToExplore.ForwardCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.ForwardCell);
                            if (neighbourCellToExplore.BackwardCell && !exploredCells.Contains(neighbourCellToExplore.BackwardCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.BackwardCell);
                            if (neighbourCellToExplore.RightCell && !exploredCells.Contains(neighbourCellToExplore.RightCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.RightCell);
                            if (neighbourCellToExplore.LeftCell && !exploredCells.Contains(neighbourCellToExplore.LeftCell))
                                cellsToExploreNextRound.Add(neighbourCellToExplore.LeftCell);
                        }
                        
                        if (neighbourCellToExplore)
                            exploredCells.Add(neighbourCellToExplore);
                    }
                }
                
                // Should update cells to explore here
                surroundingToExploreThisRoundCells = cellsToExploreNextRound;

            } while (validNeighbourCells.Count == 0 && surroundingToExploreThisRoundCells.Count > 0);

            if (validNeighbourCells.Count == 0)
            {
                Debug.LogError($"Could not find a neighbour valid cell. Respawning at a random position.");
                
                return ReturnRandomCell();
            }

            return validNeighbourCells.ElementAt(UnityEngine.Random.Range(0, validNeighbourCells.Count));
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
