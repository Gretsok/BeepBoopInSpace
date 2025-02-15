using System;
using Game.ArchitectureTools.ActivatableSystem;
using UnityEngine;

namespace Game.Gameplay.InteractionSystem
{
    public class Interactor : AActivatableSystem
    {
        [field: SerializeField]
        public float SightDistance { get; set; } = 3f;
        [field: SerializeField]
        public LayerMask SightLayerMask { get; private set; }

        public Transform InteractionSource { get; private set; }

        public delegate bool AdditionalInteractionCondition();
        private AdditionalInteractionCondition m_additionalInteractionCondition;
        public void SetDependencies(Transform interactionSource, AdditionalInteractionCondition additionalInteractionCondition)
        {
            InteractionSource = interactionSource;
            m_additionalInteractionCondition = additionalInteractionCondition;
        }
        
        public Interactable CurrentInteractableInSight { get; private set; }

        public void Interact()
        {
            if (!IsActivated)
                return;
            
            CurrentInteractableInSight?.TriggerInteraction();
        }

        protected override void HandleDeactivation()
        {
            base.HandleDeactivation();
            CurrentInteractableInSight = null;
        }

        private void FixedUpdate()
        {
            if (!IsActivated)
                return;

            var canInteract = m_additionalInteractionCondition?.Invoke();

            if (canInteract.HasValue && !canInteract.Value)
            {
                CurrentInteractableInSight?.NotifyEndBeingLookedAt();
                CurrentInteractableInSight = null;
                
                return;
            }
            
            var oldInteractableInSight = CurrentInteractableInSight;
            if (Physics.Raycast(InteractionSource.position, InteractionSource.forward, out RaycastHit hit,
                    SightDistance, SightLayerMask))
            {
                CurrentInteractableInSight = hit.collider.GetComponent<Interactable>();
            }
            else
            {
                CurrentInteractableInSight = null;
            }

            if (oldInteractableInSight != CurrentInteractableInSight)
            {
                oldInteractableInSight?.NotifyEndBeingLookedAt();
                CurrentInteractableInSight?.NotifyStartBeingLookedAt();
            }
        }
    }
}