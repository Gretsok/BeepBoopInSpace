using System;
using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Game.Gameplay.GridSystem
{
    public class GridBuilder : AManager<GridBuilder>
    {
        [SerializeField]
        private Vector2Int m_gridSize = new Vector2Int(10, 10);

        [SerializeField] 
        private Vector2 m_cellSpacing = new Vector2(1f, 1f);
        [SerializeField]
        private Cell m_cellPrefab;

        [SerializeField]
        List<Row> m_cells = new();

        [Serializable]
        public class Row
        {
            [FormerlySerializedAs("Cells")] public List<Cell> RowData = new();
        }
        
        [Button]
        public void BuildGrid()
        {
            var gridSize = m_gridSize;
            var source = transform;
            var cellPrefab = m_cellPrefab;
            var cellSpacing = m_cellSpacing;

            List<Row> cells = new();
            
            for (int x = 0; x < gridSize.x; x++)
            {
                cells.Add(new Row());
                for (int y = 0; y < gridSize.y; y++)
                {
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
            var cells = m_cells;
            
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var cell = cells[x].RowData[y];

                    if (!cell)
                        continue;
                    
                    if (Application.isPlaying)
                        Destroy(cell.gameObject);
                    else
                        DestroyImmediate(cell.gameObject);
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
    }
}