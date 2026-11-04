using System.Collections;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.Levels._0_Core;
using Game.Tournament.Config;
using Game.Tournament.Flow;
using Game.Tournament.Results;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Tournament
{
    public class TournamentContext : AManager<TournamentContext>
    {
        // The config is in the Addressables group of the Tournament folder.
        private const string k_ConfigAddress = "Assets/Game/Tournament/Config/TournamentConfigDataAsset.asset";

        [field: Header("Data")]
        [field: SerializeField]
        public TournamentConfigDataAsset ConfigDataAsset { get; private set; }

        [field: Header("Managers")]
        [field: SerializeField]
        public TournamentFlowManager FlowManager { get; private set; }
        [field: SerializeField]
        public TournamentResultsManager ResultsManager { get; private set; }
        [field: SerializeField]
        public CurrentLevelInfoManager CurrentLevelInfoManager { get; private set; }

        // In a build, the bundle of the config and its levels goes away with the tournament's scenes, and destroys them
        // even though this context still points at them: the next levels wouldn't load. Held until the tournament ends.
        private AsyncOperationHandle<TournamentConfigDataAsset> m_configHandle;

        protected override IEnumerator Initialize()
        {
            DontDestroyOnLoad(gameObject);
            m_configHandle = Addressables.LoadAssetAsync<TournamentConfigDataAsset>(k_ConfigAddress);
            yield return m_configHandle;
            ResultsManager.InjectDependencies(ConfigDataAsset);
            FlowManager.InjectDependencies(ResultsManager, ConfigDataAsset, CurrentLevelInfoManager);
        }

        private void OnDestroy()
        {
            if (m_configHandle.IsValid())
                Addressables.Release(m_configHandle);
        }

        /// <summary>Ends the tournament before it has a champion, without its results: the players leave for the main menu.</summary>
        public void Abandon()
        {
            Debug.Log("[Tournament] Abandoned.");
            FlowManager.Abandon();
            Destroy(gameObject);
        }
    }
}
