using System;
using UnityEngine.AddressableAssets;

namespace Game.Gameplay.GridSystem
{
    [Serializable]
    public class CellsDictionaryDataAssetReference : AssetReferenceT<CellsDictionaryDataAsset>
    {
        public CellsDictionaryDataAssetReference(string guid) : base(guid)
        {
        }
    }
}
