using Game.Gameplay.InteractionSystem;
using UnityEngine;

namespace Game.Gameplay.HoldingSystem
{
    public class HolderInteractableComponent : MonoBehaviour
    {
        private Holder m_holder;
        private Interactable m_interactable;
        private InteractionPanelInteractableComponent m_panelInteractableComponent;

        private void Awake()
        {
            m_holder = GetComponent<Holder>();
            m_interactable = GetComponent<Interactable>();
            m_panelInteractableComponent = GetComponent<InteractionPanelInteractableComponent>();

            m_interactable.OnInteract += HandleInteract;
            m_holder.OnCurrentHoldableChanged += HandleCurrentHoldableChanged;
            HandleCurrentHoldableChanged(m_holder, m_holder.CurrentHoldable);
        }

        private void Start()
        {
            m_holder.Activate();
        }

        private void HandleCurrentHoldableChanged(Holder arg1, Holdable arg2)
        {
            if (!m_panelInteractableComponent)
                return;

            m_panelInteractableComponent.InteractionText = arg2 != null ? "Grab" : "Store";
            m_panelInteractableComponent.ForceRefresh();
        }

        private void HandleInteract(Interactable interactable, Interactor interactor)
        {
            var interactorHolder = interactor.GetComponent<HolderReferencer>().Holder;
            var localHoldingObject = m_holder.CurrentHoldable;
            var interactorHoldingObject = interactorHolder.CurrentHoldable;

            if (!localHoldingObject && !interactorHoldingObject)
                return;
            
            if (localHoldingObject && interactorHoldingObject)
                return;

            if (interactorHoldingObject)
            {
                interactorHolder.TryToReleaseCurrentHoldable();
                m_holder.TryToHoldHoldable(interactorHoldingObject);
            }
            else
            {
                interactorHolder.TryToHoldHoldable(localHoldingObject);
            }
        }
    }
}