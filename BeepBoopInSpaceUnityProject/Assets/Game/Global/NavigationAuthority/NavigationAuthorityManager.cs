using System.Linq;
using Game.Global.PlayerManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Game.Global.NavigationAuthority
{
    public class NavigationAuthorityManager : MonoBehaviour
    {
        private PlayerManagement.PlayerManager m_playerManager;
        private InputSystemUIInputModule m_inputModule;
        private EventSystem m_eventSystem;
        private int m_mainPlayerID = 0;
        public int MainPlayerID => m_mainPlayerID;
        private InputActionAsset m_actionAsset;

        public void SetDependencies(PlayerManagement.PlayerManager playerManager, EventSystem eventSystem)
        {
            m_playerManager = playerManager;
            m_eventSystem = eventSystem;
            m_inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            m_actionAsset = InputActionAsset.FromJson(m_inputModule.actionsAsset.ToJson());
            m_inputModule.actionsAsset = m_actionAsset;
            m_actionAsset.Enable();
        }

        private void Start()
        {
            m_playerManager.OnPlayersChanged += HandlePlayersChanged;
            UpdatePlayerDevicesOnEventSystem();
        }

        private void OnDestroy()
        {
            m_playerManager.OnPlayersChanged -= HandlePlayersChanged;
        }

        private void HandlePlayersChanged(PlayerManager obj)
        {
            UpdatePlayerDevicesOnEventSystem();
        }

        public void ChangeMainPlayer(int playerID)
        {
            m_mainPlayerID = playerID;
            UpdatePlayerDevicesOnEventSystem();
        }
        
        private void UpdatePlayerDevicesOnEventSystem()
        {
            var player = m_playerManager.Players.FirstOrDefault(player => player.PlayerInput.playerIndex == m_mainPlayerID);
            if (!player)
                return;
            m_actionAsset.devices = 
                player.PlayerInput.devices;
        }
        
    }
}
