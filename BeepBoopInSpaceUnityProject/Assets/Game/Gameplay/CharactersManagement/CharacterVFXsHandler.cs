using UnityEngine;
using UnityEngine.VFX;

namespace Game.Gameplay.CharactersManagement
{
    public class CharacterVFXsHandler : MonoBehaviour
    {
        [SerializeField]
        private VisualEffect m_explosionVisualEffect;

        [SerializeField]
        private VisualEffect m_spawnVisualEffect;

        public void PlayExplosionEffect()
        {
            m_explosionVisualEffect.Play();
        }

        public void PlaySpawnEffect()
        {
            m_spawnVisualEffect.Play();
        }
    }
}