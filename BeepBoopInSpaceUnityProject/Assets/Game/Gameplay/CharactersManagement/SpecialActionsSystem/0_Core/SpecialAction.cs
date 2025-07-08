using System;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core
{
    public class SpecialAction : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent m_onActionPressed;

        public event Action<CharacterReferencesHolder> OnDependenciesInjected;

        public CharacterReferencesHolder CharacterReferencesHolder { get; private set; }

        public void RegisterForDependencies(Action<CharacterReferencesHolder> onDependenciesReceived)
        {
            if (CharacterReferencesHolder)
            {
                onDependenciesReceived?.Invoke(CharacterReferencesHolder);
            }
            else
            {
                OnDependenciesInjected += onDependenciesReceived;
            }
        }
        
        public void InjectDependencies(CharacterReferencesHolder referencesHolder)
        {
            CharacterReferencesHolder = referencesHolder;
            OnDependenciesInjected?.Invoke(referencesHolder);
        }
        
        public void PerformAction()
        {
            m_onActionPressed?.Invoke();
        }
    }
}
