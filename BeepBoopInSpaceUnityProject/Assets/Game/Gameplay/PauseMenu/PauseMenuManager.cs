using System;
using DG.Tweening;
using Game.ArchitectureTools.FlowMachine;
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
            if (IsPaused)
                return;
            if (!m_gameplayFlowMachine.CurrentState.GetComponent<CanBePausedTagStateComponent>())
                return;
            
            Time.timeScale = 0;
            DOTween.defaultTimeScaleIndependent = true;
            
            m_gameplayFlowMachine.Pause(this);
            OnPause?.Invoke(this);
            
            IsPaused = true;
        }

        public Action<PauseMenuManager> OnResume;
        public void Resume()
        {
            if (!IsPaused)
                return;
            
            Time.timeScale = 1;
            DOTween.defaultTimeScaleIndependent = false;
            
            m_gameplayFlowMachine.Unpause(this);
            OnResume?.Invoke(this);
            
            IsPaused = false;
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