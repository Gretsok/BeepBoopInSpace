using System;
using System.Collections.Generic;
using Game.Gameplay.CharactersManagement.Death.Invincibility;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.Online.Gameplay;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Gameplay.CharactersManagement.Death
{
    public class DeathController : MonoBehaviour
    {
        public CharacterReferencesHolder CharacterReferencesHolder { get; private set; }
        
        public void InjectDependencies(CharacterReferencesHolder referencesHolder)
        {
            CharacterReferencesHolder = referencesHolder;
        }


        #region Invincibility Management
        public event Action<DeathController> OnInvincibilityActivated;
        public event Action<DeathController> OnInvincibilityDeactivated;
        private readonly HashSet<IInvincibilityGiver> m_invincibilityGivers = new();
        private bool m_isInvincibleTag = false; 
        public bool IsInvincible => m_isInvincibleTag;

        public void RegisterInvincibilityGiver(IInvincibilityGiver invincibilityGiver)
        {
            m_invincibilityGivers.Add(invincibilityGiver);
            UpdateInvincibilityStatus();
        }

        public void UnregisterInvincibilityGiver(IInvincibilityGiver invincibilityGiver)
        {
            m_invincibilityGivers.Remove(invincibilityGiver);
            UpdateInvincibilityStatus();
        }

        private void UpdateInvincibilityStatus()
        {
            var oldStatus = m_isInvincibleTag;
            m_isInvincibleTag = m_invincibilityGivers.Count > 0;
            if (oldStatus != m_isInvincibleTag)
            {
                if (m_isInvincibleTag)
                    OnInvincibilityActivated?.Invoke(this);
                else
                    OnInvincibilityDeactivated?.Invoke(this);
            }
        }
        #endregion

        [SerializeField]
        private UnityEvent m_onDeath;
        public event Action<DeathController> OnDeath;

        [SerializeField]
        private UnityEvent m_onResurrection;
        public event Action<DeathController> OnResurrection;

        [SerializeField]
        private float m_defaultWaitDurationToResurrect = 2f;
        public float DefaultWaitDurationToResurrect => m_defaultWaitDurationToResurrect;
        private float m_waitDurationToResurrect = 3f;
        public float WaitDurationToResurrect => m_waitDurationToResurrect;
        
        public bool IsAlive { get; private set; } = true;

        public bool CanResurrect { get; set; } = true;
        
        public void Kill(float a_waitDurationToResurrect = -1f, bool a_cannotCounter = false)
        {
            // Dying twice would count twice (Least Deaths) and schedule two resurrections.
            if (!IsAlive || (IsInvincible && !a_cannotCounter))
                return;

            if (a_waitDurationToResurrect < 0f)
                a_waitDurationToResurrect = m_defaultWaitDurationToResurrect;

            GameplaySync.NotifyKill(this, a_waitDurationToResurrect);
            Die(a_waitDurationToResurrect, true);
        }

        /// <summary>Dead for good, off the grid, even while waiting to come back: its player left the online room.</summary>
        public void KillForGood()
        {
            CanResurrect = false;
            CancelInvoke(nameof(Resurrect));
            if (IsAlive)
                Kill(a_cannotCounter: true);
        }

        /// <summary>Online guest: the host's robot died. The host resurrects it.</summary>
        public void ReplicateKill(float waitDurationToResurrect)
        {
            if (!IsAlive)
                return;
            Die(waitDurationToResurrect, false);
        }

        private void Die(float waitDurationToResurrect, bool schedulesResurrection)
        {
            IsAlive = false;
            m_waitDurationToResurrect = waitDurationToResurrect;

            if (CanResurrect)
            {
                if (schedulesResurrection)
                    Invoke(nameof(Resurrect), m_waitDurationToResurrect);
            }
            else
            {
                CharacterReferencesHolder.GridWalker.MoveToCell(null);
                
            }
            
            m_onDeath?.Invoke();
            OnDeath?.Invoke(this);
        }

        public void Resurrect()
        {
            if (IsAlive)
                return;
            GameplaySync.NotifyResurrect(this);
            IsAlive = true;
            
            m_onResurrection?.Invoke();
            OnResurrection?.Invoke(this);
        }
    }
}
