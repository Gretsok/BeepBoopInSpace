using UnityEngine;

namespace Game.Gameplay.CharactersManagement
{
    public class CharacterDestructionHandler : MonoBehaviour
    {
        private Transform m_model;
        private CharacterVFXsHandler m_VFXsHandler;

        public void SetDependencies(Transform model, CharacterVFXsHandler vfxHandler)
        {
            m_model = model;
            m_VFXsHandler = vfxHandler;
        }

        [SerializeField]
        private float m_destructionDuration = 2f;
        
        public bool IsDestroyed { get; private set; }

        public void Destroy()
        {
            IsDestroyed = true;
            
            m_model.gameObject.SetActive(false);
            
            m_VFXsHandler.PlayExplosionEffect();
            
            Invoke(nameof(Respawn), m_destructionDuration);
        }

        public void Respawn()
        {
            IsDestroyed = false;
            
            m_model.gameObject.SetActive(true);
            m_VFXsHandler.PlaySpawnEffect();
        }
    }
}