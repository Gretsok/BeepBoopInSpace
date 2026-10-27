using System;
using Game.Global.PlayerManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.UIElements;

namespace Game.Global.InputIndicationsManagement
{
    /// <summary>
    /// Shows, as the background of a UI Toolkit element, the input sprite of an action for the current device of a
    /// player. UI Toolkit counterpart of <see cref="InputIndicationWidget"/>. Dispose it when the element goes away.
    /// </summary>
    public class InputIndicationView : IDisposable
    {
        private readonly VisualElement m_element;
        private readonly InputIndicationActionDataAsset m_actionDataAsset;
        private AbstractPlayer m_player;

        /// <summary>Without a player, shows the sprite of the device players are the most likely to use instead of nothing.</summary>
        public bool ShowsLikelyDeviceWithoutPlayer { get; set; }

        public InputIndicationView(VisualElement element, InputIndicationActionDataAsset actionDataAsset)
        {
            m_element = element;
            m_actionDataAsset = actionDataAsset;
        }

        public void SetPlayer(AbstractPlayer player)
        {
            if (m_player)
                m_player.PlayerInput.onControlsChanged -= HandleControlsChanged;
            m_player = player;
            if (m_player)
                m_player.PlayerInput.onControlsChanged += HandleControlsChanged;
            Refresh();
        }

        public void Dispose()
        {
            SetPlayer(null);
        }

        private void HandleControlsChanged(PlayerInput playerInput)
        {
            Refresh();
        }

        private void Refresh()
        {
            var controlScheme = m_player ? m_player.PlayerInput.currentControlScheme
                : ShowsLikelyDeviceWithoutPlayer ? LikelyControlScheme() : null;
            var sprite = controlScheme != null ? m_actionDataAsset.GetSpriteFor(controlScheme) : null;
            m_element.style.backgroundImage = sprite ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
        }

        // The game is played with gamepads: the keyboard only when none is plugged in.
        private static string LikelyControlScheme()
        {
            var gamepad = Gamepad.current;
            if (gamepad == null)
                return Keyboard.current != null ? "KBM" : null;
            return gamepad is DualShockGamepad ? "PSGamepad" : "XboxGamepad";
        }
    }
}
