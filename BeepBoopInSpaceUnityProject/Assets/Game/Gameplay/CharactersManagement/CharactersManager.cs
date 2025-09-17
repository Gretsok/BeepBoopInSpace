using System;
using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core;
using Game.Gameplay.Levels._0_Core;
using Game.PlayerManagement;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement
{
    public class CharactersManager : AManager<CharactersManager>
    {
        [SerializeField]
        private CharacterPawn m_characterPawnPrefab;
        [SerializeField]
        private CharacterPlayerController m_characterPlayerControllerPrefab;

        private List<CharacterPawn> m_characterPawns = new List<CharacterPawn>();
        public IReadOnlyList<CharacterPawn> CharacterPawns => m_characterPawns;
        
        private Dictionary<CharacterPlayerController, CharacterPawn> m_characterPlayerControllers = new ();
        public IReadOnlyDictionary<CharacterPlayerController, CharacterPawn> CharacterPlayerControllers => m_characterPlayerControllers;

        public event Action<CharactersManager> OnCharactersCreated;
        
        public void CreateCharactersAndPlayerControllers(SpecialAction specialActionPrefab)
        {
            var playerManager = PlayerManager.Instance;

            for (int i = 0; i < playerManager.Players.Count; i++)
            {
                var player = playerManager.Players[i];
                var character = Instantiate(m_characterPawnPrefab);
                var playerController = Instantiate(m_characterPlayerControllerPrefab);
                
                playerController.SetPlayer(player);
                playerController.InjectDependencies(character.ReferencesHolder);
                
                character.ReferencesHolder.SetSpecialAction(specialActionPrefab);
                character.ReferencesHolder.SetCharacterData(player.CharacterDataAsset);
                
                m_characterPawns.Add(character);
                m_characterPlayerControllers.Add(playerController, character);
            }
            
            OnCharactersCreated?.Invoke(this);
        }
    }
}