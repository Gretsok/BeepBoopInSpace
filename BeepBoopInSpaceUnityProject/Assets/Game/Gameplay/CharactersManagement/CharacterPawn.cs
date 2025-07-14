using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Characters;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.Gameplay.CharactersManagement.Rumble;
using Game.Gameplay.GridSystem;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement
{
    public class CharacterPawn : MonoBehaviour
    {
        [field: SerializeField]
        public CharacterReferencesHolder ReferencesHolder { get; private set; }
        
       
        private void Start()
        {
            ReferencesHolder.GridWalker.transform.SetParent(null);
            ReferencesHolder.SpecialAction.InjectDependencies(ReferencesHolder);
            ReferencesHolder.DeathController.InjectDependencies(ReferencesHolder);
            
            ReferencesHolder.ActionsController.InjectDependencies(ReferencesHolder);
            ReferencesHolder.MovementController.InjectDependencies(ReferencesHolder);
            
            ReferencesHolder.VFXsHandler.PlaySpawnEffect();
            
            ReferencesHolder.RumbleHandler.SetDependencies(this);
        }

        private void OnDestroy()
        {
            if (ReferencesHolder.GridWalker)
                Destroy(ReferencesHolder.GridWalker.gameObject);
        }
        


    }
}