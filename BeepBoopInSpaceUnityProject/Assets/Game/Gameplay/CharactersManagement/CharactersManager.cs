using System;
using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core;
using Game.Gameplay.Levels._0_Core;
using Game.Global;
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
        private List<CharacterPlayerController> m_characterPlayerControllers = new List<CharacterPlayerController>();
        
        private Dictionary<CharacterPlayerController, CharacterPawn> m_characterPlayerControllersAssociation = new ();
        public IReadOnlyDictionary<CharacterPlayerController, CharacterPawn> CharacterPlayerControllersAssociation => m_characterPlayerControllersAssociation;

        public event Action<CharactersManager> OnCharactersCreated;

        public AbstractPlayer GetAbstractPlayerFromCharacter(CharacterPawn characterPawn)
        {
            var playerController = m_characterPlayerControllers.Find(characterPlayerController =>
                characterPlayerController.Pawn == characterPawn);
            return playerController.Player;
        }
        
        public void CreateCharactersAndPlayerControllers(SpecialAction specialActionPrefab)
        {
            var playerManager = GlobalContext.Instance.PlayerManager;

            for (int i = 0; i < playerManager.Players.Count; i++)
            {
                var player = playerManager.Players[i];
                var character = Instantiate(m_characterPawnPrefab);
                var playerController = Instantiate(m_characterPlayerControllerPrefab);
                
                playerController.SetPlayer(player);
                playerController.InjectDependencies(character);
                
                character.ReferencesHolder.SetSpecialAction(specialActionPrefab);
                character.ReferencesHolder.SetCharacterData(player.CharacterDataAsset);
                
                m_characterPawns.Add(character);
                m_characterPlayerControllers.Add(playerController);
                m_characterPlayerControllersAssociation.Add(playerController, character);
            }
            
            OnCharactersCreated?.Invoke(this);
        }
    }
}