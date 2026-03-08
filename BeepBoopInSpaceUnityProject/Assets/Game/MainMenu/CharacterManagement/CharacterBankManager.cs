using System.Collections.Generic;
using System.Linq;
using Game.ArchitectureTools.Manager;
using Game.Characters;
using UnityEngine;

namespace Game.MainMenu
{
    public class CharacterBankManager : AManager<CharacterBankManager>
    {
        [SerializeField]
        private List<CharacterDataAsset> m_charactersData = new List<CharacterDataAsset>();

        private readonly Dictionary<CharacterWidget, CharacterDataAsset> m_associations = new();

        public CharacterDataAsset GetFirstAvailableCharacterData()
        {
            return m_charactersData.First(data => !m_associations.ContainsValue(data));
        }
        
        public CharacterDataAsset GetNextCharacterDataAfter(CharacterDataAsset characterDataAsset)
        {
            var index = m_charactersData.FindIndex(data => data == characterDataAsset);
            
            return m_charactersData[(index + 1) % m_charactersData.Count];
        }

        public void NotifyAssociation(CharacterWidget widget, CharacterDataAsset characterDataAsset)
        {
            if (m_associations.ContainsKey(widget))
                m_associations[widget] = characterDataAsset;
            else
                m_associations.Add(widget, characterDataAsset);
        }

        public bool IsCharacterAlreadyTakenByAnotherWidget(CharacterDataAsset characterDataAssetToFind, CharacterWidget widgetOwner)
        {
            foreach (var (widget, characterData) in m_associations)
            {
                if (characterData == characterDataAssetToFind && widget != widgetOwner)
                    return true;
            }
            
            return false;
        }
    }
}