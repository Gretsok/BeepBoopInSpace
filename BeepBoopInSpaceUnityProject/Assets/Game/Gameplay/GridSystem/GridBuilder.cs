using System;
using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using NaughtyAttributes;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Game.Gameplay.GridSystem
{
    public class GridBuilder : AManager<GridBuilder>
    {
        [SerializeField]
        private GridDataAsset m_gridDataAsset;
        [SerializeField]
        private CellsDictionaryDataAsset m_cellsDictionaryDataAsset;
        [SerializeField]
        private Vector2Int m_gridSize = new Vector2Int(10, 10);

        [SerializeField] 
        private Vector2 m_cellSpacing = new Vector2(1f, 1f);


        [SerializeField]
        List<Row> m_cells = new();

        [Serializable]
        public class Row
        {
            [FormerlySerializedAs("Cells")] public List<Cell> RowData = new();
        }
        
        [Button]
        public void BuildGridFromGridDataAsset()
        {
            if (!m_gridDataAsset)
            {
                Debug.LogError("No grid data asset found. Cannot read from it.");
                return;
            }
            
            CleanCells();
            var gridSize = m_gridSize;
            var source = transform;
            var cellSpacing = m_cellSpacing;

            List<Row> cells = new();
            
            for (int x = 0; x < gridSize.x; x++)
            {
                cells.Add(new Row());
                for (int y = 0; y < gridSize.y; y++)
                {
                    var cellTypeId = m_gridDataAsset.Rows[x].CellIds[y];
                    var cellPrefab = m_cellsDictionaryDataAsset.GetCellPrefabWithID(cellTypeId);
                    if (!cellPrefab)
                    {
                        cells[x].RowData.Add(null);
                        continue;
                    }
#if UNITY_EDITOR
                    var cell = UnityEditor.PrefabUtility.InstantiatePrefab(cellPrefab, source) as Cell;
#else
                    var cell = Instantiate<Cell>(cellPrefab, source);
#endif

                    cell.transform.localPosition = new Vector3(x * cellSpacing.x, 0, y * cellSpacing.y);
                    
                    cells[x].RowData.Add(cell);
                }
            }
            
            m_cells = cells;
            
            GenerateLinks();
        }

        [Button]
        public void WriteToGridDataAsset()
        {
            if (!m_gridDataAsset)
            {
                Debug.LogError("No grid data asset found. Cannot write to it.");
                return;
            }
            
            List<GridDataAsset.RowData> rows = new();
            for (int i = 0; i < m_cells.Count; i++)
            {
                var row = m_cells[i];
                GridDataAsset.RowData rowData = new GridDataAsset.RowData();

                List<int> rowIds = new();
                for (int y = 0; y < row.RowData.Count; ++y)
                {
                    if (row.RowData[y])
                        rowIds.Add(row.RowData[y].CellTypeID);
                    else
                        rowIds.Add(-1);
                }
                rowData.SetCellIds(rowIds);
                rows.Add(rowData);
            }
            
            m_gridDataAsset.SetRows(rows);
        }

        [Button]
        private void GenerateLinks()
        {
            var gridSize = m_gridSize;
            var cells = m_cells;
            
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var cell = cells[x].RowData[y];

                    if (!cell)
                        continue;
                    
                    if (x - 1 >= 0)
                        cell.SetLeftCell(cells[x - 1].RowData[y]);
                    if (x + 1 < m_gridSize.x)
                        cell.SetRightCell(cells[x + 1].RowData[y]);
                    if (y - 1 >= 0)
                        cell.SetBackwardCell(cells[x].RowData[y - 1]);
                    if (y + 1 < m_gridSize.y)
                        cell.SetForwardCell(cells[x].RowData[y + 1]);
                }
            }
        }

        [Button]
        private void CleanCells()
        {
            var gridSize = m_gridSize;
            var rows = m_cells;

            for (int i = 0; i < rows.Count; ++i)
            {
                var row = rows[i];

                for (int j = row.RowData.Count - 1; j >= 0 ; --j)
                {
                    if (row.RowData[j])
                        DestroyImmediate(row.RowData[j].gameObject);
                }
            }
            
            m_cells.Clear();
        }

        public Cell GetRandomCell()
        {
            var randomRowIndex = Random.Range(0, m_cells.Count);
            var randomCellIndexInRow = Random.Range(0, m_cells[randomRowIndex].RowData.Count);
            return m_cells[randomRowIndex].RowData[randomCellIndexInRow];
        }

        public Cell GetCellAt(Vector2Int position)
        {
            if (position.x < 0
                || position.y < 0
                || m_cells.Count <= position.x
                || m_cells[position.x].RowData.Count <= position.y)
            {
                Debug.LogError($"Cell at ({position.x}, {position.y}) is out of range.");
                return null;
            }

            return m_cells[position.x].RowData[position.y];
        }
    }
}