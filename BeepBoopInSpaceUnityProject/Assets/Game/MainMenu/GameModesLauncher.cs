using System;
using Game.Gameplay.Levels._0_Core;
using Game.Global;
using Game.LoadingScreen;
using Game.MainMenu.CharacterManagement;
using Game.Online;
using Game.Training;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Game.MainMenu
{
    public class GameModesLauncher : MonoBehaviour
    {
        [SerializeField]
        private AssetReference m_tournamentBootScene;
        [SerializeField]
        private TrainingContextAssetReference m_trainingContextAsset;
        
        private bool m_loadingTournament;

        [Button]
        public void LaunchTournament()
        {
            // The hub stays focused while the boot scene loads: a second press would boot a second tournament.
            if (m_loadingTournament) return;
            m_loadingTournament = true;
            PrepareBots();
            LoadingScreenManager.Instance?.ShowLoadingScreen();
            _ = Addressables.LoadSceneAsync(m_tournamentBootScene, LoadSceneMode.Single);
        }

        private bool m_loadingTraining = false;
        public async void LaunchTraining(LevelDataAsset levelDataAsset)
        {
            try
            {
                if (m_loadingTraining) return;
                m_loadingTraining = true;
                PrepareBots();
                LoadingScreenManager.Instance?.ShowLoadingScreen();
                await m_trainingContextAsset.InstantiateAsync().Task;
                TrainingContext.RegisterPostInitializationCallback(trainingContext =>
                {
                    trainingContext.CurrentLevelInfoManager.SetCurrentLevelDataAsset(levelDataAsset);
                    Addressables.LoadSceneAsync(levelDataAsset.GameModeScene, LoadSceneMode.Single);
                    
                    m_loadingTraining = false;
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // Online, a room plays with its players only.
        private static void PrepareBots()
        {
            if (!Netplay.IsOnline)
                GlobalContext.Instance.PlayerManager.PrepareBots(CharacterBankManager.Instance.Characters);
        }
    }
}
