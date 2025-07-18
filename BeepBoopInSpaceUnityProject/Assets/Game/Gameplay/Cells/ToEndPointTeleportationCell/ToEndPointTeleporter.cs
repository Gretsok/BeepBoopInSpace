using System.Collections;
using Game.Gameplay.GridSystem;
using UnityEngine;

namespace Game.Gameplay.Cells.ToEndPointTeleportationCell
{
    public class ToEndPointTeleporter : MonoBehaviour
    {
        private CanBeWalkedOnCellComponent m_cellComponent;
        private GridBuilder m_gridBuilder;
        
        [SerializeField]
        private GameObject m_endPointIndicator;
        [SerializeField]
        private Vector2Int m_endPointCoordinates;

        private void Awake()
        {
            m_cellComponent = GetComponent<CanBeWalkedOnCellComponent>();
        }

        private void HandleMovementControllerOnCellChanged(CanBeWalkedOnCellComponent obj)
        {
            if (!obj.MovementControllerOnCell)
                return;
            
            obj.MovementControllerOnCell.TeleportToCell(m_gridBuilder.GetCellAt(m_endPointCoordinates));
        }

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => GridBuilder.Instance);
            m_gridBuilder = GridBuilder.Instance;
            UpdateIndicatorPosition(m_gridBuilder);
            m_cellComponent.OnMovementControllerOnCellChanged += HandleMovementControllerOnCellChanged;
        }

        private void UpdateIndicatorPosition(GridBuilder gridBuilder)
        {
            if (!m_endPointIndicator)
                return;
            
            var cellAtPosition = gridBuilder.GetCellAt(m_endPointCoordinates);
            if (!cellAtPosition)
            {
                Debug.LogError($"Destination cell is null");
                return;
            }
            
            m_endPointIndicator.transform.position = cellAtPosition.transform.position;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            var gridBuilder = FindFirstObjectByType<GridBuilder>();
            UpdateIndicatorPosition(gridBuilder);
        }
#endif
    }
}
