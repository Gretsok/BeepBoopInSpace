using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.CharactersManagement.Rumble
{
    public class CharacterRumbleHandler : MonoBehaviour
    {
        private CharacterPawn m_characterPawn;

        public void SetDependencies(CharacterPawn characterPawn)
        {
            m_characterPawn = characterPawn;
        }

        [SerializeField]
        private RumbleData m_itsMeRumbleData;

        [SerializeField]
        private RumbleData m_winRumbleData;
        
        [SerializeField]
        private RumbleData m_loseRumbleData;
        
        [SerializeField]
        private RumbleData m_moveRumbleData;
        
        [SerializeField]
        private RumbleData m_turnRumbleData;
        

        public void PlayItsMeRumble(Action onComplete = null)
        {
            PlayRumbleData(m_itsMeRumbleData, onComplete);
        }

        public void PlayWinRumble(Action onComplete = null)
        {
            PlayRumbleData(m_winRumbleData, onComplete);
        }

        public void PlayLoseRumble(Action onComplete = null)
        {
            PlayRumbleData(m_loseRumbleData, onComplete);
        }

        public void PlayMoveRumble(Action onComplete = null)
        {
            PlayRumbleData(m_moveRumbleData, onComplete);
        }

        public void PlayTurnRumble(Action onComplete = null)
        {
            PlayRumbleData(m_turnRumbleData, onComplete);
        }
        
        public void PlayRumbleData(RumbleData rumbleData, Action onComplete = null)
        {
            if (!rumbleData)
                return;
            var charactersManager = CharactersManager.Instance;
            var controller = charactersManager.CharacterPlayerControllersAssociation.FirstOrDefault(pair => pair.Value == m_characterPawn)
                .Key;
            if (!controller)
                return;
            var playerdevices = controller.Player.PlayerInput.devices;

            for (int i = 0; i < playerdevices.Count; i++)
            {
                var device = playerdevices[i] as Gamepad;
                
                if (device == null)
                    continue;
                
                charactersManager.StartCoroutine(RumbleReaderUtils.RumbleRoutine(device, rumbleData, onComplete));
            }
        }

        private void OnDisable()
        {
            var charactersManager = CharactersManager.Instance;
            var controller = charactersManager.CharacterPlayerControllersAssociation.FirstOrDefault(pair => pair.Value == m_characterPawn)
                .Key;
            if (!controller)
                return;
            var playerdevices = controller.Player.PlayerInput.devices;

            for (int i = 0; i < playerdevices.Count; i++)
            {
                var device = playerdevices[i] as Gamepad;
                
                if (device == null)
                    continue;
                
                device.SetMotorSpeeds(0f, 0f);
            }
        }
    }
}