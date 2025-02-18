using Game.PlayerManagement;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement
{
    public class CharacterPlayerController : MonoBehaviour
    {
        private CharacterPawn m_characterPawn;

        public void SetCharacterPawn(CharacterPawn characterPawn)
        {
            m_characterPawn = characterPawn;
        }
        
        private AbstractPlayer m_player;

        public void SetPlayer(AbstractPlayer player)
        {
            m_player = player;
        }
        

        public void Activate()
        {
            
        }

        public void Deactivate()
        {
            
        }
    }
}