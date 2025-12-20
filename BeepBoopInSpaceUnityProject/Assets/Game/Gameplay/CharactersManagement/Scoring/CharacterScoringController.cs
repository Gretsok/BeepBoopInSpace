using UnityEngine;

namespace Game.Gameplay.CharactersManagement.Scoring
{
    public class CharacterScoringController : MonoBehaviour
    {
        public int Score { get; private set; }

        public void IncreaseScore()
        {
            ++Score;
        }

        public void SetScore(int score)
        {
            Score = score;
        }
    }
}
