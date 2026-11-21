using System;
using Game.Gameplay.CharactersManagement.CollisionsHandling;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core;
using Game.Gameplay.ProjectileSystem;
using Game.Online;
using Game.Online.Gameplay;
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

        public CharacterReferencesHolder ReferencesHolder { get; private set; }

        public event Action<SniperBulletShooter, Projectile> OnShot;
        public event Action<SniperBulletShooter> OnKilledOtherPlayer;
        
        private float m_lastShotTime;
        
        private void Start()
        {
            GetComponent<SpecialAction>()
                .RegisterForDependencies(referencesHolder => ReferencesHolder = referencesHolder);
            m_lastShotTime = Time.time;
        }

        public void Shoot()
        {
            if (Time.time - m_lastShotTime < m_cooldown)
                return;

            GameplaySync.NotifyShot(this);
            FireBullet();
        }

        /// <summary>Online guest: the host's robot shot. Its bullet only kills on the host.</summary>
        public void ReplicateShot()
        {
            FireBullet();
        }

        private void FireBullet()
        {
            var direction = ReferencesHolder.MovementController.GetWorldDirection();
            var bullet = Instantiate(m_bulletPrefab, GetMuzzlePosition(direction), Quaternion.identity);

            bullet.SetUp(bullet.transform.position, direction, HandleProjectileTriggerEnter);
            OnShot?.Invoke(this, bullet);

            m_lastShotTime = Time.time;
        }

        // On the robot's row of cells, wherever its model is in a jump or a turn: from the model, a shot right after a
        // step aside or a turn flew off the row, beside the robots in front of it. Level with the model along the row,
        // at the height of a robot on the ground (y = 0, where the moves land).
        private Vector3 GetMuzzlePosition(Vector3 direction)
        {
            var model = ReferencesHolder.ModelSource.position;
            var cell = ReferencesHolder.GridWalker.CurrentCell;
            if (!cell)
                return model + Vector3.up * m_bulletHeight + direction * m_bulletStartingForwardPosition;

            var row = cell.transform.position;
            row.y = 0f;
            var alongTheRow = Vector3.Dot(model - row, direction);
            return row + direction * (alongTheRow + m_bulletStartingForwardPosition) + Vector3.up * m_bulletHeight;
        }

        private void HandleProjectileTriggerEnter(Collider obj, Projectile projectile)
        {
            if (!obj.TryGetComponent(out CharacterCollisionsHandler collisionsHandler))
            {
                Destroy(projectile.gameObject);
                return;
            }

            var otherDeathController = collisionsHandler.ReferencesHolder.DeathController;
            var myDeathController = ReferencesHolder.DeathController;

            if (otherDeathController == myDeathController)
                return;
            if (!otherDeathController.IsAlive)
                return;
            
            if (Netplay.HasAuthority)
                otherDeathController.Kill();
            Destroy(projectile.gameObject);

            // An invincible player stops the bullet without dying: no kill to count.
            if (!otherDeathController.IsAlive)
                OnKilledOtherPlayer?.Invoke(this);
        }
    }
}