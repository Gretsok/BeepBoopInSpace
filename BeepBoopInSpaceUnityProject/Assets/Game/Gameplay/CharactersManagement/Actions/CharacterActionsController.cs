using System.Collections.Generic;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement.Actions
{
    public class CharacterActionsController : MonoBehaviour
    {
        private CharacterReferencesHolder m_referencesHolder;
        public CharacterReferencesHolder ReferencesHolder => m_referencesHolder;

        public void InjectDependencies(CharacterReferencesHolder referencesHolder)
        {
            m_referencesHolder = referencesHolder;
        }
        
        public delegate void FActionKeyDelegate();

        public class ActionKeysConfiguration
        {
            public FActionKeyDelegate[] Action = new FActionKeyDelegate[8];
        }

        public ActionKeysConfiguration KeysConfiguration { get; private set; }
        public void SetConfiguration(List<int> config, ECharacterControlMode controlMode)
        {
            var actions = GetActions(controlMode);
            // The configuration shuffles every button: the mode's actions go on its buttons in their shuffled order.
            var buttons = config.FindAll(button => button < controlMode.ButtonsCount());

            var newConfig = new ActionKeysConfiguration();
            for (int i = 0; i < actions.Length; i++)
            {
                newConfig.Action[buttons[i]] = actions[i];
            }

            KeysConfiguration = newConfig;
        }

        private FActionKeyDelegate[] GetActions(ECharacterControlMode controlMode)
        {
            var movementController = ReferencesHolder.MovementController;
            switch (controlMode)
            {
                case ECharacterControlMode.Simplified:
                    return new FActionKeyDelegate[]
                    {
                        movementController.GoUp, movementController.GoLeft, movementController.GoRight, movementController.GoDown
                    };
                case ECharacterControlMode.SimplifiedWithSpecialAction:
                    return new FActionKeyDelegate[]
                    {
                        movementController.GoUp, movementController.GoLeft, movementController.GoRight, movementController.GoDown,
                        PlaySpecialAction
                    };
                case ECharacterControlMode.Advanced:
                default:
                    return new FActionKeyDelegate[]
                    {
                        movementController.WalkForward, movementController.WalkLeft, movementController.WalkRight,
                        movementController.TurnRight, movementController.TurnLeft, PlaySpecialAction
                    };
            }
        }

        public void TryToPerformAction(int index)
        {
            if (!ReferencesHolder.DeathController.IsAlive)
                return;
            KeysConfiguration.Action[index]?.Invoke();
        }
        
        

        private void PlaySpecialAction()
        {
            ReferencesHolder.SpecialAction.PerformAction();
        }
    }
}
