using System;
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
        
        
        [SerializeField]
        private UnityEvent m_onDeath;
        public event Action<DeathController> OnDeath;

        [SerializeField]
        private UnityEvent m_onResurrection;
        public event Action<DeathController> OnResurrection;

        [SerializeField]
        private float m_defaultWaitDurationToResurrect = 2f;
        
        public bool IsAlive { get; private set; } = true;

        public bool CanResurrect { get; set; } = true;
        
        public void Kill(float a_waitDurationToResurrect = -1f)
        {
            IsAlive = false;
            
            if (a_waitDurationToResurrect < 0f)
                a_waitDurationToResurrect = m_defaultWaitDurationToResurrect;
            
            if (CanResurrect)
                Invoke(nameof(Resurrect), a_waitDurationToResurrect);
            else
            {
                CharacterReferencesHolder.GridWalker.MoveToCell(null);
                
            }
            
            m_onDeath?.Invoke();
            OnDeath?.Invoke(this);
        }

        public void Resurrect()
        {
            IsAlive = true;
            
            m_onResurrection?.Invoke();
            OnResurrection?.Invoke(this);
        }
    }
}
