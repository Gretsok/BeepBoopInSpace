using System;
using Game.Global.Save.Serialization;
using UnityEngine;

namespace Game.Global.Save
{
    public class SaveManager : MonoBehaviour
    {
        [SerializeField]
        private ASerializer m_serializer;
        
        public SaveProfile Profile { get; private set; }

        public void LoadProfile(Action<ERequestResult> onCompleted = null)
        {
            m_serializer.LoadProfile((result, profile) =>
            {
                Profile = profile;
                onCompleted?.Invoke(result);
            });
        }

        public void SaveProfile(Action<ERequestResult> onCompleted = null)
        {
            m_serializer.SaveProfile(Profile, (result) =>
            {
                onCompleted?.Invoke(result);
            });
        }
    }
}
