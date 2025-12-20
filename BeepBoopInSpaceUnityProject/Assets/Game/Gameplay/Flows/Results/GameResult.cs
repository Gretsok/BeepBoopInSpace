using System.Collections.Generic;
using System.Linq;
using Game.Gameplay.Levels._0_Core;
using Game.PlayerManagement;

namespace Game.Gameplay.Flows.Results
{
    public class GameResult
    {
        public struct SPlayerResult
        {
            public AbstractPlayer Player;
            public int Score;
        }

        private List<SPlayerResult> m_players = new();
        public IReadOnlyList<SPlayerResult> Players => m_players;
        private LevelDataAsset m_levelDataAsset;
        public LevelDataAsset LevelDataAsset => m_levelDataAsset;
        private EScoreCalculationMethod m_scoreCalculationMethod;
        public EScoreCalculationMethod ScoreCalculationMethod => m_scoreCalculationMethod;

        public void SetUp(
            IEnumerable<SPlayerResult> playersResults, 
            LevelDataAsset levelDataAsset,
            EScoreCalculationMethod scoreCalculationMethod
            )
        {
            m_players = playersResults.ToList();
            m_levelDataAsset = levelDataAsset;
            m_scoreCalculationMethod = scoreCalculationMethod;
        }
    }
}
