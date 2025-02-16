using Game.ArchitectureTools.Manager;
using Game.Data.Enums;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

namespace Game.Data.Item
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

        [Button("Generate ID")]
        private void GenerateID()
        {
            EditorUtility.SetDirty(gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
            id = System.Guid.NewGuid().ToString();
        }
        
        public void Start()
        {
            instanceId = ItemsManager.Instance.GetNextItemInstanceId();
        }
    }
}
