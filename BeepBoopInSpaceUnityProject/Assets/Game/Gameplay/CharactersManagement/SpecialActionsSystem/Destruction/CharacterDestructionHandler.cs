using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.Gameplay.CharactersManagement.Rumble;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement.SpecialActionsSystem.Destruction
{
    public class CharacterDestructionHandler : MonoBehaviour
    {
        [SerializeField]
        private RumbleData m_explosionRumbleData;
        
        private CharacterReferencesHolder m_referencesHolder;
        
        private void Start()
        {
            GetComponent<SpecialAction>().RegisterForDependencies(referencesHolder => m_referencesHolder = referencesHolder);
        }
        
        public void Destroy()
        {
            m_referencesHolder.DeathController.Kill();
            
            m_referencesHolder.RumbleHandler.PlayRumbleData(m_explosionRumbleData);
        }
    }
}