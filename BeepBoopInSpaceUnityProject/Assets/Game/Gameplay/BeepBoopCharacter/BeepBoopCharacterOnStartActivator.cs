using UnityEngine;

namespace Game.Gameplay.BeepBoopCharacter
{
    public class BeepBoopCharacterOnStartActivator : MonoBehaviour
    {
        [field: SerializeField]
        public BeepBoopCharacter Character { get; private set; }

        private void Start()
        {
            Character.Activate();
        }
    }
}
