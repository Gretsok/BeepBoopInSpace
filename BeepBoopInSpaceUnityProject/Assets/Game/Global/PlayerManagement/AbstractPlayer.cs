using System.Linq;
using Game.Characters;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Game.Global.PlayerManagement
{
    [RequireComponent(typeof(PlayerInput))]
    public class AbstractPlayer : MonoBehaviour
    {
        public PlayerInput PlayerInput => GetComponent<PlayerInput>();
        
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
    }
}