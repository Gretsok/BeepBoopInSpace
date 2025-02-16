using UnityEngine;

namespace Game.Gameplay.InteractionSystem
{
    public class InteractableCollider : MonoBehaviour
    {
        [field: SerializeField]
        public Interactable Interactable { get; private set; }
    }
}