using Game.UITools.Screens;

namespace Game.MainMenu
{
    /// <summary>
    /// A main menu screen: its state activates it, which enables its GameObject and clones a fresh visual tree from
    /// the UXML; the screen binds its elements in HandleActivation. Focus, navigation, sounds, Cancel and input
    /// indications come from AUIToolkitScreen.
    /// </summary>
    public abstract class AMainMenuScreen : AUIToolkitScreen
    {
        public bool IsActivated { get; private set; }

        public void Activate()
        {
            if (IsActivated)
                return;
            IsActivated = true;
            // Shown without having been activated (left enabled in the scene): a fresh tree, bound this time.
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Deactivate(bool force = false)
        {
            if (!IsActivated && !force)
                return;
            IsActivated = false;
            // A screen that isn't shown has no tree to hide: it still gets its HandleDeactivation, as states force
            // their screen off when they initialize.
            if (!IsShown)
                HandleDeactivation();
            gameObject.SetActive(false);
        }

        protected sealed override void HandleShown()
        {
            // Only once its state has activated it: before that, the screen may not have its dependencies.
            if (IsActivated)
                HandleActivation();
        }

        protected sealed override void HandleHidden()
        {
            HandleDeactivation();
        }

        protected virtual void HandleActivation()
        {}

        protected virtual void HandleDeactivation()
        {}
    }
}
