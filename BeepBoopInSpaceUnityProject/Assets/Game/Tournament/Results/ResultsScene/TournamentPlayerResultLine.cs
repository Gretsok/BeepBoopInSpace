using System.Linq;
using Game.PlayerManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tournament.Results
{
    public class TournamentPlayerResultLine : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text m_scoreText;

        [SerializeField]
        private Image m_robotAvatarPicture;

        [SerializeField]
        private Transform m_progressContainer;

        [SerializeField]
        private TournamentResultWidget m_resultWidgetPrefab;
        
        public AbstractPlayer Player { get; private set; }
        
        public void SetUp(AbstractPlayer player)
        {
            Player = player;
            
            var resultsManager = TournamentContext.Instance.ResultsManager;

            var gameResults = resultsManager.TournamentGameResults;
            var playerResults = resultsManager.GetPlayerResultsOf(player).ToList();
            var score = playerResults.Count(result => result.IsWinner);
            
            m_scoreText.text = score.ToString();
            m_robotAvatarPicture.sprite = player.CharacterDataAsset.AvatarSprite;

            for (int i = m_progressContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(m_progressContainer.GetChild(i).gameObject);
            }

            for (int i = 0; i < gameResults.Count; i++)
            {
                var widget = Instantiate(m_resultWidgetPrefab, m_progressContainer);
                widget.InflateResult(gameResults[i], playerResults[i]);
            }
        }
    }
}
