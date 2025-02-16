using System;
using System.Collections;
using DG.Tweening;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.BeepBoopCharacter.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.PauseMenu
{
    public class PauseMenuManager : AManager<PauseMenuManager>
    {
        [field: SerializeField]
        public PlayerInputController PlayerInputController { get; private set; }
        
        public bool IsPaused { get; private set; }
        
        private PauseControls m_controls;
        protected override IEnumerator Initialize()
        {
            m_controls = new PauseControls();
            m_controls.Enable();
            yield return null;
            
            m_controls.Pause.Toggle.started += HandlePauseToggleStarted;
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
            
            Time.timeScale = 0;
            DOTween.defaultTimeScaleIndependent = true;
            
            PlayerInputController.Disable();
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
            
            OnResume?.Invoke(this);
            PlayerInputController.Enable();
            
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