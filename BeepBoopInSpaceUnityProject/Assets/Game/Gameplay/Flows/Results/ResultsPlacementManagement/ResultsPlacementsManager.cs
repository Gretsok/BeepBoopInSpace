using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.CharactersManagement;
using UnityEngine;

namespace Game.Gameplay.Flows.Results.ResultsPlacementManagement
{
    public class ResultsPlacementsManager : AManager<ResultsPlacementsManager>
    {
        [SerializeField] private List<ResultsCharacterPositionner> m_characterPositionners;
        public IReadOnlyList<ResultsCharacterPositionner> CharacterPositionners => m_characterPositionners;
        
        public void SetCharacterAt(CharacterPawn pawn, int rankIndex)
        {
            m_characterPositionners[rankIndex].InflateData(pawn, rankIndex);
        }
    }
}