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
        public void SetConfiguration(List<int> config)
        {
            var newConfig = new ActionKeysConfiguration();
            for (int i = 0; i < 8; i++)
            {
                if (i == config[0])
                {
                     newConfig.Action[i] = ReferencesHolder.MovementController.WalkForward;
                }
                else if (i == config[1])
                {
                    newConfig.Action[i] = ReferencesHolder.MovementController.WalkLeft;
                }
                else if (i == config[2])
                {
                    newConfig.Action[i] = ReferencesHolder.MovementController.WalkRight;
                }
                else if (i == config[3])
                {
                    newConfig.Action[i] = ReferencesHolder.MovementController.TurnRight;
                }
                else if (i == config[4])
                {
                    newConfig.Action[i] = ReferencesHolder.MovementController.TurnLeft;
                }
                else if (i == config[5])
                {
                    newConfig.Action[i] = PlaySpecialAction;
                }
                else
                {
                    newConfig.Action[i] = null;
                }
            }
            
            
            KeysConfiguration = newConfig;
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
