using System.Collections.Generic;
using Game.Gameplay.GridSystem;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Gameplay.Levels._0_Core
{
    [CreateAssetMenu(fileName = "{LevelName} - LevelDataAsset", menuName = "Game/Gameplay/Levels/Level Data Asset")]
    public class LevelDataAsset : ScriptableObject
    {
        [field: SerializeField]
        public string NameKey { get; private set; }

        [field: SerializeField]
        public AssetReference GameModeScene { get; private set; }
        [field: SerializeField]
        public List<AssetReference> AdditionalScenes { get; private set; }
        [field: SerializeField]
        public int AdditionalSceneIndexToActivate { get; private set; }
        
        [field: SerializeField]
        public GridDataAssetReference GridDataAsset { get; private set; }
        
        [field: SerializeField]
        public CellsDictionaryDataAssetReference CellsDictionaryDataAsset { get; private set; }
        
        // TO ADD : Available special actions with weights.
    }
}
