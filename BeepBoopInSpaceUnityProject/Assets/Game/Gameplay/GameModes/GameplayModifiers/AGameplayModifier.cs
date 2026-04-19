using System.Collections;
using UnityEngine;

namespace Game.Gameplay.GameModes.GameplayModifiers
{
    public class AGameplayModifier : MonoBehaviour
    {
        public bool IsInitialized { get; private set; }
        
        public IEnumerator Initialize()
        {
            yield return HandleInitializationRoutine();
            IsInitialized = true;
        }

        protected virtual IEnumerator HandleInitializationRoutine()
        {
            yield break;
        }
    }
}
