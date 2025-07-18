using System.Collections;
using System.Collections.Generic;
using Game.Gameplay.GridSystem;
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
            if (!obj.MovementControllerOnCell)
                return;
            
            obj.MovementControllerOnCell.TeleportToCell(m_gridBuilder.GetRandomWalkableCell());
        }
    }
}
