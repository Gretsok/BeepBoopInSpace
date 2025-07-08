using UnityEngine;

namespace Game.Characters
{
    [CreateAssetMenu(fileName = "{name} - CharacterData", menuName = "Game/Characters/CharacterData")]
    public class CharacterDataAsset : ScriptableObject
    {
        [field: SerializeField]
        public GameObject CharacterPrefab { get; private set; }
        [field: SerializeField]
        public string Name { get; private set; }
        [field: SerializeField]
        public Color CharacterColor { get; private set; }
        [field: SerializeField]
        public Sprite NameplateSprite { get; private set; }
    }
}