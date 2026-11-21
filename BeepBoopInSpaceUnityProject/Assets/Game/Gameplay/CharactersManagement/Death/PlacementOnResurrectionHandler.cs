using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement.Death.Invincibility;
using Game.Gameplay.GlobalGameplayData;
using Game.Gameplay.GridSystem;
using Game.Gameplay.GridSystem.GenericComponents;
using Game.Online;
using Game.Online.Gameplay;
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
        private float m_resurrectionTime;
        private DeathPlacementFX m_deathPlacementFX;
        private Vector3? m_shownRespawnPosition;

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
            // A new spark for each death, from where the robot died.
            m_deathPlacementFX = null;
            m_shownRespawnPosition = null;

            // Online guest: the host says where the robot comes back (ReplicateRespawnTarget) and takes it there.
            if (!Netplay.HasAuthority)
                return;
            // Detaching clears the walker's current cell, so the death cell must be read first.
            m_deathCell = m_deathController.CharacterReferencesHolder.GridWalker.CurrentCell;
            m_deathController.CharacterReferencesHolder.MovementController.DistachFromCurrentCell();

            if (m_deathController.CanResurrect)
            {
                m_respawnCell = DecideAndReturnRespawnCell(m_deathCell);
                if (!m_respawnCell)
                    return;

                m_resurrectionTime = Time.time + m_deathController.WaitDurationToResurrect;
                GameplaySync.NotifyRespawnTarget(m_deathController, m_respawnCell, m_deathController.WaitDurationToResurrect);
                ShowRespawnTarget(m_respawnCell, m_deathController.WaitDurationToResurrect);
            }
        }

        // Another robot can walk onto the respawn cell while the spark travels, a hazard can take it: the robot will
        // come back on the closest valid cell, where its spark heads to instead of announcing the wrong cell.
        private void Update()
        {
            if (!m_respawnCell || m_deathController.IsAlive || !m_deathController.CanResurrect || !Netplay.HasAuthority
                || CellIsValid(m_respawnCell))
                return;

            var newRespawnCell = ReturnClosestCellFrom(m_respawnCell);
            if (!newRespawnCell || newRespawnCell == m_respawnCell)
                return;
            m_respawnCell = newRespawnCell;
            var travelDuration = Mathf.Max(0f, m_resurrectionTime - Time.time);
            GameplaySync.NotifyRespawnTarget(m_deathController, m_respawnCell, travelDuration);
            ShowRespawnTarget(m_respawnCell, travelDuration);
        }

        /// <summary>Online guest: the host's robot will come back on the cell.</summary>
        public void ReplicateRespawnTarget(Cell respawnCell, float travelDuration)
        {
            ShowRespawnTarget(respawnCell, travelDuration);
        }

        // The robot's spark, from where it died to where it comes back. A new respawn cell turns it toward it, or
        // starts a new one from the previous cell when it had already arrived.
        private void ShowRespawnTarget(Cell respawnCell, float travelDuration)
        {
            var destination = respawnCell.transform.position;
            if (m_deathPlacementFX)
                m_deathPlacementFX.Retarget(destination, travelDuration);
            else
            {
                var source = m_shownRespawnPosition ?? m_deathController.CharacterReferencesHolder.ModelSource.position;
                m_deathPlacementFX = Instantiate(m_deathPlacementFXPrefab, source, Quaternion.identity);
                m_deathPlacementFX.SetUp(m_deathController.CharacterReferencesHolder.CharacterDataAsset,
                    source,
                    destination,
                    travelDuration);
            }
            m_shownRespawnPosition = destination;
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
            if (m_respawnCell && Netplay.HasAuthority)
            {
                // The closest valid cell to the one the spark showed, else a random valid one: no other attempt
                // would find more.
                if (!CellIsValid(m_respawnCell))
                    m_respawnCell = ReturnClosestCellFrom(m_respawnCell);
                if (m_respawnCell)
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
