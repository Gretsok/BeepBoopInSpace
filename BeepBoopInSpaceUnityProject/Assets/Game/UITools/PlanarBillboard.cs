using UnityEngine;

namespace Game.UITools
{
    public class PlanarBillboard : MonoBehaviour
    {
        public enum EBillboardType
        {
            LookAtCamera = 0,
            CopyCameraRotation = 1
        }

        [SerializeField]
        private Transform m_cameraTransform;
        [SerializeField]
        private EBillboardType m_billboardType = EBillboardType.CopyCameraRotation;
        [SerializeField]
        private bool m_reverseForward = false;

        protected virtual Transform FetchCameraTransform()
        {
            return Camera.main?.transform;
        }

        private void LateUpdate()
        {
            if (!m_cameraTransform)
                m_cameraTransform = FetchCameraTransform();
            if (!m_cameraTransform)
                return;

            Quaternion newRotation = Quaternion.identity;
            if (m_billboardType == EBillboardType.CopyCameraRotation)
            {
                newRotation = m_cameraTransform.rotation;
            }
            else if (m_billboardType == EBillboardType.LookAtCamera)
            {
                newRotation = Quaternion.LookRotation(transform.position - m_cameraTransform.position);
            }
            
            var newForward = Vector3.ProjectOnPlane(newRotation * Vector3.forward, Vector3.up);
            transform.rotation = Quaternion.LookRotation(m_reverseForward ? -newForward : newForward, Vector3.up);
        }
    }
}
