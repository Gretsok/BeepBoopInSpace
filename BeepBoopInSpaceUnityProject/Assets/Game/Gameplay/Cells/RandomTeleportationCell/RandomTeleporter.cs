using System.Collections;
using System.Collections.Generic;
using Game.Gameplay.GridSystem;
using Game.Gameplay.GridSystem.GenericComponents;
using Game.Online;
using UnityEngine;

namespace Game.Gameplay.Cells.RandomTeleportationCell
{
    public class RandomTeleporter : MonoBehaviour
    {
        private CanBeWalkedOnCellComponent m_cellComponent;
        private GridBuilder m_gridBuilder;
        
        private void Awake()
        {
            m_cellComponent = GetComponent<CanBeWalkedOnCellComponent>();
        }
        
        private IEnumerator Start()
        {
            yield return new WaitUntil(() => GridBuilder.Instance);
            m_gridBuilder = GridBuilder.Instance;
            m_cellComponent.OnMovementControllerOnCellChanged += HandleMovementControllerOnCellChanged;
        }

        private void HandleMovementControllerOnCellChanged(CanBeWalkedOnCellComponent obj)
        {
            // Online guest: the host teleports.
            if (!obj.MovementControllerOnCell || !Netplay.HasAuthority)
                return;
            
            var destination = m_gridBuilder.GetRandomAvailableWalkableCell(cell => !cell.TryGetComponent<KillingCellComponent>(out _));
            if (destination)
                obj.MovementControllerOnCell.TeleportToCell(destination);
        }
    }
}
