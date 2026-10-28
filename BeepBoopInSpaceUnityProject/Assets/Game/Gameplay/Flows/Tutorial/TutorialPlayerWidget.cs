using System;
using Game.Global.PlayerManagement;
using Game.UITools.Screens;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Game.Gameplay.Flows.Tutorial
{
    /// <summary>
    /// A player's card in the tutorial, cloned from the TutorialPlayerCard UXML: the robot, the player's badge, and
    /// whether the player is ready, in the character's color once ready. Players with a mouse can click it.
    /// </summary>
    public class TutorialPlayerWidget
    {
        private const string k_ReadyClass = "tutorial-card--ready";
        private const string k_BadgeDoneClass = "gs-badge--done";

        private readonly VisualElement m_badge;

        public AbstractPlayer Player { get; }
        public Button Element { get; }
        public VisualElement ReadyGlyph { get; }
        public bool IsReady { get; private set; }

        public event Action<TutorialPlayerWidget> OnReadyRequested;
        public event Action<TutorialPlayerWidget> OnUnreadyRequested;

        public TutorialPlayerWidget(VisualTreeAsset template, AbstractPlayer player)
        {
            Player = player;

            Element = template.Instantiate().Q<Button>("player-card");
            // Styled by Screens.uss alone, without the default theme's button style.
            Element.RemoveFromClassList(Button.ussClassName);
            ReadyGlyph = Element.Q("ready-glyph");
            m_badge = Element.Q("badge");

            var character = player.CharacterDataAsset;
            Element.Q("icon").style.backgroundImage = character.SelectionIcon
                ? new StyleBackground(character.SelectionIcon)
                : new StyleBackground(character.AvatarSprite);
            Element.Q<Label>("badge-label").text = $"J{player.PlayerInput.playerIndex + 1}";

            // Gamepad players press their Ready button (TutorialPlayerController): only the mouse clicks the card.
            if (player.PlayerInput.GetDevice<Mouse>() == null)
                Element.pickingMode = PickingMode.Ignore;
            Element.clicked += HandleClicked;
        }

        public void SetStatus(bool isReady)
        {
            IsReady = isReady;
            Element.EnableInClassList(k_ReadyClass, isReady);
            m_badge.EnableInClassList(k_BadgeDoneClass, isReady);

            var color = isReady ? new StyleColor(Player.CharacterDataAsset.SelectionColor) : new StyleColor(StyleKeyword.Null);
            Element.style.SetBorderColor(color);
            m_badge.style.backgroundColor = color;
        }

        private void HandleClicked()
        {
            if (IsReady)
                OnUnreadyRequested?.Invoke(this);
            else
                OnReadyRequested?.Invoke(this);
        }
    }
}
