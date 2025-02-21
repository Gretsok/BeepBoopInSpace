using Game.ArchitectureTools.Manager;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.Gameplay.CameraManagement
{
    public class CameraManager : AManager<CameraManager>
    {
        [field: SerializeField] 
        public CinemachineCamera GameplayCamera { get; private set; }
        [field: SerializeField] 
        public CinemachineCamera GlobalViewResultsCamera { get; private set; }

        public void SwitchToGameplayCamera()
        {
            GlobalViewResultsCamera.gameObject.SetActive(false);
            GameplayCamera.gameObject.SetActive(true);
        }

        public void SwitchToGlobalViewResultsCamera()
        {
            GameplayCamera.gameObject.SetActive(false);
            GlobalViewResultsCamera.gameObject.SetActive(true);
        }

        public void StopAllManagedCameras()
        {
            GlobalViewResultsCamera.gameObject.SetActive(false);
            GameplayCamera.gameObject.SetActive(false);
        }
    }
}