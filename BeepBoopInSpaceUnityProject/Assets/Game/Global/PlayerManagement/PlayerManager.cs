using System;
using System.Collections.Generic;
using System.Linq;
using Game.Characters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Global.PlayerManagement
{
    [RequireComponent(typeof(PlayerInputManager))]
    public class PlayerManager : MonoBehaviour
    {
        public PlayerInputManager PlayerInputManager { get; private set; }

        private List<AbstractPlayer> m_players = new();
        /// <summary>The players of this machine, who joined in the character selection.</summary>
        public IReadOnlyList<AbstractPlayer> Players => m_players;

        private IReadOnlyList<AbstractPlayer> m_gamePlayers;
        /// <summary>The players of the games: the local ones and their bots, or the whole online room's (some of them remote) in its order.</summary>
        public IReadOnlyList<AbstractPlayer> GamePlayers => m_gamePlayers ?? (m_bots.Count > 0 ? m_playersAndBots : m_players);

        public const int k_MaxGamePlayers = 4;
        private readonly List<AbstractPlayer> m_bots = new();
        private readonly List<AbstractPlayer> m_playersAndBots = new();
        // What the players chose in the hub, none yet at first.
        private int m_chosenBotsCount = -1;

        /// <summary>
        /// What the players chose before the character selection: an online room (the character selection leads to the
        /// online screen) or the local games (to the hub). Kept until they go back to the home screen.
        /// </summary>
        public bool PlaysOnline { get; set; }

        public int MinBotsCount => m_players.Count >= 2 ? 0 : 1;
        public int MaxBotsCount => Mathf.Max(0, k_MaxGamePlayers - m_players.Count);

        /// <summary>How many robots the computer plays in the local games: a player alone gets a full game by default.</summary>
        public int BotsCount
        {
            get
            {
                var count = m_chosenBotsCount >= 0 ? m_chosenBotsCount : m_players.Count == 1 ? MaxBotsCount : 0;
                return Mathf.Clamp(count, MinBotsCount, MaxBotsCount);
            }
        }

        public void CycleBotsCount()
        {
            var next = BotsCount + 1;
            m_chosenBotsCount = next > MaxBotsCount ? MinBotsCount : next;
        }

        /// <summary>Before a local game: the bots take characters that the players don't play, in the players' wake.</summary>
        public void PrepareBots(IReadOnlyList<CharacterDataAsset> characters)
        {
            var count = BotsCount;
            while (m_bots.Count > count)
            {
                Destroy(m_bots[^1].gameObject);
                m_bots.RemoveAt(m_bots.Count - 1);
            }
            while (m_bots.Count < count)
            {
                var botObject = new GameObject($"Bot {m_bots.Count + 1}");
                botObject.transform.SetParent(transform);
                var bot = botObject.AddComponent<AbstractPlayer>();
                bot.IsBot = true;
                m_bots.Add(bot);
            }

            // A character without its selection art yet (Volto) would show a placeholder for a robot nobody chose.
            var freeCharacters = characters
                .Where(character => character.SelectionIcon && m_players.All(player => player.CharacterDataAsset != character))
                .OrderBy(_ => UnityEngine.Random.value).ToList();
            for (int i = 0; i < m_bots.Count; i++)
            {
                m_bots[i].SetCharacterData(freeCharacters[i % freeCharacters.Count]);
                m_bots[i].SetNumber(m_players.Count + i + 1);
            }

            m_playersAndBots.Clear();
            m_playersAndBots.AddRange(m_players);
            m_playersAndBots.AddRange(m_bots);
        }

        /// <summary>The online room's players for the next games, null to play with the local ones again.</summary>
        public void SetGamePlayers(IReadOnlyList<AbstractPlayer> players)
        {
            m_gamePlayers = players;
        }

        public void Initialize()
        {
            PlayerInputManager = GetComponent<PlayerInputManager>();

            PlayerInputManager.onPlayerJoined += HandlePlayerJoined;
            PlayerInputManager.onPlayerLeft += HandlePlayerLeft;
        }

        public event Action<PlayerManager> OnPlayersChanged;
        
        public event Action<PlayerManager, AbstractPlayer> OnPlayerJoined;
        private void HandlePlayerJoined(PlayerInput obj)
        {
            if (obj.TryGetComponent(out AbstractPlayer player)
                && !m_players.Contains(player))
            {
                m_players.Add(player);
                obj.transform.SetParent(transform);
                OnPlayerJoined?.Invoke(this, player);
                OnPlayersChanged?.Invoke(this);
            }
        }
        
        public event Action<PlayerManager, AbstractPlayer> OnPlayerLeft;
        private void HandlePlayerLeft(PlayerInput obj)
        {
            if (obj.TryGetComponent(out AbstractPlayer player))
            {
                m_players.RemoveAll(p => p == player);
                OnPlayerLeft?.Invoke(this, player);
                OnPlayersChanged?.Invoke(this);
            }
        }

        public AbstractPlayer GetPlayerByIndex(int playerIndex)
        {
            return m_players.Find(player => player.PlayerInput.playerIndex == playerIndex);
        }

        public PlayerInput AddPlayerFromDevice(InputDevice device)
        {
            return PlayerInputManager.JoinPlayer(-1, -1, null, device);
        }

        /// <summary>Lets new players join with <paramref name="joinAction"/>, or with any button when it is null.</summary>
        public void ListenForNewPlayers(InputAction joinAction = null)
        {
            if (joinAction == null)
            {
                PlayerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
                return;
            }
            StopListeningForNewPlayers();
            PlayerInputManager.joinAction = new InputActionProperty(joinAction);
            PlayerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered;
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

            foreach (var bot in m_bots)
                Destroy(bot.gameObject);
            m_bots.Clear();
            m_playersAndBots.Clear();
            m_chosenBotsCount = -1;
            PlaysOnline = false;
        }
    }
}