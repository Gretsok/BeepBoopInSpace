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
        
        /// <summary>Equal scores share a rank (its badge), each robot keeps its own place. A game without a winner shows no badge.</summary>
        public void SetCharacterAt(CharacterPawn pawn, int placeIndex, int rankIndex, bool showsRank, bool isLast)
        {
            m_characterPositionners[placeIndex].InflateData(pawn, rankIndex, showsRank, isLast);
        }
    }
}