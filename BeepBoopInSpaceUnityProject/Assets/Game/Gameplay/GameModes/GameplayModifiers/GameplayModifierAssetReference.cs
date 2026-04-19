using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Gameplay.GameModes.GameplayModifiers
{
    [Serializable]
    public class GameplayModifierAssetReference : AssetReferenceT<GameObject>
    {
        public GameplayModifierAssetReference(string guid) : base(guid)
        {
        }

        public override bool ValidateAsset(string mainAssetPath)
        {
#if UNITY_EDITOR

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(mainAssetPath);
            return base.ValidateAsset(mainAssetPath) && prefab && prefab.GetComponent<AGameplayModifier>();
#else
            return false;
#endif
        }
    }
}
