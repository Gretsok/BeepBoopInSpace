using DG.Tweening;
using UnityEngine;

namespace Game.MainMenu
{
    public class CharacterWidget : MonoBehaviour
    {
        [field: SerializeField]
        public GameObject AssociatedCharacter { get; private set; }

        [SerializeField] 
        private CanvasGroup m_container;

        private void Awake()
        {
            Deactivate();
        }

        public void Activate()
        {
            m_container.alpha = 1;
            
            AssociatedCharacter.SetActive(true);
        }

        public void Deactivate()
        {
            m_container.alpha = 0;
            
            AssociatedCharacter.SetActive(false);
        }

        public void Pop()
        {
            transform.DOKill(true);
            transform.DOPunchScale(Vector3.one * 0.1f, 0.1f);
        }
    }
}