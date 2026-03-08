using Game.Gameplay.Flows.Results;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tournament.Results
{
    public class TournamentResultWidget : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_winContainer;
        
        [SerializeField]
        private Image m_minigameImage;

        public void InflateResult(GameResult gameResult, GameResult.SPlayerResult playerResult)
        {
            m_winContainer.SetActive(playerResult.IsWinner);
            m_minigameImage.sprite = gameResult.LevelDataAsset.Thumbnail;
            m_minigameImage.color = Color.white;
        }
    }
}
