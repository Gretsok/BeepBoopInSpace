using System.Collections;
using Game.ArchitectureTools.Manager;
using Game.Global.PlayerManagement;
using Game.Global.Settings;
using Game.Global.Save;
using Game.Global.SFXManagement;
using UnityEngine;

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

        protected override IEnumerator Initialize()
        {
            DontDestroyOnLoad(gameObject);
            SaveManager.LoadProfile(_ =>
            {
                SettingsManager.Initialize(SaveManager);
            });
            
            AudioManager.Initialize();
            PlayerManager.Initialize();
            
            yield break;
        }
    }
}
