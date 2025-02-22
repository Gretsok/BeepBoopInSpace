using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using Game.Characters;
using UnityEngine;

namespace Game.MainMenu
{
    public class CharacterBankManager : AManager<CharacterBankManager>
    {
        [SerializeField]
        private List<CharacterData> m_charactersData = new List<CharacterData>();

        private Dictionary<CharacterWidget, CharacterData> m_associations = new();
        
        public CharacterData GetNextCharacterDataAfter(CharacterData characterData)
        {
            var index = m_charactersData.FindIndex(data => data == characterData);
            
            return m_charactersData[(index + 1) % m_charactersData.Count];
        }

        public void NotifyAssociation(CharacterWidget widget, CharacterData characterData)
        {
            if (m_associations.ContainsKey(widget))
                m_associations[widget] = characterData;
            else
                m_associations.Add(widget, characterData);
        }

        public bool IsCharacterAlreadyTakenByAnotherWidget(CharacterData characterDataToFind, CharacterWidget widgetOwner)
        {
            foreach (var (widget, characterData) in m_associations)
            {
                if (characterData == characterDataToFind && widget != widgetOwner)
                    return true;
            }
            
            return false;
        }
    }
}