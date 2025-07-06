using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Gameplay.GridSystem
{
    [CreateAssetMenu(fileName = "CellsDictionaryDataAsset", menuName = "Game/Gameplay/Grid System/Cells Dictionary Data Asset")]
    public class CellsDictionaryDataAsset : ScriptableObject
    {
        [SerializeField]
        private List<Cell> m_cellsPrefabs;

        public Cell GetCellPrefabWithID(int id)
        {
            return m_cellsPrefabs.FirstOrDefault(cellPrefab => cellPrefab.CellTypeID == id);
        }
    }
}
