using System;
using DG.Tweening;
using Game.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Game.MainMenu
{
    public class CharacterWidget : MonoBehaviour
    {
        [field: SerializeField]
        public Transform ModelContainer { get; private set; }
        [field: SerializeField]
        public CharacterData CharacterData { get; private set; }
        
        [SerializeField] 
        private CanvasGroup m_container;

        [field: SerializeField]
        public TMP_Text NameText { get; private set; }

        [SerializeField]
        private CanvasGroup m_alreadySelectedBlock;
        
        [SerializeField]
        private AudioSource m_playerPopAudioSource;

        public bool CanPlay => !CharacterBankManager.Instance.IsCharacterAlreadyTakenByAnotherWidget(CharacterData, this);
        
        private CharacterBankManager m_characterBankManager;
        
        private void Awake()
        {
            CharacterBankManager.RegisterPostInitializationCallback(manager =>
            {
                m_characterBankManager = manager;
            });
            Deactivate();
        }

        public bool IsActivated { get; private set; }
        
        public void Activate()
        {
            m_container.alpha = 1;

            m_characterBankManager.NotifyAssociation(this, CharacterData);

            IsActivated = true;
        }
        

        public void UpdateModel()
        {
            for (int i = ModelContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(ModelContainer.GetChild(i).gameObject);
            }

            Instantiate(CharacterData.CharacterPrefab, ModelContainer);
            
            m_alreadySelectedBlock.gameObject.SetActive(m_characterBankManager.IsCharacterAlreadyTakenByAnotherWidget(CharacterData, this));
            NameText.text = CharacterData.Name;
        }

        public void Deactivate()
        {
            m_container.alpha = 0;
            
            m_alreadySelectedBlock.gameObject.SetActive(false);
            
            for (int i = ModelContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(ModelContainer.GetChild(i).gameObject);
            }
            
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

        [SerializeField]
        private UnityEvent m_onCharacterDataUpdated;
        public Action OnCharacterDataUpdated;
        public void SwitchToNextCharacter()
        {
            CharacterData = m_characterBankManager.GetNextCharacterDataAfter(CharacterData);
            m_characterBankManager.NotifyAssociation(this, CharacterData);
            m_onCharacterDataUpdated?.Invoke();
            OnCharacterDataUpdated?.Invoke();
        }
    }
}