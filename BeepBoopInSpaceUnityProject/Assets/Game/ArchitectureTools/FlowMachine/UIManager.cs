using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

namespace Game.ArchitectureTools.FlowMachine
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField]
        private List<Panel> m_registeredPanels = new List<Panel>();

        public T GetPanel<T>() where T : Panel
        {
            return m_registeredPanels.FirstOrDefault(panel => panel is T) as T;
        }
        
        #if UNITY_EDITOR
        [Button]
        private void FetchPanelsInCurrentScene()
        {
            var panels = FindObjectsByType<Panel>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .ToList().FindAll(panel => panel.gameObject.scene == gameObject.scene);
            
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] == null || m_registeredPanels.Contains(panels[i]))
                    continue;
                m_registeredPanels.Add(panels[i]);
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        #endif
    }
}
