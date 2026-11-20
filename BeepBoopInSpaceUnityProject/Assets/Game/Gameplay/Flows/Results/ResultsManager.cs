using System;
using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.CharactersManagement;

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
        public void ProcessGameResult(List<CharacterPawn> orderedCharacterPawns, List<CharacterPawn> winners)
        {
            GameResult gameResult = new GameResult();

            var charactersManager = CharactersManager.Instance;
            var currentLevelInfoManager = GameplayContext.Instance.CurrentLevelInfoManager;
            
            List<GameResult.SPlayerResult> playersResults = new List<GameResult.SPlayerResult>();
            for (int i = 0; i < orderedCharacterPawns.Count; i++)
            {
                var pawn = orderedCharacterPawns[i];
                var player = charactersManager.GetAbstractPlayerFromCharacter(pawn);
                
                playersResults.Add(new GameResult.SPlayerResult()
                {
                    Player = player,
                    Score = pawn.ReferencesHolder.ScoringController.Score,
                    IsWinner = winners.Contains(pawn),
                });
            }
            
            gameResult.SetUp(playersResults, currentLevelInfoManager.CurrentLevelDataAsset, ScoreCalculationMethod);
            OnGameResultProcessed?.Invoke(this, gameResult);
        }
    }
}
