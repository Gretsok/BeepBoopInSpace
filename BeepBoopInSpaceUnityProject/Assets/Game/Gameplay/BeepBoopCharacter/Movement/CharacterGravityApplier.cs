using Game.ArchitectureTools.ActivatableSystem;
using UnityEngine;

namespace Game.Gameplay.BeepBoopCharacter.Movement
{
    public class CharacterGravityApplier : AActivatableSystem
    {
        [field: SerializeField]
        public float Gravity { get; set; } = 15f;
        public Rigidbody Rigidbody { get; private set; }

        public void SetDependencies(Rigidbody associatedRigidbody)
        {
            Rigidbody = associatedRigidbody;
        }

        private void FixedUpdate()
        {
            if (!IsActivated)
                return;
            
            Rigidbody.AddForce(Vector3.down * Gravity, ForceMode.Acceleration);
        }
    }
}