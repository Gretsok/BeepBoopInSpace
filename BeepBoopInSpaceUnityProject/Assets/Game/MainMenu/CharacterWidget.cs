using DG.Tweening;
using Game.Characters;
using TMPro;
using UnityEngine;

namespace Game.MainMenu
{
    public class CharacterWidget : MonoBehaviour
    {
        [field: SerializeField]
        public GameObject AssociatedCharacter { get; private set; }
        [field: SerializeField]
        public CharacterData CharacterData { get; private set; }
        
        [SerializeField] 
        private CanvasGroup m_container;

        [field: SerializeField]
        public TMP_Text NameText { get; private set; }
        
        
        [SerializeField]
        private AudioSource m_playerPopAudioSource;
        
        private void Awake()
        {
            Deactivate();
        }

        public bool IsActivated { get; private set;}
        
        public void Activate()
        {
            m_container.alpha = 1;
            
            AssociatedCharacter.SetActive(true);
            
            NameText.text = CharacterData.Name;
            IsActivated = true;
        }

        public void Deactivate()
        {
            m_container.alpha = 0;
            
            AssociatedCharacter.SetActive(false);
            IsActivated = false;
        }

        public void Pop()
        {
            if (!IsActivated)
                return;
            transform.DOKill(true);
            transform.DOPunchScale(Vector3.one * 0.1f, 0.1f);
            m_playerPopAudioSource.Play();
        }
    }
}