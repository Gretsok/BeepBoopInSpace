using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UITools
{
    public class SelectOnHover : MonoBehaviour,
        IPointerEnterHandler
    {
        [SerializeField]
        private GameObject m_target;

        public void SetTarget(GameObject target)
        {
            m_target = target;
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
            #endif
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(m_target ?? gameObject, eventData);
        }
    }
}