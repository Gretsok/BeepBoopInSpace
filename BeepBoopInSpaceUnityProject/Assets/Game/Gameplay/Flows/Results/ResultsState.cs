using System.Collections.Generic;
using System.Linq;
using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.CameraManagement;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.Flows.Results.ResultsPlacementManagement;
using Game.Gameplay.MusicsManagement;
using Game.Online;
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
        private ResultsManager m_resultsManager;
        private List<CharacterPawn> m_orderedCharacters = new ();
        private readonly List<CharacterPawn> m_winners = new ();

        private int m_currentViewedCharacterIndex = -1;
        private ResultsCharacterPositionner m_currentPositionner;
        private ResultsPanel m_resultsPanel;

        protected override void HandleEnter()
        {
            base.HandleEnter();
            m_resultsPanel = GameplayContext.Instance.UIManager.GetPanel<ResultsPanel>();
            
            m_cameraManager = CameraManager.Instance;
            m_cameraManager.SwitchToGlobalViewResultsCamera();
            
            m_resultsManager = ResultsManager.Instance;

            var characters = CharactersManager.Instance.CharacterPawns;
            var higherIsBest = m_resultsManager.ScoreCalculationMethod == EScoreCalculationMethod.HigherScoreIsBest;
            // OrderBy keeps the robots' order between equal scores: the same results on every machine of an online room.
            m_orderedCharacters = (higherIsBest ? characters.OrderByDescending(ScoreOf) : characters.OrderBy(ScoreOf)).ToList();

            // Equal scores share their rank, and the best one is a shared win. Nobody wins a game in which nobody scored.
            var bestScore = ScoreOf(m_orderedCharacters[0]);
            m_winners.Clear();
            if (!higherIsBest || bestScore > 0)
                m_winners.AddRange(m_orderedCharacters.Where(character => ScoreOf(character) == bestScore));

            m_resultsPlacementsManager = ResultsPlacementsManager.Instance;

            var rank = 0;
            for (int i = 0; i < m_orderedCharacters.Count; i++)
            {
                var character = m_orderedCharacters[i];
                if (i > 0 && ScoreOf(character) != ScoreOf(m_orderedCharacters[i - 1]))
                    rank = i;
                var isLast = i == m_orderedCharacters.Count - 1 && ScoreOf(character) != bestScore;
                m_resultsPlacementsManager.SetCharacterAt(character, i, rank, m_winners.Count > 0, isLast);
            }

            m_resultsManager.ProcessGameResult(m_orderedCharacters, m_winners);
            
            m_currentViewedCharacterIndex = m_orderedCharacters.Count;
            Invoke(nameof(ViewingNextCharacter), m_waitTimeOnStart);
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            // Pending close-ups would otherwise keep running, and overlap a new sequence if the state is entered again.
            CancelInvoke();
        }

        private static int ScoreOf(CharacterPawn character)
        {
            return character.ReferencesHolder.ScoringController.Score;
        }

        private void ViewingNextCharacter()
        {
            if (m_currentPositionner)
                m_currentPositionner.Display(false, false);
            
            --m_currentViewedCharacterIndex;
            var character = m_orderedCharacters[m_currentViewedCharacterIndex];
            m_currentPositionner = m_resultsPlacementsManager.CharacterPositionners.First(p => p.Character == character);
            
            m_currentPositionner.Display(true, true);
            
            
            if (m_winners.Contains(character))
            {
                character.ReferencesHolder.AnimationsHandler.Win();
                character.ReferencesHolder.RumbleHandler.PlayWinRumble();
                m_currentPositionner.ResultCharacter.Win();
            }
            else
            {
                character.ReferencesHolder.AnimationsHandler.Lose();
                character.ReferencesHolder.RumbleHandler.PlayLoseRumble();
                m_currentPositionner.ResultCharacter.Lose();
            }

            var nextStep = m_currentViewedCharacterIndex > 0 ? nameof(ViewingNextCharacter) : nameof(HandleEndOfCloseUps);
            Invoke(nextStep, m_timeToLookAtCharactersByRank[m_currentViewedCharacterIndex]);
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
            MusicsManager.Instance.StartPlayingResultsMusic();

            // Online, the host leaves the results for everyone.
            if (Netplay.HasAuthority)
                m_resultsPanel.ShowButton(() => RequestState(m_nextState));
            else
                m_resultsPanel.ShowWaitingForHost();
        }
    }
}