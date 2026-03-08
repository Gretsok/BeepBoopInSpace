using System.Collections.Generic;
using System.Linq;
using Game.Gameplay.Flows.Results;
using Game.PlayerManagement;
using Game.Tournament.Config;
using UnityEngine;

namespace Game.Tournament.Results
{
    public class TournamentResultsManager : MonoBehaviour
    {
        private TournamentConfigDataAsset m_configDataAsset;
        private readonly List<GameResult> m_tournamentGameResults = new ();
        public IReadOnlyList<GameResult> TournamentGameResults => m_tournamentGameResults;

        private readonly Dictionary<AbstractPlayer, int> m_playersScores = new();
        
        private ResultsManager m_resultsManager;
        
        public void InjectDependencies(TournamentConfigDataAsset configDataAsset)
        {
            m_configDataAsset = configDataAsset;
        }

        public void RegisterGameResult(GameResult gameResult)
        {
            m_tournamentGameResults.Add(gameResult);
            ProcessTotalPlayersScores();
        }

        private void ProcessTotalPlayersScores()
        {
            m_playersScores.Clear();

            for (int i = 0; i < m_tournamentGameResults.Count; i++)
            {
                var gameResult = m_tournamentGameResults[i];

                bool higherIsBest = gameResult.ScoreCalculationMethod == EScoreCalculationMethod.HigherScoreIsBest;
                
                int winningScore = higherIsBest
                    ? int.MinValue : int.MaxValue;
                AbstractPlayer winningPlayer = null;
                for (int playerResultIndex = 0; playerResultIndex < gameResult.Players.Count; ++playerResultIndex)
                {
                    var playerResult = gameResult.Players[playerResultIndex];
                    if (!m_playersScores.ContainsKey(playerResult.Player))
                        m_playersScores.Add(playerResult.Player, 0);

                    if (higherIsBest)
                    {
                        if (playerResult.Score > winningScore)
                        {
                            winningPlayer = playerResult.Player;
                            winningScore = playerResult.Score;
                        }
                    }
                    else
                    {
                        if (playerResult.Score < winningScore)
                        {
                            winningPlayer = playerResult.Player;
                            winningScore = playerResult.Score;
                        }
                    }
                }
                
                if (winningPlayer)
                    ++m_playersScores[winningPlayer];
                else
                {
                    Debug.LogError($"WHAT THE F*CK IS HAPPENING ????? WHY IS THIS HAPPENING ???? HAAAAAAAAAAAAAA");
                }
            }
        }

        public bool IsGameComplete()
        {
            return m_playersScores.Values.ToList().Exists(score => score >= m_configDataAsset.ScoreToReach);
        }

        public IEnumerable<GameResult.SPlayerResult> GetPlayerResultsOf(AbstractPlayer player)
        {
            List<GameResult.SPlayerResult> results = new();

            for (int i = 0; i < m_tournamentGameResults.Count; i++)
            {
                var gameResult = m_tournamentGameResults[i];
                
                results.Add(gameResult.Players.First(result => result.Player == player));
            }

            return results;
        }
    }
}
