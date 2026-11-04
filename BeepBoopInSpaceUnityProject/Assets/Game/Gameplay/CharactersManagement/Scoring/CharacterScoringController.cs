using Game.Online.Gameplay;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement.Scoring
{
    public class CharacterScoringController : MonoBehaviour
    {
        public int Score { get; private set; }

        public void IncreaseScore()
        {
            ++Score;
            GameplaySync.NotifyScore(this);
        }

        public void SetScore(int score)
        {
            Score = score;
            GameplaySync.NotifyScore(this);
        }
    }
}
