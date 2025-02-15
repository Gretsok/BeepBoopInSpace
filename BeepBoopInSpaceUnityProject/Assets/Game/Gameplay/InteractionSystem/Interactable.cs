using System;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Gameplay.InteractionSystem
{
    public class Interactable : MonoBehaviour
    {
        [SerializeField] 
        private UnityEvent m_onStartBeingLookedAt;
        public Action<Interactable> OnStartBeingLookedAt;
        public void NotifyStartBeingLookedAt()
        {
            m_onStartBeingLookedAt?.Invoke();
            OnStartBeingLookedAt?.Invoke(this);
        }

        [SerializeField]
        private UnityEvent m_onStopBeingLookedAt;
        public Action<Interactable> OnStopBeingLookedAt;
        public void NotifyEndBeingLookedAt()
        {
            m_onStopBeingLookedAt?.Invoke();
            OnStopBeingLookedAt?.Invoke(this);
        }

        [SerializeField]
        private UnityEvent m_onInteract;
        public Action<Interactable> OnInteract;
        public void TriggerInteraction()
        {
            m_onInteract?.Invoke();
            OnInteract?.Invoke(this);
        }
    }
}