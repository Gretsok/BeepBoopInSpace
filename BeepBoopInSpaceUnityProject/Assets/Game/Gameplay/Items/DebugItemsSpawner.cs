using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Game.Data.Item
{
    public class DebugItemsSpawner : MonoBehaviour
    {
        [SerializeField] private float itemSpacing = 1f;
        [SerializeField] private List<Item> items = new List<Item>();

        private List<Item> m_instantiatedItems = new List<Item>();
        
        [Button]
        private void DisplayItems()
        {
            Vector3 pos = Vector3.zero;
            foreach (var item in items)
            {
                var newItem = Instantiate(item, pos, Quaternion.identity);
                pos += Vector3.right * itemSpacing;
                
                m_instantiatedItems.Add(newItem);
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [Button]
        private void CleanItems()
        {
            for (int i = m_instantiatedItems.Count - 1; i >= 0; --i)
            {
                DestroyImmediate(m_instantiatedItems[i].gameObject);
            }
            m_instantiatedItems.Clear();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
