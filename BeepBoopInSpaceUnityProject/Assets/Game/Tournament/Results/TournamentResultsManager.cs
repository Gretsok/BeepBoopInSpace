using System.Collections.Generic;
using System.Linq;
using Game.Gameplay.Flows.Results;
using Game.Global.PlayerManagement;
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

            // A game can have several winners (a tie for the best score), or none (nobody scored).
            foreach (var gameResult in m_tournamentGameResults)
            {
                foreach (var playerResult in gameResult.Players)
                {
                    m_playersScores.TryGetValue(playerResult.Player, out var score);
                    m_playersScores[playerResult.Player] = playerResult.IsWinner ? score + 1 : score;
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
