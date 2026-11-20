using System.Linq;
using Game.Characters;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Game.Global.PlayerManagement
{
    /// <summary>
    /// A player of the game. A local player has a PlayerInput, from the PlayerInputManager. A player of another machine of
    /// an online room has none: the online manager creates it, its inputs reach the host through the network. Nor does a
    /// bot, the computer plays its robot.
    /// </summary>
    public class AbstractPlayer : MonoBehaviour
    {
        public PlayerInput PlayerInput => GetComponent<PlayerInput>();

        /// <summary>A player of another machine of the online room, or a bot: no buttons on this machine.</summary>
        public bool IsRemote => !PlayerInput;

        /// <summary>The computer plays this player's robot.</summary>
        public bool IsBot { get; set; }

        private int m_number;
        /// <summary>The player's badge number (J1 to J4): their rank in the online room, else in the local players.</summary>
        public int Number => m_number > 0 ? m_number : PlayerInput ? PlayerInput.playerIndex + 1 : 0;

        /// <summary>What the player's badge says.</summary>
        public string BadgeLabel => IsBot ? Localization.Loc.BotBadge : Localization.Loc.PlayerBadge(Number);
        
        [field: SerializeField]
        public CharacterDataAsset CharacterDataAsset { get; private set; }

        public bool Initialized { get; private set; } = false;

        private void Awake()
        {
            MakeKBMCompletePairingIfNeeded();
            Initialized = true;
        }

        private void MakeKBMCompletePairingIfNeeded()
        {
            var playerInput = PlayerInput;
            if (!playerInput)
                return;

            var mouse = InputSystem.GetDevice<Mouse>();
            var keyboard = InputSystem.GetDevice<Keyboard>();
            
            if (keyboard == null || mouse == null)
                return;

            if (playerInput.devices.Contains(keyboard) && !playerInput.devices.Contains(mouse))
            {
                InputUser.PerformPairingWithDevice(mouse, user: playerInput.user);
                return;
            }
            
            if (playerInput.devices.Contains(mouse) && !playerInput.devices.Contains(keyboard))
            {
                InputUser.PerformPairingWithDevice(keyboard, user: playerInput.user);
                return;
            }
        }

        public void SetCharacterData(CharacterDataAsset characterDataAsset)
        {
            CharacterDataAsset = characterDataAsset;
        }

        /// <summary>The badge number in an online room, 0 to go back to the local one.</summary>
        public void SetNumber(int number)
        {
            m_number = number;
        }
    }
}