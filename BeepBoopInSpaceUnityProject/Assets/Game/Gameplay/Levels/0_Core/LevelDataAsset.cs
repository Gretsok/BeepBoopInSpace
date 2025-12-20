using System.Collections.Generic;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core;
using Game.Gameplay.GameModes;
using Game.Gameplay.GlobalGameplayData;
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
        public AssetReference EnvironmentScene { get; private set; }
        [field: SerializeField]
        public AssetReference LevelScene { get; private set; }
        [field: SerializeField]
        public List<AssetReference> AdditionalScenes { get; private set; }
        
        [field: SerializeField]
        public ObjectiveManagerAssetReference ObjectiveManagerPrefab { get; private set; }
        
        [field: SerializeField]
        public SpecialActionAssetReference SpecialActionPrefab { get; private set; }
        
        [field: SerializeField]
        public List<AssetReference> AdditionalSystemsToInstantiate { get; private set; }
        
        [field: SerializeField]
        public GlobalGameplayDataAsset GlobalGameplayDataAsset { get; private set; }
        
        // TO ADD : Available special actions with weights.
    }
}
