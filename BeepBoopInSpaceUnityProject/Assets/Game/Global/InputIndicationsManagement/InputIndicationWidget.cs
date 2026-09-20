using Game.Global.PlayerManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Global.InputIndicationsManagement
{
    /// <summary>
    /// Updates associated image with the correct input sprite.
    /// </summary>
    public class InputIndicationWidget : MonoBehaviour
    {
        [SerializeField]
        private Image m_image;

        [SerializeField]
        private InputIndicationActionDataAsset m_actionDataAsset;

        private PlayerManager m_playerManager;
        private AbstractPlayer m_abstractPlayer;

        private void Awake()
        {
            GlobalContext.RegisterPostInitializationCallback(context =>
            {
                m_playerManager = context.PlayerManager;
                RefreshIndication();
            });
        }

        public void SetPlayer(AbstractPlayer player)
        {
            if (m_abstractPlayer)
                m_abstractPlayer.PlayerInput.onControlsChanged -= HandleControlsChanged;
            m_abstractPlayer = player;
            if (m_abstractPlayer)
                m_abstractPlayer.PlayerInput.onControlsChanged += HandleControlsChanged;
            RefreshIndication();
        }

        private void HandleControlsChanged(PlayerInput obj)
        {
            RefreshIndication();
        }

        public void ForceRefreshIndication()
        {
            RefreshIndication();
        }
        
        private void RefreshIndication()
        {
            if (!m_abstractPlayer && m_playerManager.Players.Count > 0)
            {
                SetPlayer(m_playerManager.Players[0]);
            }
            
            var abstractPlayer = m_abstractPlayer;
            if (!abstractPlayer)
            {
                Debug.LogError($"Could not find players. Aborting input indication refresh.");
                return;
            }

            if (!m_actionDataAsset)
            {
                Debug.LogError($"Missing action data asset on this InputIndicationWidget! Aborting input indication refresh.");
                return;
            }

            var controlScheme = abstractPlayer.PlayerInput.currentControlScheme;
            var sprite = m_actionDataAsset.GetSpriteFor(controlScheme);

            if (!sprite)
            {
                Debug.LogError($"Could not load input sprite. Aborting input indication refresh.");
                return;
            }
            
            m_image.sprite = sprite;
        }

        private void OnValidate()
        {
            if (!m_image)
                m_image = GetComponent<Image>();
        }
    }
}