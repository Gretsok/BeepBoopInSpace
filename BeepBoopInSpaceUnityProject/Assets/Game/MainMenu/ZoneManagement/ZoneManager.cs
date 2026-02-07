using NaughtyAttributes;
using UnityEngine;

namespace Game.MainMenu.ZoneManagement
{
    public class ZoneManager : MonoBehaviour
    {
        [field: SerializeField]
        public CameraZone EntranceZone { get; private set; }
        
        [field: SerializeField]
        public CameraZone CharacterSelectionZone { get; private set; }
        
        [field: SerializeField]
        public CameraZone HubZone { get; private set; }
        
        [field: SerializeField]
        public CameraZone OptionsZone { get; private set; }

        private void Awake()
        {
            EntranceZone.DeactivateZone();
            CharacterSelectionZone.DeactivateZone();
            HubZone.DeactivateZone();
            OptionsZone.ActivateZone();
        }

        [Button]
        public void SwitchToEntranceCamera()
        {
            EntranceZone.ActivateZone();
            CharacterSelectionZone.DeactivateZone();
            HubZone.DeactivateZone();
            OptionsZone.DeactivateZone();
        }
        
        [Button]
        public void SwitchToCharacterSelectionCamera()
        {
            CharacterSelectionZone.ActivateZone();
            HubZone.DeactivateZone();
            EntranceZone.DeactivateZone();
            OptionsZone.DeactivateZone();
        }
        
        [Button]
        public void SwitchToHubCamera()
        {
            HubZone.ActivateZone();
            EntranceZone.DeactivateZone();
            CharacterSelectionZone.DeactivateZone();
            OptionsZone.DeactivateZone();
        }
        
        [Button]
        public void SwitchToOptionsCamera()
        {
            OptionsZone.ActivateZone();
            EntranceZone.DeactivateZone();
            CharacterSelectionZone.DeactivateZone();
            HubZone.DeactivateZone();
        }
    }
}