using System.Collections;
using DG.Tweening;
using Game.ArchitectureTools.Manager;
using TMPro;
using UnityEngine;

namespace Game.Gameplay.InteractionSystem
{
    public class InteractionPanel : AManager<InteractionPanel>
    {
        public InteractionPanelInteractableComponent CurrentlyRegisteredComponent { get; private set; }

        public void RegisterComponent(InteractionPanelInteractableComponent component)
        {
            CurrentlyRegisteredComponent = component;
            
            Show(CurrentlyRegisteredComponent.InteractionText);
        }

        public void UnregisterComponent(InteractionPanelInteractableComponent component)
        {
            if (CurrentlyRegisteredComponent != component)
                return;
            
            Hide();
            CurrentlyRegisteredComponent = null;
        }


        [SerializeField] 
        private CanvasGroup m_container;

        [SerializeField] 
        private TMP_Text m_interactionText;


        protected override IEnumerator Initialize()
        {
            m_interactionText.text = "";
            m_container.alpha = 0;
            yield return null;
        }

        public void Show(string text)
        {
            m_interactionText.text = text;
            
            m_container.DOKill();
            m_container.DOFade(1f, 0.1f);
        }

        public void Hide()
        {
            m_container.DOKill();
            m_container.DOFade(0f, 0.3f).onComplete += () =>
            {
                m_interactionText.text = "";

            };
        }
    }
}