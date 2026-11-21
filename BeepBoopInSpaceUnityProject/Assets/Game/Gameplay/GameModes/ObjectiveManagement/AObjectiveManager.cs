using System;
using System.Collections;
using UnityEngine;

namespace Game.Gameplay.GameModes.ObjectiveManagement
{
    public class AObjectiveManager : MonoBehaviour
    {
        [field: SerializeField]
        public string ObjectiveLabelKey { get; private set; }

        /// <summary>What the scores count, over the score board of the announcements (French, translated by Loc).</summary>
        public virtual string ScoreCaption => "Score";

        /// <summary>The score that wins the game, shown as as many pips on the score board; 0 shows the number instead.</summary>
        public virtual int ScoreGoal => 0;

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
