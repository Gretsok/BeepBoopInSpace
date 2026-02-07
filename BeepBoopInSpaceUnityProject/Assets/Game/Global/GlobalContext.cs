using Game.ArchitectureTools.Manager;
using Game.Global.Settings;
using Game.Global.Save;
using UnityEngine;

namespace Game.Global
{
    public class GlobalContext : AManager<GlobalContext>
    {
        [field: SerializeField]
        public SaveManager SaveManager { get; private set; }
        [field: SerializeField]
        public SettingsManager SettingsManager { get; private set; }

        private void Start()
        {
            SaveManager.LoadProfile((result) =>
            {
                SettingsManager.Initialize(SaveManager);
            });
        }
    }
}
