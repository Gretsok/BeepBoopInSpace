using System;
using System.Collections.Generic;
using Game.Gameplay.CharactersManagement.Death.Invincibility;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
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
            if (IsInvincible && !a_cannotCounter)
                return;

            IsAlive = false;
            
            if (a_waitDurationToResurrect < 0f)
                a_waitDurationToResurrect = m_defaultWaitDurationToResurrect;
            
            m_waitDurationToResurrect = a_waitDurationToResurrect;
            
            if (CanResurrect)
                Invoke(nameof(Resurrect), m_waitDurationToResurrect);
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
            IsAlive = true;
            
            m_onResurrection?.Invoke();
            OnResurrection?.Invoke(this);
        }
    }
}
