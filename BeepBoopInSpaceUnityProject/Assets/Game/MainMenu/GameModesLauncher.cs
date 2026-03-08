using System;
using Game.Gameplay.Levels._0_Core;
using Game.Gameplay.LoadingScreen;
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
        
        [Button]
        public void LaunchTournament()
        {
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
    }
}
