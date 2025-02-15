using Game.ArchitectureTools.ActivatableSystem;
using Game.Gameplay.BeepBoopCharacter.Movement;
using Game.Gameplay.InteractionSystem;
using UnityEngine;

namespace Game.Gameplay.BeepBoopCharacter
{
    public class BeepBoopCharacter : AActivatableSystem
    {
        
        [field: Header("Internal components")]
        [field: SerializeField]
        public Rigidbody Rigidbody { get; private set; }
        [field: SerializeField]
        public Camera Camera { get; private set; }
        [field: SerializeField]
        public CharacterGravityApplier GravityApplier { get; private set; }
        [field: SerializeField]
        public CharacterGroundDetector GroundDetector { get; private set; }
        [field: SerializeField]
        public CharacterMovementController MovementController { get; private set; }
        [field: SerializeField]
        public CharacterRotationController RotationController { get; private set; }
        [field: SerializeField]
        public Interactor Interactor { get; private set; }


        private void Awake()
        {
            GravityApplier.SetDependencies(Rigidbody);
            GroundDetector.SetDependencies(transform);
            MovementController.SetDependencies(Rigidbody, GroundDetector);
            RotationController.SetDependencies(transform, Camera);
            Interactor.SetDependencies(Camera.transform, () => GroundDetector.IsGrounded);
        }

        protected override void HandleActivation()
        {
            base.HandleActivation();
            GravityApplier.Activate();
            GroundDetector.Activate();
            MovementController.Activate();
            RotationController.Activate();
            Interactor.Activate();
        }

        protected override void HandleDeactivation()
        {
            base.HandleDeactivation();
            GravityApplier.Deactivate();
            GroundDetector.Deactivate();
            MovementController.Deactivate();
            RotationController.Deactivate();
            Interactor.Deactivate();
        }
    }
}