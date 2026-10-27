using Game.Global.PlayerManagement;
using Game.MainMenu.CharacterManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.MainMenu.CharacterSelection
{
    /// <summary>
    /// A player's inputs in the character selection: move the cursor in the grid, pick at random, call (the robot pops),
    /// get ready, and cancel being ready. The main player's Cancel goes through the screen, like every menu's.
    /// </summary>
    public class CharacterSelectionPlayerController : MonoBehaviour
    {
        private const float k_MoveThreshold = 0.5f;
        private const float k_RepeatDelay = 0.4f;
        private const float k_RepeatInterval = 0.15f;

        private AbstractPlayer m_player;
        private CharacterWidget m_widget;
        private CharacterRoster m_roster;
        private bool m_handlesBack;

        private InputAction m_moveAction;
        private InputAction m_readyAction;
        private InputAction m_backAction;
        private InputAction m_randomAction;
        private InputAction m_callAction;

        // The button a player joins with must not also make them ready: buttons count once all are released.
        private bool m_isArmed;
        private Vector2Int m_heldDirection;
        private float m_nextMoveTime;

        public void SetDependencies(AbstractPlayer player, CharacterWidget widget, CharacterRoster roster, bool handlesBack)
        {
            m_player = player;
            m_widget = widget;
            m_roster = roster;
            m_handlesBack = handlesBack;

            var actionMap = m_player.PlayerInput.actions.FindActionMap("MainMenu");
            m_moveAction = actionMap.FindAction("Move");
            m_readyAction = actionMap.FindAction("Ready");
            m_backAction = actionMap.FindAction("Back");
            m_randomAction = actionMap.FindAction("Random");
            m_callAction = actionMap.FindAction("Call");
        }

        public void Activate()
        {
            m_readyAction.performed += HandleReadyPerformed;
            m_backAction.performed += HandleBackPerformed;
            m_randomAction.performed += HandleRandomPerformed;
            m_callAction.performed += HandleCallPerformed;
        }

        public void Deactivate()
        {
            m_readyAction.performed -= HandleReadyPerformed;
            m_backAction.performed -= HandleBackPerformed;
            m_randomAction.performed -= HandleRandomPerformed;
            m_callAction.performed -= HandleCallPerformed;
        }

        private void Update()
        {
            if (!m_player)
                return;
            if (!m_isArmed)
                m_isArmed = !m_readyAction.IsPressed() && !m_backAction.IsPressed() && !m_randomAction.IsPressed();

            var direction = ToDirection(m_moveAction.ReadValue<Vector2>());
            if (direction != m_heldDirection)
            {
                m_heldDirection = direction;
                m_nextMoveTime = Time.unscaledTime + k_RepeatDelay;
                Move(direction);
            }
            else if (direction != Vector2Int.zero && Time.unscaledTime >= m_nextMoveTime)
            {
                m_nextMoveTime = Time.unscaledTime + k_RepeatInterval;
                Move(direction);
            }
        }

        // The dominant axis of the stick or d-pad, y going down like the grid's rows.
        private static Vector2Int ToDirection(Vector2 value)
        {
            if (value.magnitude < k_MoveThreshold)
                return Vector2Int.zero;
            return Mathf.Abs(value.x) >= Mathf.Abs(value.y)
                ? new Vector2Int(value.x > 0f ? 1 : -1, 0)
                : new Vector2Int(0, value.y > 0f ? -1 : 1);
        }

        private void Move(Vector2Int direction)
        {
            if (direction == Vector2Int.zero || m_widget.IsReady)
                return;
            m_widget.Hover(m_roster.GetNeighbour(m_widget.CharacterDataAsset, direction));
        }

        private void HandleReadyPerformed(InputAction.CallbackContext context)
        {
            if (m_isArmed)
                m_widget.TrySetReady();
        }

        private void HandleBackPerformed(InputAction.CallbackContext context)
        {
            if (m_isArmed && m_handlesBack)
                m_widget.CancelReady();
        }

        private void HandleRandomPerformed(InputAction.CallbackContext context)
        {
            if (m_isArmed && !m_widget.IsReady)
                m_widget.Hover(m_roster.PickRandom(m_widget));
        }

        private void HandleCallPerformed(InputAction.CallbackContext context)
        {
            m_widget.Pop();
        }
    }
}
