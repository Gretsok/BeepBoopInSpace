using System;
using Game.Global.PlayerManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Flows.Tutorial
{
    public class TutorialPlayerWidget : MonoBehaviour
    {
        [SerializeField]
        private Image m_avatarImage;
        [SerializeField]
        private Button m_getReadyButton;
        [SerializeField]
        private Button m_unreadyButton;

        public AbstractPlayer Player { get; private set; }
        
        public event Action<TutorialPlayerWidget> OnReadyRequested;
        public event Action<TutorialPlayerWidget> OnUnreadyRequested;

        public void SetPlayer(AbstractPlayer player)
        {
            Player = player;
            m_avatarImage.sprite = player.CharacterDataAsset.AvatarSprite;
        }

        public void SetStatus(bool status)
        {
            m_getReadyButton.gameObject.SetActive(!status);
            m_unreadyButton.gameObject.SetActive(status);
        }

        private void Awake()
        {
            m_getReadyButton.onClick.AddListener(HandleGetReadyButtonClicked);
            m_unreadyButton.onClick.AddListener(HandleUnreadyButtonClicked);
        }

        private void OnDestroy()
        {
            m_getReadyButton.onClick.RemoveListener(HandleGetReadyButtonClicked);
            m_unreadyButton.onClick.RemoveListener(HandleUnreadyButtonClicked);
        }

        private void HandleGetReadyButtonClicked()
        {
            OnReadyRequested?.Invoke(this);
        }

        private void HandleUnreadyButtonClicked()
        {
            OnUnreadyRequested?.Invoke(this);
        }
    }
}
