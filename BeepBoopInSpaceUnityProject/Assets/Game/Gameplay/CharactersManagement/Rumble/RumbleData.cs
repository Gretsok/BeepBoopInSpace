using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.CharactersManagement.Rumble
{
    [CreateAssetMenu(fileName = "{Name} - RumbleData", menuName = "Game/Gameplay/CharactersManagement/RumbleData")]
    public class RumbleData : ScriptableObject
    {
        [field: SerializeField]
        public float RumbleDuration { get; private set; }
        
        [field: SerializeField]
        public Vector2 LowFrequenciesLimits { get; private set; }
        [field: SerializeField]
        public AnimationCurve LowFrequenciesCurve { get; private set; }
        [field: SerializeField]
        public Vector2 HighFrequenciesLimits { get; private set; }
        [field: SerializeField]
        public AnimationCurve HighFrequenciesCurve { get; private set; }
        
        #if UNITY_EDITOR
        [Button]
        public void TestRumble()
        {
            var devices = InputSystem.devices;

            for (int i = 0; i < devices.Count; i++)
            {
                var device = devices[i] as Gamepad;
                if (device == null)
                    continue;
                
                Unity.EditorCoroutines.Editor.EditorCoroutineUtility
                    .StartCoroutineOwnerless(RumbleReaderUtils.RumbleRoutine(device, this, null));
            }
        }
        #endif
    }
}