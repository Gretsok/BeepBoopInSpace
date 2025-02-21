using System.Collections.Generic;
using System.Linq;
using Game.Gameplay.CameraManagement;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.FlowMachine;
using Game.Gameplay.Flows.Results.ResultsPlacementManagement;
using UnityEngine;

namespace Game.Gameplay.Flows.Results
{
    public class ResultsState : AFlowState
    {
        [SerializeField]
        private AFlowState m_nextState;

        [SerializeField]
        private float m_waitTimeOnStart = 1f;
        [SerializeField]
        private List<float> m_timeToLookAtCharactersByRank = new (){ 4f, 3f, 3f, 3f};
        
        private CameraManager m_cameraManager;
        private ResultsPlacementsManager m_resultsPlacementsManager;
        private List<CharacterPawn> m_orderedCharacters = new ();

        private int m_currentViewedCharacterIndex = -1;
        private ResultsCharacterPositionner m_currentPositionner;

        protected override void HandleEnter()
        {
            base.HandleEnter();
            m_cameraManager = CameraManager.Instance;
            m_cameraManager.SwitchToGlobalViewResultsCamera();

            var charactersManager = CharactersManager.Instance;
            m_orderedCharacters = new List<CharacterPawn>(charactersManager.CharacterPawns);
            m_orderedCharacters.Sort((item1, item2) => item2.Score.CompareTo(item1.Score));
            
            m_resultsPlacementsManager = ResultsPlacementsManager.Instance;

            for (int i = 0; i < m_orderedCharacters.Count; i++)
            {
                var character = m_orderedCharacters[i];
                m_resultsPlacementsManager.SetCharacterAt(character, i);
            }
            
            m_currentViewedCharacterIndex = m_orderedCharacters.Count;
            Invoke(nameof(ViewingNextCharacter), m_waitTimeOnStart);
        }

        private void ViewingNextCharacter()
        {
            if (m_currentPositionner)
                m_currentPositionner.Display(false, false);
            
            --m_currentViewedCharacterIndex;
            var character = m_orderedCharacters[m_currentViewedCharacterIndex];
            m_currentPositionner = m_resultsPlacementsManager.CharacterPositionners.First(p => p.Character == character);
            
            m_currentPositionner.Display(true, true);
            
            if (m_currentViewedCharacterIndex > 0)
            {
                Invoke(nameof(ViewingNextCharacter), m_timeToLookAtCharactersByRank[m_currentViewedCharacterIndex]);
            }
            else
            {
                Invoke(nameof(HandleEndOfCloseUps), m_timeToLookAtCharactersByRank[0]);
            }
        }

        private void HandleEndOfCloseUps()
        {
            for (int i = 0; i < m_resultsPlacementsManager.CharacterPositionners.Count; i++)
            {
                var positionner = m_resultsPlacementsManager.CharacterPositionners[i];
                if (positionner.Character == null)
                    continue;
                
                positionner.Display(true, false);
            }
            CameraManager.Instance.SwitchToGlobalViewResultsCamera();
            
            ResultsPanel.Instance.ShowButton(() => RequestState(m_nextState));
        }
    }
}