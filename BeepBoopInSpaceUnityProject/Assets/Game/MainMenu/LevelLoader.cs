using Game.ArchitectureTools.Manager;
using Game.Gameplay.Levels._0_Core;
using Game.Gameplay.LoadingScreen;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Game.MainMenu
{
    public class LevelLoader : AManager<LevelLoader>
    {
        [SerializeField]
        private LevelDataAsset m_levelDataAsset;

        public void LoadLevel()
        {
            LoadingScreenManager.Instance?.ShowLoadingScreen();
            var op = Addressables.LoadSceneAsync(m_levelDataAsset.GameModeScene, LoadSceneMode.Single);
        }
    }
}
