using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.PlayerManagement
{
    [RequireComponent(typeof(PlayerInput))]
    public class AbstractPlayer : MonoBehaviour
    {
        public PlayerInput PlayerInput => GetComponent<PlayerInput>();
    }
}