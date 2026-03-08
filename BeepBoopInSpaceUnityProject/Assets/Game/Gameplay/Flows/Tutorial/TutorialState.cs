using Game.ArchitectureTools.FlowMachine;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Flows.Tutorial
{
    public class TutorialState : AFlowState
    {
        [SerializeField]
        private Button m_nextButton;

        [SerializeField]
        private AFlowState m_nextState;

        protected override void HandleEnter()
        {
            base.HandleEnter();
            
            m_nextButton.onClick.AddListener(HandleNextButtonClicked);
        }

        protected override void HandleLeave()
        {
            base.HandleLeave();
            m_nextButton.onClick.RemoveListener(HandleNextButtonClicked);
        }

        private void HandleNextButtonClicked()
        {
            RequestState(m_nextState);
        }
    }
}