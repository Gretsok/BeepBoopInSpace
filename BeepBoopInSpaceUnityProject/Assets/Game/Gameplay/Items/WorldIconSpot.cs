using NaughtyAttributes;
using UnityEngine;

namespace Game.Gameplay.Items
{
    public class WorldIconSpot : MonoBehaviour
    {
        [SerializeField] 
        private Item m_itemPrefab;

        [HideInInspector, SerializeField] 
        private Item m_currentItemOnSpot;

        private void Start()
        {
            RefreshSpot();
        }

        private void OnValidate()
        {
            RefreshSpot();
        }

        [Button]
        public void RefreshSpot()
        {
            if (m_currentItemOnSpot == null)
            {
                if (Application.isPlaying)
                    m_currentItemOnSpot = Instantiate(m_itemPrefab, transform);
                else
                    m_currentItemOnSpot = UnityEditor.PrefabUtility.InstantiatePrefab(m_itemPrefab, transform) as Item;
            }
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
}