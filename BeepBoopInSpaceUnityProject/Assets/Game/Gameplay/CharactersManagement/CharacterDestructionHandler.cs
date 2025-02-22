using Game.Gameplay.CharactersManagement.Rumble;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement
{
    public class CharacterDestructionHandler : MonoBehaviour
    {
        private Transform m_model;
        private CharacterVFXsHandler m_VFXsHandler;
        private CharacterRumbleHandler m_rumbleHandler;
        private CharacterSFXsHandler m_SFXsHandler;

        public void SetDependencies(Transform model, CharacterVFXsHandler vfxHandler, 
            CharacterRumbleHandler rumbleHandler, CharacterSFXsHandler sfxHandler)
        {
            m_model = model;
            m_VFXsHandler = vfxHandler;
            m_rumbleHandler = rumbleHandler;
            m_SFXsHandler = sfxHandler;
        }

        [SerializeField]
        private float m_destructionDuration = 2f;
        
        public bool IsDestroyed { get; private set; }

        public void Destroy()
        {
            IsDestroyed = true;
            
            m_model.gameObject.SetActive(false);
            
            m_VFXsHandler.PlayExplosionEffect();
            m_rumbleHandler.PlayExplodedRumble();
            m_SFXsHandler.PlayExplosionAudio();
            
            Invoke(nameof(Respawn), m_destructionDuration);
        }

        public void Respawn()
        {
            IsDestroyed = false;
            
            m_model.gameObject.SetActive(true);
            m_VFXsHandler.PlaySpawnEffect();
            m_SFXsHandler.PlaySpawnAudio();
        }
    }
}