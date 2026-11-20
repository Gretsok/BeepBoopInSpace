using System.Collections.Generic;
using System.Linq;
using Game.ArchitectureTools.Manager;
using Game.Characters;
using Game.Global.PlayerManagement;
using UnityEngine;

namespace Game.MainMenu.CharacterManagement
{
    /// <summary>The playable characters, and which one each player's widget points at in the character selection.</summary>
    public class CharacterBankManager : AManager<CharacterBankManager>
    {
        [SerializeField]
        private List<CharacterDataAsset> m_charactersData = new List<CharacterDataAsset>();

        private readonly Dictionary<CharacterWidget, CharacterDataAsset> m_associations = new();

        public IReadOnlyList<CharacterDataAsset> Characters => m_charactersData;

        /// <summary>A character nobody points at, else one nobody has chosen yet.</summary>
        public CharacterDataAsset GetFirstAvailableCharacterData()
        {
            return m_charactersData.FirstOrDefault(data => !m_associations.ContainsValue(data))
                ?? m_charactersData.FirstOrDefault(data => !IsCharacterAlreadyTakenByAnotherWidget(data, null))
                ?? m_charactersData[0];
        }

        public void NotifyAssociation(CharacterWidget widget, CharacterDataAsset characterDataAsset)
        {
            m_associations[widget] = characterDataAsset;
        }

        public void UnregisterWidget(CharacterWidget widget)
        {
            m_associations.Remove(widget);
        }

        /// <summary>
        /// The characters of the players back from a game become this bank's. The first menu of a session has the build's
        /// copies of the characters, the next ones those of their Addressables bundle: a copy kept from another menu is
        /// another object, which neither the grid, nor the robots' choice, nor the online room's ranks recognize.
        /// </summary>
        public void RemapCharactersOf(IEnumerable<AbstractPlayer> players)
        {
            foreach (var player in players)
            {
                var character = player.CharacterDataAsset;
                if (character && !m_charactersData.Contains(character))
                    player.SetCharacterData(m_charactersData.FirstOrDefault(data => data.name == character.name) ?? character);
            }
        }

        /// <summary>Whether a player other than <paramref name="widgetOwner"/>'s has chosen the character.</summary>
        public bool IsCharacterAlreadyTakenByAnotherWidget(CharacterDataAsset characterDataAssetToFind, CharacterWidget widgetOwner)
        {
            return m_associations.Any(association => association.Value == characterDataAssetToFind
                                                     && association.Key != widgetOwner && association.Key.IsReady);
        }

        /// <summary>The widgets pointing at the character, in player order.</summary>
        public IEnumerable<CharacterWidget> GetWidgetsOn(CharacterDataAsset characterDataAsset)
        {
            return m_associations.Where(association => association.Value == characterDataAsset)
                .Select(association => association.Key)
                .OrderBy(widget => widget.SlotIndex);
        }
    }
}
