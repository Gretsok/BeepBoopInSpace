using Game.Gameplay.Flows;
using UnityEngine;

namespace Game.Gameplay.GameModes.Meteorites
{
    public class MeteoriteLoadStateComponent : MonoBehaviour
    {
        private LoadState m_loadState;

        private void Awake()
        {
            m_loadState = GetComponent<LoadState>();
            m_loadState.AddLoadingRequirement(IsMeteoritesManagerInitialized);
        }

        private bool IsMeteoritesManagerInitialized()
        {
            return MeteoritesManager.Instance && MeteoritesManager.Instance.IsInitialized;
        }
    }
}
