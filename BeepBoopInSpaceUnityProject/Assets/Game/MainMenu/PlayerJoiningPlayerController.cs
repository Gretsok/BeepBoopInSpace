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
            
            m_player.SetCharacterData(widget.CharacterDataAsset);
        }

        public void Activate()
        {
            m_player.PlayerInput.actions.FindActionMap("MainMenu").FindAction("Pop").started += HandlePopStarted;
            m_player.PlayerInput.actions.FindActionMap("MainMenu").FindAction("SwitchToNextCharacter").started += HandleSwitchToNextCharacterStarted;
        }

        private void HandleSwitchToNextCharacterStarted(InputAction.CallbackContext obj)
        {
            m_widget.SwitchToNextCharacter();
            m_player.SetCharacterData(m_widget.CharacterDataAsset);
        }

        private void HandlePopStarted(InputAction.CallbackContext obj)
        {
            Pop();
        }

        public void Deactivate()
        {
            m_player.PlayerInput.actions.FindActionMap("MainMenu").FindAction("Pop").started -= HandlePopStarted;
            m_player.PlayerInput.actions.FindActionMap("MainMenu").FindAction("SwitchToNextCharacter").started -= HandleSwitchToNextCharacterStarted;
        }


        public void Pop()
        {
            m_widget.Pop();
        }
    }
}