using System;
using Game.PlayerManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Flows.Tutorial
{
    public class TutorialPlayerController : MonoBehaviour
    {
        private InputAction m_readyAction;
        public AbstractPlayer Player { get; private set; }

        public event Action<TutorialPlayerController> OnPlayerStatusToggleRequested;
        public void Initialize(AbstractPlayer player)
        {
            Player = player;

            m_readyAction = Player.PlayerInput.actions.FindActionMap("FlowControl").FindAction("Ready");
            m_readyAction.Enable();
            m_readyAction.started += HandleReadyActionStarted;
        }

        private void OnDestroy()
        {
            m_readyAction.started -= HandleReadyActionStarted;
            m_readyAction.Disable();
        }

        private void HandleReadyActionStarted(InputAction.CallbackContext obj)
        {
            OnPlayerStatusToggleRequested?.Invoke(this);
        }
    }
}
