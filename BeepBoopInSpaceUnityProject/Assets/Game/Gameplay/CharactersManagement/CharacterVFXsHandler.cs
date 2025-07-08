using UnityEngine;
using UnityEngine.VFX;

namespace Game.Gameplay.CharactersManagement
{
    public class CharacterVFXsHandler : MonoBehaviour
    {
        [SerializeField]
        private VisualEffect m_spawnVisualEffect;
        
        public void PlaySpawnEffect()
        {
            m_spawnVisualEffect.Play();
        }
    }
}