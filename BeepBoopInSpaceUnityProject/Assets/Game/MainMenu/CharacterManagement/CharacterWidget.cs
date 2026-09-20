using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Characters;
using Game.Global.InputIndicationsManagement;
using Game.Global.PlayerManagement;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Game.MainMenu.CharacterManagement
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

        [SerializeField]
        private List<InputIndicationWidget> m_inputIndicationWidgets = new();
        
        public AbstractPlayer Player { get; private set; }

        public void SetPlayer(AbstractPlayer player)
        {
            Player = player;
            m_inputIndicationWidgets?.ForEach(widget => widget.SetPlayer(Player));
        }

        public bool CanPlay => !CharacterBankManager.Instance.IsCharacterAlreadyTakenByAnotherWidget(CharacterDataAsset, this);
        
        private CharacterBankManager m_characterBankManager;
        
        private void Awake()
        {
            CharacterBankManager.RegisterPostInitializationCallback(manager =>
            {
                m_characterBankManager = manager;
            });
            Deactivate(true);
        }

        public bool IsActivated { get; private set; } = false;
        public event Action<CharacterWidget> OnActivated;
        public event Action<CharacterWidget> OnDeactivated;
        
        public void Activate()
        {
            if (IsActivated)
                return;
            m_container.alpha = 1;

            CharacterDataAsset = m_characterBankManager.GetFirstAvailableCharacterData();
            m_characterBankManager.NotifyAssociation(this, CharacterDataAsset);

            IsActivated = true;
            OnActivated?.Invoke(this);
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

        public void Deactivate(bool force = false)
        {
            if (!IsActivated && !force)
                return;
            m_container.alpha = 0;
            
            m_alreadySelectedBlock.gameObject.SetActive(false);
            
            for (int i = ModelContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(ModelContainer.GetChild(i).gameObject);
            }
            
            CharacterDataAsset = null;
            m_characterBankManager.UnregisterWidget(this);
            
            IsActivated = false;
            OnDeactivated?.Invoke(this);
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