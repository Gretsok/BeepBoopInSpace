using Game.Gameplay.CharactersManagement.ReferencesHolding;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement.CollisionsHandling
{
    public class CharacterCollisionsHandler : MonoBehaviour
    {
        [field: SerializeField]
        public CharacterReferencesHolder ReferencesHolder { get; private set; }
    }
}
