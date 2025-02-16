using System;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Gameplay.HoldingSystem
{
    public class Holdable : MonoBehaviour
    {
        public bool IsBeingHeld { get; private set; }


        [SerializeField] 
        private UnityEvent m_onStartBeingHeld;
        public Action<Holdable> OnStartBeingHeld { get; set; }
        public void NotifyStartBeingHeld()
        {
            IsBeingHeld = true;
            
            m_onStartBeingHeld?.Invoke();
            OnStartBeingHeld?.Invoke(this);
        }

        [SerializeField]
        private UnityEvent m_onStopBeingHeld;
        public Action<Holdable> OnStopBeingHeld { get; set; }
        public void NotifyStopBeingHeld()
        {
            IsBeingHeld = false;
            
            m_onStopBeingHeld?.Invoke();
            OnStopBeingHeld?.Invoke(this);
        }
    }
}