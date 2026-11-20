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
                    if (IsAzertyKeyboard())
                        return KBM_Azerty_InputSpriteDataAsset.InputSprite;
                    return KBM_Qwerty_InputSpriteDataAsset.InputSprite;
                case "XboxGamepad":
                case "OtherGamepad":
                    return XboxGamepad_InputSpriteDataAsset.InputSprite;
                case "PSGamepad":
                    return PSGamepad_InputSpriteDataAsset.InputSprite;
                default:
                    return null;
            }
        }

        // Layout names are the platform's ("French" on Windows), never "AZERTY": the key that writes Q on a QWERTY
        // keyboard writes A on an AZERTY one.
        private static bool IsAzertyKeyboard()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && string.Equals(keyboard.qKey.displayName, "A", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
