using Game.Gameplay.Levels._0_Core;
using UnityEngine;

namespace Game.MainMenu.LevelSelection
{
    public class LevelSelectionState : AMainMenuFlowState
    {
        [SerializeField]
        private LevelSelectionScreen m_levelSelectionScreen;

        protected override void HandleInitialization()
        {
            base.HandleInitialization();
            m_levelSelectionScreen.Deactivate(true);
        }

        protected override void HandleEnter()
        {
            base.HandleEnter();
            m_levelSelectionScreen.OnLevelSelected += HandleLevelSelected;
            m_levelSelectionScreen.Activate();
        }

        private void HandleLevelSelected(LevelDataAsset obj)
        {
            MainMenuContext.GameModesLauncher.LaunchTraining(obj);
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            m_levelSelectionScreen.OnLevelSelected -= HandleLevelSelected;
            m_levelSelectionScreen.Deactivate();
        }
    }
}
