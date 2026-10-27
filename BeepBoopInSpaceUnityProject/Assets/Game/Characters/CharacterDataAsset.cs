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
        [field: SerializeField]
        public Sprite AvatarSprite { get; private set; }

        [field: Header("Character selection")]
        [field: SerializeField, Tooltip("Head shot in the character grid.")]
        public Texture2D SelectionIcon { get; private set; }
        [field: SerializeField, Tooltip("Lowers the icon in its grid cell (100x125 px), for icons with empty space under the robot.")]
        public float SelectionIconOffset { get; private set; }
        [field: SerializeField, Tooltip("Big picture in the player's circle. The icon stands in for it while it is missing.")]
        public Texture2D SelectionPreview { get; private set; }
        [field: SerializeField, Tooltip("Color of the player's circle and badges once the character is chosen.")]
        public Color SelectionColor { get; private set; } = new(0.16f, 0.58f, 0.78f);
    }
}
