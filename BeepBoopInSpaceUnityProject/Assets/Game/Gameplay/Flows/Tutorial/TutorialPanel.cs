using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Global.InputIndicationsManagement;
using Game.Global.PlayerManagement;
using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Gameplay.Flows.Tutorial
{
    /// <summary>
    /// The rules of the game and a card per player, until everyone is ready. The tutorial state fills it before
    /// showing it: the data is kept until the visual tree exists.
    /// </summary>
    public class TutorialPanel : AUIToolkitScreen
    {
        [SerializeField]
        private VisualTreeAsset m_playerCardTemplate;
        [SerializeField]
        private InputIndicationActionDataAsset m_readyIndicationAction;

        private readonly List<AbstractPlayer> m_players = new();
        private readonly Dictionary<AbstractPlayer, TutorialPlayerWidget> m_playerWidgets = new();
        private readonly Dictionary<AbstractPlayer, bool> m_playerStatus = new();
        public IReadOnlyDictionary<AbstractPlayer, bool> PlayerStatus => m_playerStatus;

        private string m_objectiveLabel;
        private string m_specialActionLabel;
        private bool m_hasMeteorites;

        private VisualElement m_waitingBar;
        private Tween m_waitingTween;

        public event Action<TutorialPanel> OnPlayerStatusUpdated;

        public void InflatePlayers(IReadOnlyList<AbstractPlayer> players)
        {
            m_players.Clear();
            m_players.AddRange(players);
            m_playerStatus.Clear();
            foreach (var player in players)
                m_playerStatus.Add(player, false);
            RefreshPlayers();
        }

        public void InflateGameData(string objectiveLabelKey, string specialActionLabelKey, bool hasMeteorites)
        {
            m_objectiveLabel = objectiveLabelKey;
            m_specialActionLabel = specialActionLabelKey;
            m_hasMeteorites = hasMeteorites;
            RefreshGameData();
        }

        protected override void HandleShown()
        {
            m_waitingBar = Query<VisualElement>("waiting-bar");
            RefreshGameData();
            RefreshPlayers();
        }

        protected override void HandleHidden()
        {
            ResetWaitingBar();
            m_waitingBar = null;
            m_playerWidgets.Clear();
        }

        private void RefreshGameData()
        {
            if (!IsShown)
                return;
            Query<Label>("objective-label").text = m_objectiveLabel;
            Query<Label>("special-action-label").text = m_specialActionLabel;
            Query<VisualElement>("meteorites").style.display = m_hasMeteorites ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RefreshPlayers()
        {
            m_playerWidgets.Clear();
            if (!IsShown)
                return;

            var container = Query<VisualElement>("player-cards");
            container.Clear();
            foreach (var player in m_players)
            {
                var widget = new TutorialPlayerWidget(m_playerCardTemplate, player);
                widget.SetStatus(m_playerStatus[player]);
                widget.OnReadyRequested += HandleReadyRequested;
                widget.OnUnreadyRequested += HandleUnreadyRequested;
                container.Add(widget.Element);
                AddIndication(widget.ReadyGlyph, m_readyIndicationAction, player);
                m_playerWidgets.Add(player, widget);
            }
        }

        private void HandleReadyRequested(TutorialPlayerWidget widget)
        {
            SetPlayerStatus(widget.Player, true);
        }

        private void HandleUnreadyRequested(TutorialPlayerWidget widget)
        {
            SetPlayerStatus(widget.Player, false);
        }

        public void TogglePlayerStatus(AbstractPlayer player)
        {
            SetPlayerStatus(player, !m_playerStatus[player]);
        }

        private void SetPlayerStatus(AbstractPlayer player, bool isReady)
        {
            m_playerStatus[player] = isReady;
            if (m_playerWidgets.TryGetValue(player, out var widget))
                widget.SetStatus(isReady);

            OnPlayerStatusUpdated?.Invoke(this);
        }

        public void PlayWaitingBarFor(float duration)
        {
            ResetWaitingBar();
            if (m_waitingBar == null)
                return;
            var waitingBar = m_waitingBar;
            m_waitingTween = DOTween.To(() => 0f, percent => waitingBar.style.width = Length.Percent(percent), 100f, duration)
                .SetTarget(waitingBar)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void ResetWaitingBar()
        {
            m_waitingTween?.Kill();
            m_waitingTween = null;
            if (m_waitingBar != null)
                m_waitingBar.style.width = 0;
        }
    }
}
