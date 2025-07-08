using Game.Characters;
using Game.Gameplay.CharactersManagement.Death;
using Game.Gameplay.CharactersManagement.Rumble;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem.Destruction;
using Game.Gameplay.GridSystem;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement.ReferencesHolding
{
    public class CharacterReferencesHolder : MonoBehaviour
    {

        [field: SerializeField]
        public Transform ModelSource { get; private set; }
        [field: SerializeField] 
        public GridWalker GridWalker { get; private set; }
        [field: SerializeField]
        public SpecialAction SpecialAction { get; private set; }
        [field: SerializeField]
        public DeathController DeathController { get; private set; }
        public CharacterDataAsset CharacterDataAsset { get; private set; }
        
        [field: Header("Feedback Handlers")]
        [field: SerializeField]
        public CharacterVFXsHandler VFXsHandler { get; private set; }
        [field: SerializeField]
        public CharacterAnimationsHandler AnimationsHandler { get; private set; }
        [field: SerializeField]
        public CharacterRumbleHandler RumbleHandler { get; private set; }
        [field: SerializeField]
        public CharacterSFXsHandler SFXsHandler { get; private set; }

        public void SetSpecialAction(SpecialAction specialActionPrefab)
        {
            SpecialAction = Instantiate(specialActionPrefab, transform);
        }
        
        public void SetCharacterData(CharacterDataAsset characterDataAsset)
        {
            CharacterDataAsset = characterDataAsset;
            SetModel(CharacterDataAsset.CharacterPrefab.transform);
        }
        
        public void SetModel(Transform modelPrefab)
        {
            while (ModelSource.childCount > 0)
            {
                var child = ModelSource.GetChild(0);
                Destroy(child.gameObject);
            }
            
            AnimationsHandler = Instantiate(modelPrefab, ModelSource).GetComponent<CharacterAnimationsHandler>();
        }
    }
}
