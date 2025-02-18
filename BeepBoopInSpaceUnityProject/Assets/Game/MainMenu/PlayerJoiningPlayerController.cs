using Game.PlayerManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.MainMenu
{
    public class PlayerJoiningPlayerController : MonoBehaviour
    {
        private AbstractPlayer m_player;
        private CharacterWidget m_widget;

        public void SetDependencies(AbstractPlayer player, CharacterWidget widget)
        {
            m_player = player;
            m_widget = widget;
        }

        public void Activate()
        {
            m_player.PlayerInput.actions.FindActionMap("MainMenu").FindAction("Pop").started += HandlePopStarted;
        }

        private void HandlePopStarted(InputAction.CallbackContext obj)
        {
            Pop();
        }

        public void Deactivate()
        {
            m_player.PlayerInput.actions.FindActionMap("MainMenu").FindAction("Pop").started -= HandlePopStarted;
        }


        public void Pop()
        {
            m_widget.Pop();
        }
    }
}