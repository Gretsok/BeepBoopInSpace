using UnityEngine;

namespace Game.Gameplay.HoldingSystem
{
    public class RigidbodyHoldableComponent : MonoBehaviour
    {
        private Rigidbody m_rigidbody;
        private Holdable m_holdable;
        private void Awake()
        {
            m_holdable = GetComponent<Holdable>();
            m_rigidbody = GetComponent<Rigidbody>();

            m_holdable.OnStartBeingHeld += HandleStartBeingHeld;
            m_holdable.OnStopBeingHeld += HandleStopBeingHeld;
        }

        private void HandleStartBeingHeld(Holdable obj)
        {
            m_rigidbody.detectCollisions = false;
            m_rigidbody.isKinematic = true;
        }

        private void HandleStopBeingHeld(Holdable obj)
        {
            m_rigidbody.isKinematic = false;
            m_rigidbody.detectCollisions = true;

        }
    }
}