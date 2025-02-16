using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Gameplay.InteractionSystem
{
    public class Interactable : MonoBehaviour
    {
        public delegate bool InteractableCondition();

        private readonly List<InteractableCondition> m_conditions = new();

        public void AddCondition(InteractableCondition condition)
        {
            if (!m_conditions.Contains(condition))
                m_conditions.Add(condition);
        }

        public void RemoveCondition(InteractableCondition condition)
        {
            m_conditions.RemoveAll(c => c == condition);
        }

        public bool CanBeInteractedWith()
        {
            for (int i = 0; i < m_conditions.Count; i++)
            {
                var condition = m_conditions[i]?.Invoke();

                if (condition.HasValue && !condition.Value)
                {
                    return false;
                }
            }

            return true;
        }
        
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
            if (!CanBeInteractedWith())
                return;
            
            m_onInteract?.Invoke();
            OnInteract?.Invoke(this, interactor);
        }
    }
}