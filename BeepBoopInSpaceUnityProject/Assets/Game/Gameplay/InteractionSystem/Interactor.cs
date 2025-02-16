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
        
        public delegate bool AdditionalInteractionConditionOnSpecificInteractable(Interactable interactable);
        private AdditionalInteractionConditionOnSpecificInteractable m_additionalInteractionConditionOnSpecificInteractable;
        
        public void SetDependencies(Transform interactionSource, 
            AdditionalInteractionCondition additionalInteractionCondition = null, 
            AdditionalInteractionConditionOnSpecificInteractable additionalInteractionConditionOnSpecificInteractable = null)
        {
            InteractionSource = interactionSource;
            m_additionalInteractionCondition = additionalInteractionCondition;
            m_additionalInteractionConditionOnSpecificInteractable = additionalInteractionConditionOnSpecificInteractable;
        }
        
        public Interactable CurrentInteractableInSight { get; private set; }

        public void Interact()
        {
            if (!IsActivated)
                return;
            
            CurrentInteractableInSight?.TriggerInteraction(this);
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
                CurrentInteractableInSight?.NotifyEndBeingLookedAt(this);
                CurrentInteractableInSight = null;
                
                return;
            }
            
            
            
            var oldInteractableInSight = CurrentInteractableInSight;

            CurrentInteractableInSight = GetCurrentInteractableInSight();

            if (CurrentInteractableInSight != null)
            {
                var canInteractWithThisInteractable = m_additionalInteractionConditionOnSpecificInteractable?.Invoke(CurrentInteractableInSight);
                if ((canInteractWithThisInteractable.HasValue && !canInteractWithThisInteractable.Value)
                    || !CurrentInteractableInSight.CanBeInteractedWith())
                {
                    CurrentInteractableInSight = null;
                }
                else
                {
                    CurrentInteractableInSight = CurrentInteractableInSight;
                }
            }
            
            if (oldInteractableInSight != CurrentInteractableInSight)
            {
                oldInteractableInSight?.NotifyEndBeingLookedAt(this);
                CurrentInteractableInSight?.NotifyStartBeingLookedAt(this);
            }
        }

        private Interactable GetCurrentInteractableInSight()
        {
            if (Physics.Raycast(InteractionSource.position, InteractionSource.forward, out RaycastHit hit,
                    SightDistance, SightLayerMask))
            {
                return hit.collider.GetComponent<Interactable>();
            }
            else
            {
                return null;
            }
        }
    }
}