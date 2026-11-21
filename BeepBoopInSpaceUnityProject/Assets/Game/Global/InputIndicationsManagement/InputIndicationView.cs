using System;
using Game.Global.PlayerManagement;
using UnityEngine.InputSystem;
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
            var sprite = m_player ? m_actionDataAsset.GetSpriteFor(m_player.PlayerInput.currentControlScheme) : null;
            m_element.style.backgroundImage = sprite ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
        }
    }
}
