using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Gameplay.CharactersManagement.Actions;
using Game.Gameplay.PauseMenu;
using Game.Global.InputIndicationsManagement;
using Game.Global.PlayerManagement;
using Game.Online;
using Game.Online.Gameplay;
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
        private const string k_FourButtonsControlsClass = "tutorial__controls--four-buttons";
        private const string k_FiveButtonsControlsClass = "tutorial__controls--five-buttons";
        private const string k_ControlsRowClass = "tutorial__controls-row";
        private const string k_ControlsGlyphClass = "tutorial__controls-glyph";
        private const string k_GlyphClass = "ui-glyph";

        [SerializeField]
        private VisualTreeAsset m_playerCardTemplate;
        [SerializeField]
        private InputIndicationActionDataAsset m_readyIndicationAction;
        [SerializeField, Tooltip("The game's buttons, in the order of the actions: the control modes use the first ones.")]
        private List<InputIndicationActionDataAsset> m_actionIndications;

        private readonly List<AbstractPlayer> m_players = new();
        private readonly Dictionary<AbstractPlayer, TutorialPlayerWidget> m_playerWidgets = new();
        private readonly Dictionary<AbstractPlayer, bool> m_playerStatus = new();
        public IReadOnlyDictionary<AbstractPlayer, bool> PlayerStatus => m_playerStatus;

        private string m_objectiveLabel;
        private string m_specialActionLabel;
        private bool m_hasMeteorites;
        private ECharacterControlMode m_controlMode;

        private VisualElement m_waitingBar;
        private Tween m_waitingTween;
        private Label m_deviceNotice;
        private PauseMenuManager m_pauseMenuManager;

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

        public void InflateGameData(string objectiveLabelKey, string specialActionLabelKey, bool hasMeteorites,
            ECharacterControlMode controlMode)
        {
            m_objectiveLabel = objectiveLabelKey;
            m_specialActionLabel = specialActionLabelKey;
            m_hasMeteorites = hasMeteorites;
            m_controlMode = controlMode;
            RefreshGameData();
        }

        protected override void HandleShown()
        {
            m_waitingBar = Query<VisualElement>("waiting-bar");
            m_deviceNotice = Query<Label>("device-notice");
            m_pauseMenuManager = GameplayContext.Instance.PauseMenuManager;
            m_pauseMenuManager.OnPlayersWithoutDeviceChanged += HandlePlayersWithoutDeviceChanged;
            RefreshDeviceNotice();
            RefreshGameData();
            RefreshPlayers();
        }

        protected override void HandleHidden()
        {
            ResetWaitingBar();
            m_waitingBar = null;
            m_playerWidgets.Clear();
            if (m_pauseMenuManager)
                m_pauseMenuManager.OnPlayersWithoutDeviceChanged -= HandlePlayersWithoutDeviceChanged;
            m_pauseMenuManager = null;
            m_deviceNotice = null;
        }

        private void HandlePlayersWithoutDeviceChanged(PauseMenuManager obj)
        {
            RefreshDeviceNotice();
        }

        // The pause can't open during the tutorial, which may be waiting for a player without a pad: it says why.
        private void RefreshDeviceNotice()
        {
            var notice = m_pauseMenuManager.DeviceNotice;
            m_deviceNotice.style.display = string.IsNullOrEmpty(notice) ? DisplayStyle.None : DisplayStyle.Flex;
            m_deviceNotice.text = notice;
        }

        private void RefreshGameData()
        {
            if (!IsShown)
                return;
            Query<Label>("objective-label").text = Game.Global.Localization.Loc.T(m_objectiveLabel);
            Query<Label>("special-action-label").text = Game.Global.Localization.Loc.T(m_specialActionLabel);
            // The simplified controls without a special action: no button plays it.
            var specialActionDisplay = m_controlMode == ECharacterControlMode.Simplified ? DisplayStyle.None : DisplayStyle.Flex;
            Query<Label>("special-action-title").style.display = specialActionDisplay;
            Query<Label>("special-action-label").style.display = specialActionDisplay;
            Query<VisualElement>("meteorites").style.display = m_hasMeteorites ? DisplayStyle.Flex : DisplayStyle.None;
            var controls = Query<VisualElement>("controls");
            controls.EnableInClassList(k_FourButtonsControlsClass, m_controlMode == ECharacterControlMode.Simplified);
            controls.EnableInClassList(k_FiveButtonsControlsClass, m_controlMode == ECharacterControlMode.SimplifiedWithSpecialAction);
            RefreshControls(controls);
        }

        // The picture shows an Xbox controller: the other devices get the row of their buttons, one per device.
        private void RefreshControls(VisualElement picture)
        {
            var rows = Query<VisualElement>("controls-rows");
            foreach (var row in rows.Query(className: k_ControlsRowClass).ToList())
                row.RemoveFromHierarchy();

            var players = m_players.Where(player => !player.IsRemote)
                .GroupBy(player => player.PlayerInput.currentControlScheme)
                .Select(sameDevice => sameDevice.First())
                .ToList();
            var onlyXbox = players.All(player => player.PlayerInput.currentControlScheme is "XboxGamepad" or "OtherGamepad");
            picture.style.display = onlyXbox ? DisplayStyle.Flex : DisplayStyle.None;
            rows.style.display = onlyXbox ? DisplayStyle.None : DisplayStyle.Flex;
            if (onlyXbox)
                return;

            foreach (var player in players)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList(k_ControlsRowClass);
                rows.Add(row);
                foreach (var action in m_actionIndications.Take(m_controlMode.ButtonsCount()))
                {
                    var glyph = new VisualElement { pickingMode = PickingMode.Ignore };
                    glyph.AddToClassList(k_GlyphClass);
                    glyph.AddToClassList(k_ControlsGlyphClass);
                    row.Add(glyph);
                    AddIndication(glyph, action, player);
                }
            }
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
            RequestPlayerStatus(widget.Player, true);
        }

        private void HandleUnreadyRequested(TutorialPlayerWidget widget)
        {
            RequestPlayerStatus(widget.Player, false);
        }

        public void TogglePlayerStatus(AbstractPlayer player)
        {
            RequestPlayerStatus(player, !m_playerStatus[player]);
        }

        // A player of this machine gets ready or not: an online guest asks the host, which answers with the status.
        private void RequestPlayerStatus(AbstractPlayer player, bool isReady)
        {
            if (Netplay.IsGuest)
            {
                GameplaySync.SendTutorialStatusRequest(m_players.IndexOf(player), isReady);
                return;
            }
            SetPlayerStatus(player, isReady);
        }

        /// <summary>Sets whether a player is ready, and tells the guests of an online room.</summary>
        public void SetPlayerStatus(AbstractPlayer player, bool isReady)
        {
            if (!m_playerStatus.ContainsKey(player))
                return;
            GameplaySync.NotifyTutorialStatus(m_players.IndexOf(player), isReady);
            ApplyPlayerStatus(player, isReady);
        }

        /// <summary>Online guest: whether the host says a player is ready.</summary>
        public void ApplyPlayerStatus(AbstractPlayer player, bool isReady)
        {
            if (!m_playerStatus.ContainsKey(player))
                return;
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
