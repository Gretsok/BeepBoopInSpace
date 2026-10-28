using System.Collections;
using Game.ArchitectureTools.Manager;
using UnityEngine;

namespace Game.Gameplay.Flows.End
{
    [RequireComponent(typeof(EndState))]
    public class EndStateGrabber : AManager<EndStateGrabber>
    {
        public EndState EndState { get; private set; }

        protected override IEnumerator Initialize()
        {
            EndState = GetComponent<EndState>();
            yield break;
        }
    }
}