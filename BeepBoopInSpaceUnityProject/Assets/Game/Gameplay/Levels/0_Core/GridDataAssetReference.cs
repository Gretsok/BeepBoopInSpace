using System;
using Game.Gameplay.GridSystem;
using UnityEngine.AddressableAssets;

namespace Game.Gameplay.Levels._0_Core
{
    [Serializable]
    public class GridDataAssetReference : AssetReferenceT<GridDataAsset>
    {
        public GridDataAssetReference(string guid) : base(guid)
        {
        }
    }
}
