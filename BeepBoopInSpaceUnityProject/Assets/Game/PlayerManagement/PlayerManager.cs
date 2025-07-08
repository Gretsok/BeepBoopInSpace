using System;
using System.Collections;
using System.Collections.Generic;
using Game.ArchitectureTools.Manager;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.PlayerManagement
{
    [RequireComponent(typeof(PlayerInputManager))]
    public class PlayerManager : AManager<PlayerManager>
    {
        public PlayerInputManager PlayerInputManager { get; private set; }

        private readonly List<AbstractPlayer> m_players = new();
        public IReadOnlyList<AbstractPlayer> Players => m_players;

        protected override IEnumerator Initialize()
        {
            PlayerInputManager = GetComponent<PlayerInputManager>();

            PlayerInputManager.onPlayerJoined += HandlePlayerJoined;
            PlayerInputManager.onPlayerLeft += HandlePlayerLeft;
            
            DontDestroyOnLoad(gameObject);
            yield return null;
        }
        
        public Action<PlayerManager, AbstractPlayer> OnPlayerJoined;
        private void HandlePlayerJoined(PlayerInput obj)
        {
            if (obj.TryGetComponent(out AbstractPlayer player)
                && !m_players.Contains(player))
            {
                m_players.Add(player);
                obj.transform.SetParent(transform);
                OnPlayerJoined?.Invoke(this, player);
            }
        }
        
        public Action<PlayerManager, AbstractPlayer> OnPlayerLeft;
        private void HandlePlayerLeft(PlayerInput obj)
        {
            if (obj.TryGetComponent(out AbstractPlayer player))
            {
                m_players.RemoveAll(p => p == player);
                OnPlayerLeft?.Invoke(this, player);
            }
        }

        public PlayerInput AddPlayerFromDevice(InputDevice device)
        {
            return PlayerInputManager.JoinPlayer(-1, -1, null, device);
        }

        public void ListenForNewPlayers()
        {
            PlayerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
        }

        public void StopListeningForNewPlayers()
        {
            PlayerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
        }

        public void RemoveAllPlayers()
        {
            for (int i = m_players.Count - 1; i >= 0; i--)
            {
                Destroy(m_players[i].gameObject);
            }
            m_players.Clear();
        }
    }
}