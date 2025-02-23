using UnityEngine;

namespace StudioIcon
{
    public class DelayedActivator : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_objectToActivate;
    
        [SerializeField]
        private float m_delay = 3f;

        private void Start()
        {
            Invoke(nameof(Show), m_delay);
        }

        private void Show()
        {
            m_objectToActivate.SetActive(true);
        }
    }
}
