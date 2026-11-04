using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.Online;
using Game.Online.Gameplay;
using Game.Global.PlayerManagement;
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
        /// <summary>The player's buttons drive the robot (in the gameplay state, outside of the pause).</summary>
        public bool IsActive { get; private set; }

        public void Activate()
        {
            IsActive = true;
            // A player of a guest's machine: their buttons arrive through the network (PerformRemoteAction).
            if (m_player.IsRemote)
                return;
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
            IsActive = false;
            if (!m_player || m_player.IsRemote)
                return;
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
            PerformAction(0);
        }

        private void HandleAction2Started(InputAction.CallbackContext obj)
        {
            PerformAction(1);
        }

        private void HandleAction3Started(InputAction.CallbackContext obj)
        {
            PerformAction(2);
        }

        private void HandleAction4Started(InputAction.CallbackContext obj)
        {
            PerformAction(3);
        }

        private void HandleAction5Started(InputAction.CallbackContext obj)
        {
            PerformAction(4);
        }

        private void HandleAction6Started(InputAction.CallbackContext obj)
        {
            PerformAction(5);
        }

        // A guest's machine sends its players' buttons to the host, which plays them.
        private void PerformAction(int index)
        {
            // Online, the pause doesn't stop the game: its menu takes the buttons of this machine's players.
            var gameplayContext = GameplayContext.Instance;
            if (gameplayContext && gameplayContext.PauseMenuManager.IsPaused)
                return;
            if (Netplay.IsGuest)
            {
                GameplaySync.SendPlayerAction(m_pawn, index);
                return;
            }
            m_referencesHolder.ActionsController.TryToPerformAction(index);
        }

        private void OnDestroy()
        {
            if (IsActive)
                Deactivate();
        }

        /// <summary>Host: a button of a player of a guest's machine.</summary>
        public void PerformRemoteAction(int index)
        {
            if (IsActive && index < 6)
                m_referencesHolder.ActionsController.TryToPerformAction(index);
        }
    }
}