using System;
using UnityEngine;

namespace Game.ArchitectureTools.ActivatableSystem
{
    public class AActivatableSystem : MonoBehaviour
    {
        public bool IsActivated { get; private set; } = false;

        public Action<AActivatableSystem> OnActivated;
        public void Activate()
        {
            if (IsActivated)
                return;
            
            HandleActivation();
            IsActivated = true;
            
            OnActivated?.Invoke(this);
        }
        
        protected virtual void HandleActivation()
        {}

        public Action<AActivatableSystem> OnDeactivated;
        public void Deactivate()
        {
            if (!IsActivated)
                return;
            
            HandleDeactivation();
            IsActivated = false;
            
            OnDeactivated?.Invoke(this);
        }
        
        protected virtual void HandleDeactivation()
        {}
    }
}