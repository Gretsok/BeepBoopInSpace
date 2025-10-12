using System;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Game.Gameplay.GlobalGameplayData
{
    [CreateAssetMenu(fileName = "{Name} - GlobalGameplayDataManager", menuName = "Game/Gameplay/GlobalGameplayDataAsset")]
    public class GlobalGameplayDataAsset : ScriptableObject
    {
        [Serializable]
        [Flags]
        public enum EResurrectionPlacement
        {
            Closest = 1,
            Random = 2, 
            Checkpoint = 4
        }
        
        [field: Header("Resurrection placement")]
        [field: SerializeField]
        public EResurrectionPlacement ResurrectionPlacementOnValidCell { get; private set; }
        [field: SerializeField]
        public EResurrectionPlacement ResurrectionPlacementOnInvalidCell { get; private set; }

        public GlobalGameplayData CreateAndGetData()
        {
            GlobalGameplayData globalGameplayData = new GlobalGameplayData();
            {
                var matching = Enum.GetValues(typeof(EResurrectionPlacement))
                    .Cast<EResurrectionPlacement>()
                    .Where(p => ResurrectionPlacementOnValidCell.HasFlag(p)) // or use HasFlag in .NET4
                    .ToArray();
                globalGameplayData.ResurrectionPlacementOnValidCell = matching[UnityEngine.Random.Range(0, matching.Length)];
            }
            {
                var matching = Enum.GetValues(typeof(EResurrectionPlacement))
                    .Cast<EResurrectionPlacement>()
                    .Where(p => ResurrectionPlacementOnInvalidCell.HasFlag(p)) // or use HasFlag in .NET4
                    .ToArray();
                globalGameplayData.ResurrectionPlacementOnInvalidCell = matching[UnityEngine.Random.Range(0, matching.Length)];
            }
            return globalGameplayData;
        }
    }

    public struct GlobalGameplayData
    {
        public GlobalGameplayDataAsset.EResurrectionPlacement ResurrectionPlacementOnValidCell;
        public GlobalGameplayDataAsset.EResurrectionPlacement ResurrectionPlacementOnInvalidCell;
    }
}
