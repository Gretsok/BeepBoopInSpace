using Game.ArchitectureTools.Manager;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.Gameplay.CameraManagement
{
    public class CameraManager : AManager<CameraManager>
    {
        private const float k_ResultsAspect = 16f / 9f;

        private float m_resultsFieldOfView;

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
            KeepTheResultsInView();
            GlobalViewResultsCamera.gameObject.SetActive(true);
        }

        // The robots and their scores fill the width of a 16:9 view: a narrower screen sees as wide, with more sky and ground.
        private void KeepTheResultsInView()
        {
            var lens = GlobalViewResultsCamera.Lens;
            if (m_resultsFieldOfView <= 0f)
                m_resultsFieldOfView = lens.FieldOfView;

            var aspect = Camera.main ? Camera.main.aspect : k_ResultsAspect;
            var heightForTheWidth = Mathf.Tan(m_resultsFieldOfView * 0.5f * Mathf.Deg2Rad) * k_ResultsAspect / Mathf.Min(aspect, k_ResultsAspect);
            lens.FieldOfView = 2f * Mathf.Atan(heightForTheWidth) * Mathf.Rad2Deg;
            GlobalViewResultsCamera.Lens = lens;
        }

        public void StopAllManagedCameras()
        {
            GlobalViewResultsCamera.gameObject.SetActive(false);
            GameplayCamera.gameObject.SetActive(false);
        }
    }
}