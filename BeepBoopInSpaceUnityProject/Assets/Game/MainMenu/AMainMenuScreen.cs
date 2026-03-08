using UnityEngine;

namespace Game.MainMenu
{
    public abstract class AMainMenuScreen : MonoBehaviour
    {
       public bool IsActivated { get; private set; }

        public void Activate()
        {
            if (IsActivated)
                return;
            IsActivated = true;
            HandleActivation();
        }

        protected virtual void HandleActivation()
        {
            gameObject.SetActive(true);
        }

        public void Deactivate(bool force = false)
        {
            if (!IsActivated && !force)
                return;
            IsActivated = false;
            HandleDeactivation();
        }

        protected virtual void HandleDeactivation()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Deactivate();
        }
    }
}