using System.Collections;
using Game.ArchitectureTools.Manager;
using UnityEngine;

namespace Game.Gameplay.LoadingScreen
{
    public class LoadingScreenManager : AManager<LoadingScreenManager>
    {
        protected override IEnumerator Initialize()
        {
            DontDestroyOnLoad(this.gameObject);
            yield break;
        }

        [SerializeField]
        private GameObject m_loadingScreen;

        public void ShowLoadingScreen()
        {
            m_loadingScreen.SetActive(true);    
        }

        public void HideLoadingScreen()
        {
            m_loadingScreen.SetActive(false);
        }
    }
}