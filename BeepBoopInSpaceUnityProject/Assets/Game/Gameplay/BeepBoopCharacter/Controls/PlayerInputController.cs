using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.BeepBoopCharacter.Controls
{
    public class PlayerInputController : MonoBehaviour
    {
        [field: SerializeField]
        public BeepBoopCharacter Character { get; private set; }
        
        private PlayerControls m_controls;

        private void Awake()
        {
            m_controls = new PlayerControls();
            Enable();

            m_controls.Mobility.Jump.started += HandleJumpStarted;
            m_controls.Interaction.Interact.started += HandleInteractStarted;
            
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDestroy()
        {
            m_controls.Mobility.Jump.started -= HandleJumpStarted;
            m_controls.Interaction.Interact.started -= HandleInteractStarted;
            
            Disable();
            m_controls.Dispose();
        }

        public void Enable()
        {
            m_controls.Enable();
        }

        public void Disable()
        {
            m_controls.Disable();
        }

        private void HandleInteractStarted(InputAction.CallbackContext obj)
        {
            if (Character.Interactor.CurrentInteractableInSight)
                Character.Interactor.Interact();
            else if (Character.Holder.CurrentHoldable)
                Character.Holder.TryToReleaseCurrentHoldable();
        }

        private void HandleJumpStarted(InputAction.CallbackContext obj)
        {
            Character.MovementController.Jump();
        }

        private void Update()
        {
            Character.MovementController.SetMovementInput(m_controls.Mobility.Move.ReadValue<Vector2>());
            Character.RotationController.SetLookAroundInput(m_controls.Mobility.LookAround.ReadValue<Vector2>());
        }
    }
}