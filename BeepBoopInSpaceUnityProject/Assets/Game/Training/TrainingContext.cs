using System.Collections;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.Levels._0_Core;
using UnityEngine;

namespace Game.Training
{
    public class TrainingContext : AManager<TrainingContext>
    {
        [field: SerializeField]
        public CurrentLevelInfoManager CurrentLevelInfoManager { get; private set; }

        protected override IEnumerator Initialize()
        {
            DontDestroyOnLoad(gameObject);
            yield break;
        }
    }
}
