using UnityEngine;

namespace Game.Gameplay.Levels._0_Core
{
    [RequireComponent(typeof(CurrentLevelInfoManager))]
    public class CurrentLevelInfoManagerStandaloneSingleton : MonoBehaviour
    {
        public static CurrentLevelInfoManager Instance { get; private set; }
        
        public void Initialize()
        {
            Instance = GetComponent<CurrentLevelInfoManager>();
        }
    }
}
