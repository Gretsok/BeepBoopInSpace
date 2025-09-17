using System.Collections.Generic;
using Game.Gameplay.Cells.Default;
using UnityEngine;

namespace Game.Gameplay.Environments.Core
{
    [CreateAssetMenu(fileName = "{Name} - CellsPoolDataAsset", menuName = "Game/Gameplay/Environments/CellsPoolDataAsset")]
    public class CellsPoolDataAsset : ScriptableObject
    {
        [SerializeField]
        private List<Cell> m_cells = new List<Cell>();
        public IReadOnlyList<Cell> Cells => m_cells;
    }
}
