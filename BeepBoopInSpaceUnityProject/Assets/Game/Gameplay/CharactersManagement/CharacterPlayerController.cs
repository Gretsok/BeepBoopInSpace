using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.PlayerManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.CharactersManagement
{
    public class CharacterPlayerController : MonoBehaviour
    {
        private CharacterPawn m_pawn;
        public CharacterPawn Pawn => m_pawn;
        private CharacterReferencesHolder m_referencesHolder;

        public void InjectDependencies(CharacterPawn pawn)
        {
            m_pawn = pawn;
            m_referencesHolder = pawn.ReferencesHolder;
        }
        
        private AbstractPlayer m_player;
        public AbstractPlayer Player => m_player;

        public void SetPlayer(AbstractPlayer player)
        {
            m_player = player;
        }
        public void Activate()
        {
            var actionsMap = m_player.PlayerInput.actions.FindActionMap("Gameplay");

            actionsMap.Enable();

            actionsMap.FindAction("Action_1").started += HandleAction1Started;
            actionsMap.FindAction("Action_2").started += HandleAction2Started;
            actionsMap.FindAction("Action_3").started += HandleAction3Started;
            actionsMap.FindAction("Action_4").started += HandleAction4Started;
            actionsMap.FindAction("Action_5").started += HandleAction5Started;
            actionsMap.FindAction("Action_6").started += HandleAction6Started;
        }

        public void Deactivate()
        {
            var actionsMap = m_player.PlayerInput.actions.FindActionMap("Gameplay");

            actionsMap.FindAction("Action_1").started -= HandleAction1Started;
            actionsMap.FindAction("Action_2").started -= HandleAction2Started;
            actionsMap.FindAction("Action_3").started -= HandleAction3Started;
            actionsMap.FindAction("Action_4").started -= HandleAction4Started;
            actionsMap.FindAction("Action_5").started -= HandleAction5Started;
            actionsMap.FindAction("Action_6").started -= HandleAction6Started;
            
            actionsMap.Disable();
        }

        private void HandleAction1Started(InputAction.CallbackContext obj)
        {
            m_referencesHolder.ActionsController.TryToPerformAction(0);
        }

        private void HandleAction2Started(InputAction.CallbackContext obj)
        {
            m_referencesHolder.ActionsController.TryToPerformAction(1);
        }

        private void HandleAction3Started(InputAction.CallbackContext obj)
        {
            m_referencesHolder.ActionsController.TryToPerformAction(2);
        }

        private void HandleAction4Started(InputAction.CallbackContext obj)
        {
            m_referencesHolder.ActionsController.TryToPerformAction(3);
        }

        private void HandleAction5Started(InputAction.CallbackContext obj)
        {
            m_referencesHolder.ActionsController.TryToPerformAction(4);
        }

        private void HandleAction6Started(InputAction.CallbackContext obj)
        {
            m_referencesHolder.ActionsController.TryToPerformAction(5);
        }
    }
}