using Game.ArchitectureTools.Manager;
using Game.Data.Enums;
using NaughtyAttributes;
using UnityEngine;

namespace Game.Gameplay.Items
{
    public class Item : MonoBehaviour
    {
        public string id;
        public int instanceId;
        public new string name;
        public Sprite icon;
        public int rank;
        public ESoundMaterial soundMaterial;
        public EItemSource source;

        #if UNITY_EDITOR
        [Button("Generate ID")]
        private void GenerateID()
        {
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
            id = System.Guid.NewGuid().ToString();
        }
        #endif
        
        public void Start()
        {
            instanceId = ItemsManager.Instance.GetNextItemInstanceId();
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}
