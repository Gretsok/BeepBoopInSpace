using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core
{
    [Serializable]
    public class SpecialActionAssetReference : AssetReferenceT<GameObject>
    {
        public SpecialActionAssetReference(string guid) : base(guid)
        {
        }

        /*public override bool ValidateAsset(Object obj)
        {
            
            return base.ValidateAsset(obj) && ((GameObject)obj).GetComponent<SpecialAction>();
        }*/

        public override bool ValidateAsset(string mainAssetPath)
        {
#if UNITY_EDITOR

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(mainAssetPath);
            return base.ValidateAsset(mainAssetPath) && prefab && prefab.GetComponent<SpecialAction>();
#else
            return false;
#endif
        }
    }
}
