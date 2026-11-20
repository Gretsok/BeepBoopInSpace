using System;
using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.CharactersManagement.Bots;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core;
using Game.Global;
using Game.Global.PlayerManagement;
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

        /// <summary>The robot's rank in the game, the rank of its player in <see cref="PlayerManager.GamePlayers"/>; -1 if none.</summary>
        public int IndexOf(CharacterReferencesHolder referencesHolder)
        {
            return m_characterPawns.FindIndex(pawn => pawn.ReferencesHolder == referencesHolder);
        }

        public CharacterPlayerController ControllerOf(CharacterPawn characterPawn)
        {
            return m_characterPlayerControllers.Find(controller => controller.Pawn == characterPawn);
        }

        public AbstractPlayer GetAbstractPlayerFromCharacter(CharacterPawn characterPawn)
        {
            var playerController = m_characterPlayerControllers.Find(characterPlayerController =>
                characterPlayerController.Pawn == characterPawn);
            return playerController.Player;
        }
        
        public void CreateCharactersAndPlayerControllers(SpecialAction specialActionPrefab)
        {
            // Online, the whole room's players, some of them on other machines.
            var players = GlobalContext.Instance.PlayerManager.GamePlayers;

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var character = Instantiate(m_characterPawnPrefab);
                var playerController = Instantiate(m_characterPlayerControllerPrefab);
                
                playerController.SetPlayer(player);
                playerController.InjectDependencies(character);
                if (player.IsBot)
                    playerController.gameObject.AddComponent<BotController>().SetUp(playerController);

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