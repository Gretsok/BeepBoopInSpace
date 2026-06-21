using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement.Movement;
using UnityEngine;

namespace Game.Gameplay.Levels._0_Core
{
    public class LevelManager : AManager<LevelManager>
    {
        [System.Serializable]
        public struct SStartingCellData
        {
            public Cell Cell;
            public CharacterMovementController.EDirection Direction;
        }
        [SerializeField]
        private List<SStartingCellData> m_startingCellsData = new();
        public IReadOnlyList<SStartingCellData> StartingCellsData => m_startingCellsData;
    }
}
