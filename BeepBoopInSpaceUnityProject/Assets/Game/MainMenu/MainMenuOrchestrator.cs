using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.LoadingScreen;
using Game.Global;
using Game.MainMenu.CharacterSelection;
using Game.MainMenu.Credits;
using Game.MainMenu.Home;
using Game.MainMenu.SettingsScreen;
using UnityEngine;

namespace Game.MainMenu
{
    public class MainMenuOrchestrator : MonoBehaviour
    {
        private FlowMachine m_flowMachine;
        
        [Header("States")]
        [SerializeField]
        private HomeState m_homeState;
        [SerializeField]
        private CharacterSelectionState m_characterSelectionState;
        [SerializeField]
        private CreditsState m_creditsState;
        [SerializeField]
        private MainMenuSettingsState m_settingsState;

        public void Initialize(GlobalContext globalContext, MainMenuContext mainMenuContext)
        {
            m_flowMachine = mainMenuContext.FlowMachine;
            m_homeState.Initialize(globalContext, mainMenuContext);
            m_characterSelectionState.Initialize(globalContext, mainMenuContext);
            m_creditsState.Initialize(globalContext, mainMenuContext);
            m_settingsState.Initialize(globalContext, mainMenuContext);

            LoadingScreenManager.Instance.HideLoadingScreen();
        }

        public void SwitchToHomeScreen()
        {
            m_flowMachine.RequestState(m_homeState);
        }

        public void SwitchToCharacterSelectionScreen()
        {
            m_flowMachine.RequestState(m_characterSelectionState);
        }

        public void SwitchToCreditsScreen()
        {
            m_flowMachine.RequestState(m_creditsState);
        }

        public void SwitchToSettingsScreen()
        {
            m_flowMachine.RequestState(m_settingsState);
        }
    }
}