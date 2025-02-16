using System;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Gameplay.InteractionSystem
{
    public class Interactable : MonoBehaviour
    {
        [SerializeField] 
        private UnityEvent m_onStartBeingLookedAt;
        public Action<Interactable, Interactor> OnStartBeingLookedAt;
        public void NotifyStartBeingLookedAt(Interactor interactor)
        {
            m_onStartBeingLookedAt?.Invoke();
            OnStartBeingLookedAt?.Invoke(this, interactor);
        }

        [SerializeField]
        private UnityEvent m_onStopBeingLookedAt;
        public Action<Interactable, Interactor> OnStopBeingLookedAt;
        public void NotifyEndBeingLookedAt(Interactor interactor)
        {
            m_onStopBeingLookedAt?.Invoke();
            OnStopBeingLookedAt?.Invoke(this, interactor);
        }

        [SerializeField]
        private UnityEvent m_onInteract;
        public Action<Interactable, Interactor> OnInteract;
        public void TriggerInteraction(Interactor interactor)
        {
            m_onInteract?.Invoke();
            OnInteract?.Invoke(this, interactor);
        }
    }
}