using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Game.MainMenu.Online
{
    /// <summary>
    /// The code of a room, typed in a row of boxes, one character each. Gamepad: the d-pad moves between the boxes,
    /// up and down change the selected one, A goes to the next. Keyboard: letters and digits fill the boxes, Backspace
    /// erases, Ctrl+V pastes a code.
    /// </summary>
    public class RoomCodeField
    {
        public const int k_Length = 6;
        private const string k_Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string k_SlotClass = "online__code-slot";
        private const string k_EmptySlotClass = "online__code-slot--empty";
        private const char k_Empty = ' ';

        private readonly Button[] m_slots;
        private readonly char[] m_code = Enumerable.Repeat(k_Empty, k_Length).ToArray();
        private readonly Func<Button> m_afterLastSlot;

        public bool IsComplete => m_code.All(character => character != k_Empty);
        public string Code => new string(m_code).Trim();
        public Button FirstSlot => m_slots[0];

        public event Action OnChanged;

        /// <param name="row">Holds the boxes, buttons of the "online__code-slot" class.</param>
        /// <param name="afterLastSlot">Where the focus goes once the last box is filled.</param>
        public RoomCodeField(VisualElement row, Func<Button> afterLastSlot)
        {
            m_afterLastSlot = afterLastSlot;
            m_slots = row.Query<Button>(className: k_SlotClass).ToList().ToArray();
            for (int i = 0; i < m_slots.Length; i++)
            {
                var index = i;
                var slot = m_slots[i];
                // Styled by MainMenu.uss alone, without the default theme's button style.
                slot.RemoveFromClassList(Button.ussClassName);
                // Up and down change the character instead of moving the focus.
                slot.RegisterCallback<NavigationMoveEvent>(evt =>
                {
                    var step = evt.direction switch
                    {
                        NavigationMoveEvent.Direction.Up => -1,
                        NavigationMoveEvent.Direction.Down => 1,
                        _ => 0,
                    };
                    if (step == 0)
                        return;
                    Cycle(index, step);
                    evt.StopPropagation();
                    slot.focusController?.IgnoreEvent(evt);
                }, TrickleDown.TrickleDown);
                slot.clicked += () => FocusAfter(index);
            }
            Refresh();
        }

        /// <summary>Follows the keyboard while the field is shown: call it from the screen's update.</summary>
        public void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (keyboard.backspaceKey.wasPressedThisFrame)
                Erase();
            else if (keyboard.vKey.wasPressedThisFrame && keyboard.ctrlKey.isPressed)
                Paste(GUIUtility.systemCopyBuffer);
        }

        /// <summary>A character typed on the keyboard (Keyboard.onTextInput).</summary>
        public void Type(char character)
        {
            character = char.ToUpperInvariant(character);
            if (k_Characters.IndexOf(character) < 0)
                return;
            var index = FocusedIndex;
            if (index < 0)
                index = Array.IndexOf(m_code, k_Empty);
            if (index < 0)
                return;
            m_code[index] = character;
            Refresh();
            FocusAfter(index);
        }

        public void Paste(string text)
        {
            var characters = (text ?? string.Empty).ToUpperInvariant().Where(character => k_Characters.IndexOf(character) >= 0).Take(k_Length).ToArray();
            if (characters.Length == 0)
                return;
            for (int i = 0; i < k_Length; i++)
                m_code[i] = i < characters.Length ? characters[i] : k_Empty;
            Refresh();
            FocusAfter(Mathf.Min(characters.Length, k_Length) - 1);
        }

        public void Clear()
        {
            for (int i = 0; i < k_Length; i++)
                m_code[i] = k_Empty;
            Refresh();
        }

        private int FocusedIndex => Array.FindIndex(m_slots, slot => slot.focusController?.focusedElement == slot);

        private void Erase()
        {
            var index = FocusedIndex;
            if (index < 0)
                return;
            // An empty box: erases the one before.
            if (m_code[index] == k_Empty && index > 0)
                index--;
            m_code[index] = k_Empty;
            Refresh();
            m_slots[index].Focus();
        }

        private void Cycle(int index, int step)
        {
            var current = k_Characters.IndexOf(m_code[index]);
            var next = current < 0
                ? step > 0 ? 0 : k_Characters.Length - 1
                : (current + step + k_Characters.Length) % k_Characters.Length;
            m_code[index] = k_Characters[next];
            Refresh();
        }

        private void FocusAfter(int index)
        {
            if (index + 1 < m_slots.Length)
                m_slots[index + 1].Focus();
            else
                m_afterLastSlot()?.Focus();
        }

        private void Refresh()
        {
            for (int i = 0; i < m_slots.Length; i++)
            {
                m_slots[i].text = m_code[i] == k_Empty ? "·" : m_code[i].ToString();
                m_slots[i].EnableInClassList(k_EmptySlotClass, m_code[i] == k_Empty);
            }
            OnChanged?.Invoke();
        }
    }
}
