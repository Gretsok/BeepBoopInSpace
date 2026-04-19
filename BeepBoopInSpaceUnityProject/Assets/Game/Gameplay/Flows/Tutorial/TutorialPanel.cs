using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.ArchitectureTools.FlowMachine;
using Game.Global.PlayerManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Flows.Tutorial
{
    public class TutorialPanel : Panel
    {
        [SerializeField]
        private TMP_Text m_objectiveLabelText;
        [SerializeField]
        private TMP_Text m_specialActionLabelText;

        [SerializeField]
        private Image m_waitingBar;

        [SerializeField]
        private Transform m_playerWidgetsContainer;
        [SerializeField]
        private TutorialPlayerWidget m_playerWidgetPrefab;
        
        private Dictionary<AbstractPlayer, TutorialPlayerWidget> m_playerWidgets = new();
        private Dictionary<AbstractPlayer, bool> m_playerStatus = new();
        public IReadOnlyDictionary<AbstractPlayer, bool> PlayerStatus => m_playerStatus;

        public event Action<TutorialPanel> OnPlayerStatusUpdated;

        public void InflatePlayers(IReadOnlyList<AbstractPlayer> players)
        {
            for (int i = m_playerWidgetsContainer.childCount - 1; i >= 0; i--)
            {
                var child = m_playerWidgetsContainer.GetChild(i);
                Destroy(child.gameObject);
            }
            
            m_playerWidgets.Clear();
            m_playerStatus.Clear();

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                
                TutorialPlayerWidget widget = Instantiate(m_playerWidgetPrefab, m_playerWidgetsContainer);
                widget.SetPlayer(player);
                widget.SetStatus(false);
                m_playerWidgets.Add(player, widget);
                m_playerStatus.Add(player, false);

                widget.OnReadyRequested += HandleReadyRequested;
                widget.OnUnreadyRequested += HandleUnreadyRequested;
            }
        }

        private void HandleReadyRequested(TutorialPlayerWidget obj)
        {
            var player = obj.Player;
            
            m_playerStatus[player] = true;
            m_playerWidgets[player].SetStatus(true);
            
            OnPlayerStatusUpdated?.Invoke(this);
        }

        private void HandleUnreadyRequested(TutorialPlayerWidget obj)
        {
            var player = obj.Player;
            
            m_playerStatus[player] = false;
            m_playerWidgets[player].SetStatus(false);
            
            OnPlayerStatusUpdated?.Invoke(this);
        }

        public void TogglePlayerStatus(AbstractPlayer player)
        {
            m_playerStatus[player] = !m_playerStatus[player];
            m_playerWidgets[player].SetStatus(m_playerStatus[player]);
            
            OnPlayerStatusUpdated?.Invoke(this);
        }

        public void PlayWaitingBarFor(float duration)
        {
            ResetWaitingBar();
            m_waitingBar.rectTransform.DOAnchorMax(new Vector2(1f, 0f), duration);
        }

        public void ResetWaitingBar()
        {
            m_waitingBar.rectTransform.DOKill();
            m_waitingBar.rectTransform.anchorMax = Vector2.zero;
        }
    }
}
