using DG.Tweening;
using Game.ArchitectureTools.ActivatableSystem;
using UnityEngine;

namespace Game.Gameplay.HoldingSystem
{
    public class Holder : AActivatableSystem
    {
        [field: SerializeField]
        public bool CanTakeHoldableFromOtherHolder { get; set; } = false;
        [field: SerializeField]
        public Transform AttachPoint { get; private set; } = null;
        
        public Holdable CurrentHoldable { get; private set; }


        protected override void HandleDeactivation()
        {
            base.HandleDeactivation();
            
            TryToReleaseCurrentHoldable();
        }

        public bool CanHold(Holdable holdable)
        {
            return (CanTakeHoldableFromOtherHolder || !holdable.IsBeingHeld) && CurrentHoldable == null && IsActivated;
        }
        
        public void TryToHoldHoldable(Holdable holdable)
        {
            if (!CanHold(holdable))
                return;
            
            HoldHoldable_Internal(holdable);
        }

        private void HoldHoldable_Internal(Holdable holdable)
        {
            CurrentHoldable = holdable;
            holdable.NotifyStartBeingHeld();
            holdable.transform.parent = AttachPoint;
            holdable.transform.DOKill();
            holdable.transform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.InOutSine);
            holdable.transform.DOLocalRotate(Vector3.zero, 0.3f).SetEase(Ease.InOutSine);
            holdable.OnStartBeingHeld += HandleHoldableStartBeingHeld;
        }

        private void HandleHoldableStartBeingHeld(Holdable obj)
        {
            obj.OnStartBeingHeld -= HandleHoldableStartBeingHeld;

            if (obj != CurrentHoldable)
                return;
            
            CurrentHoldable.transform.parent = null;
            CurrentHoldable.transform.DOKill();
            CurrentHoldable = null;
        }

        public void TryToReleaseCurrentHoldable()
        {
            if (CurrentHoldable == null)
                return;
            
            CurrentHoldable.transform.parent = null;
            CurrentHoldable.OnStartBeingHeld -= HandleHoldableStartBeingHeld;

            CurrentHoldable.NotifyStopBeingHeld();
            CurrentHoldable.transform.DOKill();
            CurrentHoldable = null;
        }
    }
}