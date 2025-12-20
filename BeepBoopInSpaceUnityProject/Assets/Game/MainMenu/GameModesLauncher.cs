using Game.ArchitectureTools.Manager;
using Game.Gameplay.LoadingScreen;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Game.MainMenu
{
    public class GameModesLauncher : AManager<GameModesLauncher>
    {
        [SerializeField]
        private AssetReference m_tournamentBootScene;
        
        [Button]
        public void LaunchTournament()
        {
            LoadingScreenManager.Instance?.ShowLoadingScreen();
            var op = Addressables.LoadSceneAsync(m_tournamentBootScene, LoadSceneMode.Single);
        }
    }
}
