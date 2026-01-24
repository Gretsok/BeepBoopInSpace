using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Tournament.Boot
{
    public class TournamentCloser : MonoBehaviour
    {
        [SerializeField]
        private AssetReference m_mainMenuSceneReference;

        private void Start()
        {
            Destroy(TournamentContext.Instance.gameObject);
            
            Addressables.LoadSceneAsync(m_mainMenuSceneReference);
        }
    }
}
