using System;
using UnityEngine;

namespace Game.Global.Save.Serialization
{
    public abstract class ASerializer : MonoBehaviour
    {
        public abstract void SaveProfile(SaveProfile profile, Action<ERequestResult> onCompleted = null);
        public abstract void LoadProfile(Action<ERequestResult, SaveProfile> onResult = null);
    }
}
