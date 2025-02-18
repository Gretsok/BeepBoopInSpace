using Game.Characters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.PlayerManagement
{
    [RequireComponent(typeof(PlayerInput))]
    public class AbstractPlayer : MonoBehaviour
    {
        public PlayerInput PlayerInput => GetComponent<PlayerInput>();
        
        [field: SerializeField]
        public CharacterData CharacterData { get; private set; }

        public void SetCharacterData(CharacterData characterData)
        {
            CharacterData = characterData;
        }
    }
}