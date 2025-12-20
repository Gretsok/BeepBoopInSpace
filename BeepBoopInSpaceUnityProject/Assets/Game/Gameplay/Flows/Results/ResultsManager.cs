using Game.ArchitectureTools.Manager;

namespace Game.Gameplay.Flows.Results
{
    public class ResultsManager : AManager<ResultsManager>
    {
        public enum EScoreCalculationMethod
        {
            HigherScoreIsBest = 0,
            LowerScoreIsBest = 1,
        }
        
        public EScoreCalculationMethod ScoreCalculationMethod { get; private set; }

        public void SetScoreCalculationMethod(EScoreCalculationMethod scoreCalculationMethod)
        {
            ScoreCalculationMethod = scoreCalculationMethod;
        }
    }
}
