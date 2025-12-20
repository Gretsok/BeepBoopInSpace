using System;
using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.Levels._0_Core;

namespace Game.Gameplay.Flows.Results
{
    public class ResultsManager : AManager<ResultsManager>
    {
        public EScoreCalculationMethod ScoreCalculationMethod { get; private set; }

        public void SetScoreCalculationMethod(EScoreCalculationMethod scoreCalculationMethod)
        {
            ScoreCalculationMethod = scoreCalculationMethod;
        }

        public event Action<ResultsManager, GameResult> OnGameResultProcessed;
        public void ProcessGameResult(List<CharacterPawn> orderedCharacterPawns)
        {
            GameResult gameResult = new GameResult();

            var charactersManager = CharactersManager.Instance;
            var currentLevelInfoManager = CurrentLevelInfoManager.Instance;
            
            List<GameResult.SPlayerResult> playersResults = new List<GameResult.SPlayerResult>();
            for (int i = 0; i < orderedCharacterPawns.Count; i++)
            {
                var pawn = orderedCharacterPawns[i];
                var player = charactersManager.GetAbstractPlayerFromCharacter(pawn);
                
                playersResults.Add(new GameResult.SPlayerResult()
                {
                    Player = player,
                    Score = pawn.ReferencesHolder.ScoringController.Score,
                });
            }
            
            gameResult.SetUp(playersResults, currentLevelInfoManager.CurrentLevelDataAsset, ScoreCalculationMethod);
            OnGameResultProcessed?.Invoke(this, gameResult);
        }
    }
}
