using System;
using DG.Tweening;
using Game.Characters;
using Game.Global.InputIndicationsManagement;
using Game.Global.PlayerManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Game.MainMenu.CharacterManagement
{
    /// <summary>
    /// A player's place in the character selection: the character model in the scene, and the slot element of the
    /// screen's UI (character name, "already taken" lock, input indication).
    /// </summary>
    public class CharacterWidget : MonoBehaviour
    {
        private const string k_ActiveSlotClass = "player-slot--active";

        [field: SerializeField]
        public Transform ModelContainer { get; private set; }
        public CharacterDataAsset CharacterDataAsset { get; private set; }

        [SerializeField]
        private AudioSource m_playerPopAudioSource;

        [SerializeField]
        private InputIndicationActionDataAsset m_popIndicationAction;

        private VisualElement m_view;
        private Label m_nameLabel;
        private VisualElement m_alreadySelectedBlock;
        private InputIndicationView m_popIndication;
        private Vector3 m_popScale = Vector3.one;

        public AbstractPlayer Player { get; private set; }

        public void SetPlayer(AbstractPlayer player)
        {
            Player = player;
            m_popIndication?.SetPlayer(Player);
        }

        /// <summary>Binds the widget to its slot element. The screen clones a new visual tree each time it opens.</summary>
        public void SetView(VisualElement view)
        {
            m_popIndication?.Dispose();

            m_view = view;
            m_nameLabel = view.Q<Label>("character-name");
            m_alreadySelectedBlock = view.Q("already-selected");
            m_popIndication = new InputIndicationView(view.Q("pop-indication"), m_popIndicationAction);
            m_popIndication.SetPlayer(Player);
            RefreshView();
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

        private void OnDestroy()
        {
            m_popIndication?.Dispose();
        }

        public bool IsActivated { get; private set; } = false;
        public event Action<CharacterWidget> OnActivated;
        public event Action<CharacterWidget> OnDeactivated;

        public void Activate()
        {
            if (IsActivated)
                return;

            CharacterDataAsset = m_characterBankManager.GetFirstAvailableCharacterData();
            m_characterBankManager.NotifyAssociation(this, CharacterDataAsset);

            IsActivated = true;
            RefreshView();
            OnActivated?.Invoke(this);
        }

        public void UpdateModel()
        {
            for (int i = ModelContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(ModelContainer.GetChild(i).gameObject);
            }

            Instantiate(CharacterDataAsset.CharacterPrefab, ModelContainer);

            RefreshView();
        }

        public void Deactivate(bool force = false)
        {
            if (!IsActivated && !force)
                return;

            for (int i = ModelContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(ModelContainer.GetChild(i).gameObject);
            }

            CharacterDataAsset = null;
            m_characterBankManager.UnregisterWidget(this);

            IsActivated = false;
            RefreshView();
            OnDeactivated?.Invoke(this);
        }

        private void RefreshView()
        {
            if (m_view == null)
                return;
            m_view.EnableInClassList(k_ActiveSlotClass, IsActivated);
            m_nameLabel.text = CharacterDataAsset ? CharacterDataAsset.Name : string.Empty;
            var isTaken = IsActivated && m_characterBankManager.IsCharacterAlreadyTakenByAnotherWidget(CharacterDataAsset, this);
            m_alreadySelectedBlock.style.display = isTaken ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Pop()
        {
            if (!IsActivated)
                return;
            DOTween.Kill(this, true);
            m_popScale = Vector3.one;
            DOTween.Punch(() => m_popScale, scale =>
            {
                m_popScale = scale;
                if (m_view != null)
                    m_view.style.scale = new Scale(scale);
            }, Vector3.one * 0.1f, 0.1f).SetTarget(this);
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
