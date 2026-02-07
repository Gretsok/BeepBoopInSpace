using System.Collections.Generic;
using NaughtyAttributes;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.MainMenu.ZoneManagement
{
    public class CameraZone : MonoBehaviour
    {
        [field: SerializeField]
        public CinemachineCamera CinemachineCamera { get; private set; }
        [SerializeField]
        private List<GameObject> m_objectsOnlyActiveWhenZoneIsActive = new List<GameObject>();

        [Button]
        public void ActivateZone()
        {
            CinemachineCamera.gameObject.SetActive(true);
            m_objectsOnlyActiveWhenZoneIsActive.ForEach(go => go.SetActive(true));
        }

        [Button]
        public void DeactivateZone()
        {
            CinemachineCamera.gameObject.SetActive(false);
            m_objectsOnlyActiveWhenZoneIsActive.ForEach(go => go.SetActive(false));
        }
    }
}
