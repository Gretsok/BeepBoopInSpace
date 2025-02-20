using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
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
        
        public void CreateCharactersAndPlayerControllers()
        {
            var playerManager = PlayerManager.Instance;

            for (int i = 0; i < playerManager.Players.Count; i++)
            {
                var player = playerManager.Players[i];
                var character = Instantiate(m_characterPawnPrefab);
                var playerController = Instantiate(m_characterPlayerControllerPrefab);
                
                playerController.SetPlayer(player);
                playerController.SetCharacterPawn(character);
                
                character.SetCharacterData(player.CharacterData);
                
                m_characterPawns.Add(character);
                m_characterPlayerControllers.Add(playerController, character);
            }
        }
    }
}