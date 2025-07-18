using Game.Gameplay.CharactersManagement.CollisionsHandling;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core;
using Game.Gameplay.ProjectileSystem;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement.SpecialActionsSystem.SniperRifle
{
    public class SniperBulletShooter : MonoBehaviour
    {
        [SerializeField]
        private float m_cooldown = 1f;
        [SerializeField]
        private Projectile m_bulletPrefab;

        [SerializeField]
        private float m_bulletHeight = 1f;

        [SerializeField]
        private float m_bulletStartingForwardPosition = 0.5f;

        private CharacterReferencesHolder m_referencesHolder;

        
        private float m_lastShotTime;
        
        private void Start()
        {
            GetComponent<SpecialAction>()
                .RegisterForDependencies(referencesHolder => m_referencesHolder = referencesHolder);
            m_lastShotTime = Time.time;
        }

        public void Shoot()
        {
            if (Time.time - m_lastShotTime < m_cooldown)
                return;
            
            var model = m_referencesHolder.ModelSource;
            var bullet = Instantiate(m_bulletPrefab,
                model.position + Vector3.up * m_bulletHeight + model.forward * m_bulletStartingForwardPosition,
                Quaternion.identity);

            bullet.SetUp(bullet.transform.position, m_referencesHolder.MovementController.GetWorldDirection(),
                HandleProjectileTriggerEnter);
            
            m_lastShotTime = Time.time;
        }

        private void HandleProjectileTriggerEnter(Collider obj, Projectile projectile)
        {
            if (!obj.TryGetComponent(out CharacterCollisionsHandler collisionsHandler))
            {
                Destroy(projectile.gameObject);
                return;
            }

            var otherDeathController = collisionsHandler.ReferencesHolder.DeathController;
            var myDeathController = m_referencesHolder.DeathController;

            if (otherDeathController == myDeathController)
                return;
            if (!otherDeathController.IsAlive)
                return;
            
            collisionsHandler.ReferencesHolder.DeathController.Kill();
            Destroy(projectile.gameObject);
        }
    }
}