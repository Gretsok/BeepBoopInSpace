using System;
using DG.Tweening;
using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.Flows.End;
using Game.Online;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.PauseMenu
{
    public class PauseMenuManager : MonoBehaviour,
        IFlowMachinePauser
    {
        private FlowMachine m_gameplayFlowMachine;
        public bool IsPaused { get; private set; }
        
        private PauseControls m_controls;
        public void Initialize(FlowMachine gameplayFlowMachine)
        {
            m_controls = new PauseControls();
            m_controls.Enable();
            
            m_controls.Pause.Toggle.started += HandlePauseToggleStarted;
            
            m_gameplayFlowMachine = gameplayFlowMachine;
        }

        private void HandlePauseToggleStarted(InputAction.CallbackContext obj)
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }

        public Action<PauseMenuManager> OnPause;
        public void Pause()
        {
            if (IsPaused || m_isQuitting)
                return;
            if (!m_gameplayFlowMachine.CurrentState.GetComponent<CanBePausedTagStateComponent>())
                return;

            // Online, the game goes on for the other machines: the menu opens over it, and takes this machine's
            // players' buttons (CharacterPlayerController).
            m_isPausingTheGame = !Netplay.IsOnline;
            if (m_isPausingTheGame)
            {
                Time.timeScale = 0;
                DOTween.defaultTimeScaleIndependent = true;

                m_gameplayFlowMachine.Pause(this);
            }
            OnPause?.Invoke(this);
            
            IsPaused = true;
        }

        public Action<PauseMenuManager> OnResume;
        public void Resume()
        {
            if (!IsPaused)
                return;

            if (m_isPausingTheGame)
            {
                Time.timeScale = 1;
                DOTween.defaultTimeScaleIndependent = false;

                m_gameplayFlowMachine.Unpause(this);
            }
            OnResume?.Invoke(this);
            
            IsPaused = false;
        }

        private bool m_isPausingTheGame;
        private bool m_isQuitting;
        public bool IsQuitting => m_isQuitting;
        /// <summary>Leaves the game for the main menu through the end state, which unloads it. A tournament is abandoned.</summary>
        public void QuitToMainMenu()
        {
            if (m_isQuitting)
                return;
            m_isQuitting = true;

            Resume();
            // Online, leaving the game leaves the room: a host closes it, its guests leave their game too.
            if (Netplay.IsOnline)
                OnlineManager.Instance.LeaveRoom();
            // This machine decides now, even an online guest's.
            m_gameplayFlowMachine.IsRemotelyDriven = false;
            var endState = EndStateGrabber.Instance.EndState;
            endState.IsQuittingToMainMenu = true;
            m_gameplayFlowMachine.RequestState(endState);
        }

        private void OnDestroy()
        {
            m_controls.Pause.Toggle.started -= HandlePauseToggleStarted;
            
            m_controls.Disable();
            m_controls.Dispose();
            m_controls = null;
        }
    }
}