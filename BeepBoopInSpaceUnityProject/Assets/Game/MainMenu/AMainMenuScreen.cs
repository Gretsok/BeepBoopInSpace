using System;
using System.Collections.Generic;
using System.Linq;
using Game.Global;
using Game.Global.InputIndicationsManagement;
using Game.Global.PlayerManagement;
using Game.Global.SFXManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Game.MainMenu
{
    /// <summary>
    /// A main menu screen, drawn by the UIDocument next to it. Activating the screen enables its GameObject, which
    /// clones a fresh visual tree from the UXML: screens query and bind their elements in HandleActivation.
    /// Gamepads and keyboard move the focus through the EventSystem (only the main player's devices once players
    /// have joined) and the mouse focuses what it hovers, so every device shares the same highlighted element.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public abstract class AMainMenuScreen : MonoBehaviour
    {
        private const string k_BackGlyphName = "back-glyph";
        private const string k_FiligreeClass = "mm-filigree";

        [SerializeField]
        private AudioPlayer m_focusAudioPlayer;

        [SerializeField, Tooltip("Button shown by the \"back-glyph\" element of the screen's Retour legend, if it has one.")]
        private InputIndicationActionDataAsset m_backIndicationAction;

        private UIDocument m_document;
        private Focusable m_silentFocusTarget;
        private GameObject m_panelGameObject;
        private readonly List<InputIndicationView> m_indications = new();

        public bool IsActivated { get; private set; }

        protected VisualElement Root => m_document ? m_document.rootVisualElement : null;
        public Focusable FocusedElement => Root?.focusController?.focusedElement;

        /// <summary>The player who has the navigation, null before anybody joins.</summary>
        protected static AbstractPlayer MainPlayer
        {
            get
            {
                var globalContext = GlobalContext.Instance;
                if (!globalContext || !globalContext.PlayerManager)
                    return null;
                var navigationAuthority = globalContext.NavigationAuthorityManager;
                var mainPlayerId = navigationAuthority ? navigationAuthority.MainPlayerID : 0;
                return globalContext.PlayerManager.Players.FirstOrDefault(player => player && player.PlayerInput.playerIndex == mainPlayerId);
            }
        }

        /// <summary>Focused when the screen opens, and again whenever nothing has the focus. Null leaves the screen unfocused.</summary>
        protected Focusable DefaultFocus { get; set; }

        private void Awake()
        {
            m_document = GetComponent<UIDocument>();
        }

        public void Activate()
        {
            if (IsActivated)
                return;
            IsActivated = true;
            gameObject.SetActive(true);
            SetUpRoot();
            if (m_backIndicationAction && Root.Q(k_BackGlyphName) != null)
                AddIndication(k_BackGlyphName, m_backIndicationAction);
            HandleActivation();
            KeepFiligreesAligned();
            FocusSilently(DefaultFocus);
        }

        protected virtual void HandleActivation()
        {}

        public void Deactivate(bool force = false)
        {
            if (!IsActivated && !force)
                return;
            IsActivated = false;
            HandleDeactivation();
            foreach (var indication in m_indications)
                indication.Dispose();
            m_indications.Clear();
            DefaultFocus = null;
            gameObject.SetActive(false);
        }

        protected virtual void HandleDeactivation()
        {}

        /// <summary>Cancel (B, Circle or Escape) pressed by the player who has the navigation.</summary>
        protected virtual void HandleCancel()
        {}

        private void OnDestroy()
        {
            Deactivate();
        }

        private void Update()
        {
            if (!IsActivated)
                return;
            // A focused element that gets hidden or disabled loses the focus: give it back, or gamepads and keyboards
            // would have nothing to navigate from.
            if (DefaultFocus != null && FocusedElement == null)
                FocusSilently(DefaultFocus);
            SelectPanelInEventSystem();
        }

        // The EventSystem sends gamepad and keyboard navigation to its selected GameObject only. For the focused element
        // to get it, that must be the GameObject UI Toolkit creates for the panel, which appears a frame after the
        // first document, too late for the first focus to select it.
        private void SelectPanelInEventSystem()
        {
            var eventSystem = EventSystem.current;
            if (!eventSystem || FocusedElement == null)
                return;
            if (!m_panelGameObject)
            {
                var handler = FindObjectsByType<PanelEventHandler>(FindObjectsInactive.Exclude).FirstOrDefault(h => h.panel == Root.panel);
                m_panelGameObject = handler ? handler.gameObject : null;
            }
            if (m_panelGameObject && eventSystem.currentSelectedGameObject != m_panelGameObject)
                eventSystem.SetSelectedGameObject(m_panelGameObject);
        }

        private void SetUpRoot()
        {
            var root = Root;
            // The document root fills the panel, so screens lay out their elements against the whole screen.
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.pickingMode = PickingMode.Ignore;

            root.RegisterCallback<PointerDownEvent>(HandlePointerDown, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerOverEvent>(HandlePointerOver);
            root.RegisterCallback<FocusInEvent>(HandleFocusIn);
            root.RegisterCallback<NavigationMoveEvent>(HandleNavigationMove);
            root.RegisterCallback<NavigationCancelEvent>(_ => HandleCancel());
        }

        // The dark shapes show the background's pattern: a texture that covers the screen, wherever the shape is, so it
        // moves whenever the shape or one of its parents does.
        private void KeepFiligreesAligned()
        {
            Root.Query(className: k_FiligreeClass).ForEach(filigree =>
            {
                for (var element = filigree.hierarchy.parent; element != null && element != Root; element = element.hierarchy.parent)
                    element.RegisterCallback<GeometryChangedEvent>(_ => AlignFiligree(filigree));
            });
        }

        private static void AlignFiligree(VisualElement filigree)
        {
            // Layout positions, not world bounds: these ignore the ongoing scale and rotation transitions and tweens.
            var position = Vector2.zero;
            for (var element = filigree.hierarchy.parent; element != null; element = element.hierarchy.parent)
                position += element.layout.position;
            filigree.style.left = -position.x;
            filigree.style.top = -position.y;
        }

        protected T Query<T>(string name) where T : VisualElement
        {
            return Root.Q<T>(name) ?? throw new InvalidOperationException(
                $"{GetType().Name}: no {typeof(T).Name} named '{name}' in {m_document.visualTreeAsset.name}.");
        }

        /// <summary>Plays <paramref name="clickAudioPlayer"/> then runs <paramref name="onClicked"/> when the button is clicked or submitted.</summary>
        protected Button BindButton(string name, Action onClicked, AudioPlayer clickAudioPlayer = null)
        {
            var button = Query<Button>(name);
            // Menu buttons are styled by MainMenu.uss alone, without the default theme's button style.
            button.RemoveFromClassList(Button.ussClassName);
            button.clicked += () =>
            {
                if (clickAudioPlayer)
                    clickAudioPlayer.Play();
                onClicked();
            };
            return button;
        }

        /// <summary>Shows on the element the button of the action for the main player's device, until the screen closes.</summary>
        protected void AddIndication(string elementName, InputIndicationActionDataAsset actionDataAsset)
        {
            var indication = new InputIndicationView(Query<VisualElement>(elementName), actionDataAsset) { ShowsLikelyDeviceWithoutPlayer = true };
            indication.SetPlayer(MainPlayer);
            m_indications.Add(indication);
        }

        /// <summary>Makes the indications follow the main player's device again, after players joined or left.</summary>
        protected void RefreshIndications()
        {
            var mainPlayer = MainPlayer;
            foreach (var indication in m_indications)
                indication.SetPlayer(mainPlayer);
        }

        /// <summary>Focuses an element without the focus sound, for focus changes the player didn't make.</summary>
        protected void FocusSilently(Focusable focusable)
        {
            if (focusable == null || FocusedElement == focusable || !focusable.canGrabFocus)
                return;
            m_silentFocusTarget = focusable;
            focusable.Focus();
            if (FocusedElement != focusable)
                m_silentFocusTarget = null;
        }

        // Clicking the background keeps the focus, so gamepad and keyboard navigation can go on from it.
        private void HandlePointerDown(PointerDownEvent evt)
        {
            if (FindFocusable(evt.target as VisualElement) == null)
                Root.focusController?.IgnoreEvent(evt);
        }

        // The mouse selects what it hovers, like the gamepad does: one highlighted element whatever the device.
        private void HandlePointerOver(PointerOverEvent evt)
        {
            var focusable = FindFocusable(evt.target as VisualElement);
            if (focusable != null && focusable != FocusedElement)
                focusable.Focus();
        }

        private void HandleFocusIn(FocusInEvent evt)
        {
            if (m_silentFocusTarget is VisualElement silentTarget && evt.target is VisualElement target
                && (target == silentTarget || silentTarget.Contains(target)))
            {
                m_silentFocusTarget = null;
                return;
            }
            if (m_focusAudioPlayer)
                m_focusAudioPlayer.Play();
        }

        // Directional navigation like uGUI's automatic one: the closest element ahead, even diagonally. UI Toolkit's own
        // only looks straight ahead, which leaves buttons in the screen corners out of the d-pad's reach.
        private void HandleNavigationMove(NavigationMoveEvent evt)
        {
            var direction = evt.direction switch
            {
                NavigationMoveEvent.Direction.Left => Vector2.left,
                NavigationMoveEvent.Direction.Right => Vector2.right,
                NavigationMoveEvent.Direction.Up => Vector2.down, // the panel's y axis points down
                NavigationMoveEvent.Direction.Down => Vector2.up,
                _ => Vector2.zero,
            };
            var from = FindNavigationTarget(evt.target as VisualElement);
            if (direction == Vector2.zero || from == null)
                return;

            var bounds = from.worldBound;
            var edge = bounds.center + Vector2.Scale(bounds.size, direction * 0.5f);
            VisualElement best = null;
            var bestScore = 0f;
            Root.Query<VisualElement>().ForEach(candidate =>
            {
                if (candidate == from || !IsNavigationTarget(candidate) || !candidate.canGrabFocus)
                    return;
                var toCandidate = candidate.worldBound.center - edge;
                var ahead = Vector2.Dot(direction, toCandidate);
                if (ahead <= 0f)
                    return;
                var score = ahead / toCandidate.sqrMagnitude;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            });

            best?.Focus();
            evt.StopPropagation();
            Root.focusController?.IgnoreEvent(evt);
        }

        // What the d-pad moves between: buttons (and level cards) and sliders, not the parts they are made of nor scrollbars.
        private static bool IsNavigationTarget(VisualElement element) =>
            (element is Button or BaseSlider<int> or BaseSlider<float>) && element.GetFirstAncestorOfType<Scroller>() == null;

        private static VisualElement FindNavigationTarget(VisualElement element)
        {
            for (; element != null; element = element.parent)
            {
                if (IsNavigationTarget(element))
                    return element;
            }
            return null;
        }

        private static Focusable FindFocusable(VisualElement element)
        {
            for (; element != null; element = element.parent)
            {
                if (element.focusable && element.canGrabFocus)
                    return element;
            }
            return null;
        }
    }
}
