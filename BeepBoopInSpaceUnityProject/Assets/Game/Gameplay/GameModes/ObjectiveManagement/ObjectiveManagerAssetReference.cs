using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Gameplay.GameModes.ObjectiveManagement
{
    [Serializable]
    public class ObjectiveManagerAssetReference : AssetReferenceT<GameObject>
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public ObjectiveManagerAssetReference(string guid) : base(guid)
        {
        }
        public override bool ValidateAsset(string mainAssetPath)
        {
#if UNITY_EDITOR

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(mainAssetPath);
            return base.ValidateAsset(mainAssetPath) && prefab && prefab.GetComponent<AObjectiveManager>();
#else
            return false;
#endif
        }
    }
}
