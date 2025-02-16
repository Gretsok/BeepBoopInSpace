using Game.Gameplay.InteractionSystem;
using UnityEngine;

namespace Game.Gameplay.HoldingSystem
{
    public class HoldableInteractableComponent : MonoBehaviour
    {
        private Interactable m_interactable;
        private Holdable m_holdable;

        private void Awake()
        {
            m_interactable = GetComponent<Interactable>();
            m_holdable = GetComponent<Holdable>();

            m_interactable.OnInteract += HandleInteract;
        }

        private void HandleInteract(Interactable arg1, Interactor arg2)
        {
            if (arg2.TryGetComponent(out HolderReferencer holderReferencer))
            {
                holderReferencer.Holder.TryToHoldHoldable(m_holdable);
            }
        }
    }
}