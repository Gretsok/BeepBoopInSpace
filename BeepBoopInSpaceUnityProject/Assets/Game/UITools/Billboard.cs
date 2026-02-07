using UnityEngine;

namespace Game.UITools
{
    [ExecuteAlways]
    public class Billboard : MonoBehaviour
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

        protected virtual Transform FetchCameraTransform()
        {
            return Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (!m_cameraTransform)
                m_cameraTransform = FetchCameraTransform();
            if (!m_cameraTransform)
                return;

            if (m_billboardType == EBillboardType.CopyCameraRotation)
            {
                transform.rotation = m_cameraTransform.rotation;
            }
            else if (m_billboardType == EBillboardType.LookAtCamera)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - m_cameraTransform.position);
            }
        }
    }
}
