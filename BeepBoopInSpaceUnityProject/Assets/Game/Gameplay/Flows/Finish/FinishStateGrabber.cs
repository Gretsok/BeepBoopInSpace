using System.Collections;
using Game.ArchitectureTools.Manager;
using UnityEngine;

namespace Game.Gameplay.Flows.Finish
{
    [RequireComponent(typeof(FinishState))]
    public class FinishStateGrabber : AManager<FinishStateGrabber>
    {
        public FinishState FinishState { get; private set; }

        protected override IEnumerator Initialize()
        {
            FinishState = GetComponent<FinishState>();
            yield break;
        }
    }
}
