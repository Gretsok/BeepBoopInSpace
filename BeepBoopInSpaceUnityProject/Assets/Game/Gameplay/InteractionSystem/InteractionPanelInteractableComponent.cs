using UnityEngine;

namespace Game.Gameplay.InteractionSystem
{
    public class InteractionPanelInteractableComponent : MonoBehaviour
    {
        [field: SerializeField]
        public string InteractionText { get; private set; }
        
        private Interactable m_interactable;
        private void Awake()
        {
            m_interactable = GetComponent<Interactable>();
            m_interactable.OnStartBeingLookedAt += HandleStartBeingLookedAt;
            m_interactable.OnStopBeingLookedAt += HandleStopBeingLookedAt;
        }

        private void HandleStartBeingLookedAt(Interactable obj)
        {
            InteractionPanel.Instance.RegisterComponent(this);
        }

        private void HandleStopBeingLookedAt(Interactable obj)
        {
            InteractionPanel.Instance.UnregisterComponent(this);
        }
    }
}
