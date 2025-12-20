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
        public CharacterDataAsset CharacterDataAsset { get; private set; }
        
        [SerializeField] 
        private CanvasGroup m_container;

        [field: SerializeField]
        public TMP_Text NameText { get; private set; }

        [SerializeField]
        private CanvasGroup m_alreadySelectedBlock;
        
        [SerializeField]
        private AudioSource m_playerPopAudioSource;

        public bool CanPlay => !CharacterBankManager.Instance.IsCharacterAlreadyTakenByAnotherWidget(CharacterDataAsset, this);
        
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

            CharacterDataAsset = m_characterBankManager.GetFirstAvailableCharacterData();
            m_characterBankManager.NotifyAssociation(this, CharacterDataAsset);

            IsActivated = true;
        }
        

        public void UpdateModel()
        {
            for (int i = ModelContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(ModelContainer.GetChild(i).gameObject);
            }

            Instantiate(CharacterDataAsset.CharacterPrefab, ModelContainer);
            
            m_alreadySelectedBlock.gameObject.SetActive(m_characterBankManager.IsCharacterAlreadyTakenByAnotherWidget(CharacterDataAsset, this));
            NameText.text = CharacterDataAsset.Name;
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
            CharacterDataAsset = m_characterBankManager.GetNextCharacterDataAfter(CharacterDataAsset);
            m_characterBankManager.NotifyAssociation(this, CharacterDataAsset);
            m_onCharacterDataUpdated?.Invoke();
            OnCharacterDataUpdated?.Invoke();
        }
    }
}