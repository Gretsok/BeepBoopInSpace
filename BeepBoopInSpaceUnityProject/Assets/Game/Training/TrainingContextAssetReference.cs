using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Training
{
    [Serializable]
    public class TrainingContextAssetReference : AssetReferenceT<GameObject>
    {
        public TrainingContextAssetReference(string guid) : base(guid)
        {
        }
        
        public override bool ValidateAsset(string mainAssetPath)
        {
#if UNITY_EDITOR

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(mainAssetPath);
            return base.ValidateAsset(mainAssetPath) && prefab && prefab.GetComponent<TrainingContext>();
#else
            return false;
#endif
        }
    }
}
