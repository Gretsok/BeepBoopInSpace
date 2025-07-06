using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Gameplay.GridSystem
{
    [CreateAssetMenu(fileName = "GridDataAsset", menuName = "Game/Gameplay/Grid System/Grid Data Asset")]
    public class GridDataAsset : ScriptableObject
    {
        [Serializable]
        public class RowData
        {
            [SerializeField]
            private List<int> m_cellIds;
            public IReadOnlyList<int> CellIds => m_cellIds;

            public void SetCellIds(IReadOnlyList<int> cellIds)
            {
                m_cellIds = cellIds.ToList();
            }
        }

        [SerializeField]
        private List<RowData> m_rows;
        public IReadOnlyList<RowData> Rows => m_rows;

        public void SetRows(IReadOnlyList<RowData> rows)
        {
            m_rows = rows.ToList();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
