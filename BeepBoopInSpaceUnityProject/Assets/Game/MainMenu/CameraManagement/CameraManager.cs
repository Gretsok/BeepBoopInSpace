using NaughtyAttributes;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.MainMenu.CameraManagement
{
    public class CameraManager : MonoBehaviour
    {
        [field: SerializeField]
        public CinemachineCamera EntranceCinemachineCamera { get; private set; }
        
        [field: SerializeField]
        public CinemachineCamera CharacterSelectionCinemachineCamera { get; private set; }
        
        [field: SerializeField]
        public CinemachineCamera HubCinemachineCamera { get; private set; }
        
        [field: SerializeField]
        public CinemachineCamera OptionsCinemachineCamera { get; private set; }

        private void Awake()
        {
            EntranceCinemachineCamera.gameObject.SetActive(false);
            CharacterSelectionCinemachineCamera.gameObject.SetActive(false);
            HubCinemachineCamera.gameObject.SetActive(false);
            OptionsCinemachineCamera.gameObject.SetActive(false);
        }

        [Button]
        public void SwitchToEntranceCamera()
        {
            EntranceCinemachineCamera.gameObject.SetActive(true);
            CharacterSelectionCinemachineCamera.gameObject.SetActive(false);
            HubCinemachineCamera.gameObject.SetActive(false);
            OptionsCinemachineCamera.gameObject.SetActive(false);
        }
        
        [Button]
        public void SwitchToCharacterSelectionCamera()
        {
            CharacterSelectionCinemachineCamera.gameObject.SetActive(true);
            HubCinemachineCamera.gameObject.SetActive(false);
            EntranceCinemachineCamera.gameObject.SetActive(false);
            OptionsCinemachineCamera.gameObject.SetActive(false);
        }
        
        [Button]
        public void SwitchToHubCamera()
        {
            HubCinemachineCamera.gameObject.SetActive(true);
            EntranceCinemachineCamera.gameObject.SetActive(false);
            CharacterSelectionCinemachineCamera.gameObject.SetActive(false);
            OptionsCinemachineCamera.gameObject.SetActive(false);
        }
        
        [Button]
        public void SwitchToOptionsCamera()
        {
            OptionsCinemachineCamera.gameObject.SetActive(true);
            EntranceCinemachineCamera.gameObject.SetActive(false);
            CharacterSelectionCinemachineCamera.gameObject.SetActive(false);
            HubCinemachineCamera.gameObject.SetActive(false);
        }
    }
}