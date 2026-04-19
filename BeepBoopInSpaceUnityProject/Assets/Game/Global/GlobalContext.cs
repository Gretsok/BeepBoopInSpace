using System.Collections;
using Game.ArchitectureTools.Manager;
using Game.Global.NavigationAuthority;
using Game.Global.PlayerManagement;
using Game.Global.Settings;
using Game.Global.Save;
using Game.Global.SFXManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Global
{
    public class GlobalContext : AManager<GlobalContext>
    {
        [field: SerializeField]
        public SaveManager SaveManager { get; private set; }
        [field: SerializeField]
        public SettingsManager SettingsManager { get; private set; }
        [field: SerializeField]
        public AudioManager AudioManager { get; private set; }
        [field: SerializeField]
        public PlayerManager PlayerManager { get; private set; }
        [field: SerializeField]
        public EventSystem EventSystem { get; private set; }
        [field: SerializeField]
        public NavigationAuthorityManager NavigationAuthorityManager { get; private set; }

        protected override IEnumerator Initialize()
        {
            DontDestroyOnLoad(gameObject);
            bool settingsManagerInitialized = false;
            SaveManager.LoadProfile(_ =>
            {
                SettingsManager.Initialize(SaveManager);
                settingsManagerInitialized = true;
            });
            
            AudioManager.Initialize();
            PlayerManager.Initialize();
            
            NavigationAuthorityManager.SetDependencies(PlayerManager, EventSystem);
            
            yield return new WaitUntil(() => settingsManagerInitialized);
        }
    }
}
