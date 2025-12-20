using System;
using System.Collections;
using UnityEngine;

namespace Game.Gameplay.GameModes
{
    public class AObjectiveManager : MonoBehaviour
    {
        private void Awake()
        {
            StartCoroutine(Initialize_Internal());
        }

        public bool IsInitialized { get; private set; }
        public event Action<AObjectiveManager> OnInitialized;
        private IEnumerator Initialize_Internal()
        {
            yield return Initialize();
            IsInitialized = true;
            
            OnInitialized?.Invoke(this);
            OnInitialized = null;
        }

        protected virtual IEnumerator Initialize()
        {
            yield break;
        }
    }
}
