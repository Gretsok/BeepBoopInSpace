using System;
using System.Collections;
using UnityEngine;

namespace Game.ArchitectureTools.Manager
{
    public abstract class AManager<TManagerType> : MonoBehaviour where TManagerType : AManager<TManagerType>
    {
        public static TManagerType Instance { get; private set; }
        public static bool IsInitialized { get; private set; } 
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this as TManagerType;
            StartCoroutine(Initialize_Internal());
        }

        private static Action<TManagerType> s_onInitializedCallbacks;
        public static void RegisterPostInitializationCallback(Action<TManagerType> callback)
        {
            if (Instance && IsInitialized)
            {
                callback?.Invoke(Instance);
                return;
            }
            s_onInitializedCallbacks += callback;
        }
        
        private IEnumerator Initialize_Internal()
        {
            yield return Initialize();
            IsInitialized = true;
            
            s_onInitializedCallbacks?.Invoke(Instance);
            s_onInitializedCallbacks = null;
        }

        protected virtual IEnumerator Initialize()
        {
            yield break;
        }
    }
}