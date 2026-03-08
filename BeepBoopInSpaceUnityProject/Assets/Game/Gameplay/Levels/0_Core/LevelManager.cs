using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.Cells.Default;
using UnityEngine;

namespace Game.Gameplay.Levels._0_Core
{
    public class LevelManager : AManager<LevelManager>
    {
        [SerializeField]
        private List<Cell> m_startingCells = new();
        public IReadOnlyList<Cell> StartingCells => m_startingCells;
    }
}
