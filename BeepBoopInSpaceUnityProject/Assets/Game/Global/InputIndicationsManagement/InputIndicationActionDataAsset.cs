using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Game.Global.InputIndicationsManagement
{
    /// <summary>
    /// Represents an action with all the different input sprites matched to their device.
    /// </summary>
    [CreateAssetMenu(fileName = "{name} - " + nameof(InputSpriteDataAsset), menuName = "Game/Global/InputIndicationsManagement/InputIndicationActionDataAsset")]
    public class InputIndicationActionDataAsset : ScriptableObject
    {
        [field: FormerlySerializedAs("<KBM_InputSpriteDataAsset>k__BackingField")]
        [field: SerializeField]
        public InputSpriteDataAsset KBM_Qwerty_InputSpriteDataAsset { get; set; }
        [field: SerializeField]
        public InputSpriteDataAsset KBM_Azerty_InputSpriteDataAsset { get; set; }
        [field: SerializeField]
        public InputSpriteDataAsset XboxGamepad_InputSpriteDataAsset { get; set; }
        [field: SerializeField]
        public InputSpriteDataAsset PSGamepad_InputSpriteDataAsset { get; set; }

        public Sprite GetSpriteFor(string controlScheme)
        {
            switch (controlScheme)
            {
                case "KBM":
                    if (Keyboard.current.keyboardLayout == "AZERTY")
                        return KBM_Azerty_InputSpriteDataAsset.InputSprite;
                    return KBM_Qwerty_InputSpriteDataAsset.InputSprite;
                case "XboxGamepad":
                    return XboxGamepad_InputSpriteDataAsset.InputSprite;
                case "PSGamepad":
                    return PSGamepad_InputSpriteDataAsset.InputSprite;
                default:
                    return null;
            }
        }
    }
}
