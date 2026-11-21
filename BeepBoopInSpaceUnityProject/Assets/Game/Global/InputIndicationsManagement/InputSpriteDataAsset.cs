using UnityEngine;

namespace Game.Global.InputIndicationsManagement
{
    /// <summary>
    /// Holds a sprite representing a specific button on a specific device.
    /// </summary>
    [CreateAssetMenu(fileName = "{name} - " + nameof(InputSpriteDataAsset), menuName = "Game/Global/InputIndicationsManagement/InputSpriteDataAsset")]
    public class InputSpriteDataAsset : ScriptableObject
    {
        [field: SerializeField]
        public Sprite InputSprite { get; private set; }
    }
}
