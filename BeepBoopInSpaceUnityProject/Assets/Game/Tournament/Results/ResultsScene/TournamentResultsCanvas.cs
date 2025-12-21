using System;
using Game.PlayerManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tournament.Results.ResultsScene
{
    public class TournamentResultsCanvas : MonoBehaviour
    {
        [SerializeField]
        private Transform m_resultsContainer;
        [SerializeField]
        private TournamentPlayerResultLine m_resultLinePrefab;

        [SerializeField]
        private Button m_okButton;

        private event Action m_quitCallback;
        public void SetUp(Action quitCallback)
        {
            var players = PlayerManager.Instance.Players;

            for (int i = m_resultsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(m_resultsContainer.GetChild(i).gameObject);
            }
            
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var line = Instantiate(m_resultLinePrefab, m_resultsContainer);
                line.SetUp(player);
            }
            
            m_quitCallback = quitCallback;
            m_okButton.onClick.AddListener(HandleOkButtonClicked);
        }

        private void HandleOkButtonClicked()
        {
            m_quitCallback?.Invoke();
            m_quitCallback = null;
        }
    }
}
