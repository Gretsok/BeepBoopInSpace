using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay.Flows.End;
using Game.Gameplay.Flows.Finish;
using Game.Gameplay.Flows.Results;
using Game.Global;
using Game.Global.Localization;
using Game.Global.PlayerManagement;
using Game.Online;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.PauseMenu
{
    public class PauseMenuManager : MonoBehaviour,
        IFlowMachinePauser
    {
        private FlowMachine m_gameplayFlowMachine;
        public bool IsPaused { get; private set; }
        
        private PauseControls m_controls;
        public void Initialize(FlowMachine gameplayFlowMachine)
        {
            m_controls = new PauseControls();
            m_controls.Enable();
            
            m_controls.Pause.Toggle.started += HandlePauseToggleStarted;
            
            m_gameplayFlowMachine = gameplayFlowMachine;
            m_gameplayFlowMachine.OnStateEntering += HandleStateEntering;

            foreach (var player in GlobalContext.Instance.PlayerManager.Players)
            {
                var playerInput = player.PlayerInput;
                playerInput.onDeviceLost += HandleDeviceLost;
                playerInput.onDeviceRegained += HandleDeviceRegained;
                m_listenedPlayerInputs.Add(playerInput);
                // Lost while no game was listening, between two games of a tournament.
                if (playerInput.hasMissingRequiredDevices)
                    AddPlayerWithoutDevice(player);
            }
        }

        private readonly List<PlayerInput> m_listenedPlayerInputs = new();
        private readonly List<AbstractPlayer> m_playersWithoutDevice = new();
        public event Action<PauseMenuManager> OnPlayersWithoutDeviceChanged;
        private bool m_isPauseOwedToADevice;

        /// <summary>What to tell this machine's players about their disconnected pads, empty when nobody misses one.</summary>
        public string DeviceNotice
        {
            get
            {
                if (m_playersWithoutDevice.Count == 0)
                    return string.Empty;
                var names = string.Join(", ", m_playersWithoutDevice.Select(player => player.BadgeLabel));
                return Loc.T(m_playersWithoutDevice.Count > 1
                    ? $"Manettes débranchées : {names}. Rebranchez-les pour continuer."
                    : $"Manette débranchée : {names}. Rebranchez-la pour continuer.");
            }
        }

        private void HandleDeviceLost(PlayerInput playerInput)
        {
            var player = playerInput.GetComponent<AbstractPlayer>();
            Debug.Log($"[Pause] Device lost by player {player.Number}.");
            AddPlayerWithoutDevice(player);
        }

        // A disconnected pad holds the game until it comes back. Online, the game wouldn't wait: nothing to do but play on.
        private void AddPlayerWithoutDevice(AbstractPlayer player)
        {
            if (!m_playersWithoutDevice.Contains(player))
                m_playersWithoutDevice.Add(player);
            m_isPauseOwedToADevice = !Netplay.IsOnline;
            OnPlayersWithoutDeviceChanged?.Invoke(this);
        }

        private void HandleDeviceRegained(PlayerInput playerInput)
        {
            m_playersWithoutDevice.Remove(playerInput.GetComponent<AbstractPlayer>());
            if (m_playersWithoutDevice.Count == 0)
                m_isPauseOwedToADevice = false;
            OnPlayersWithoutDeviceChanged?.Invoke(this);
        }

        // The states of the tutorial and the introduction can't pause: it waits for the first one that can. Not while a state
        // enters, which can't be paused yet.
        private void Update()
        {
            if (!m_isPauseOwedToADevice)
                return;
            Pause();
            m_isPauseOwedToADevice = !IsPaused;
        }

        // Online, the game goes on behind the pause: once over, the pause gives way to its end and results (Menu principal
        // there would leave a game that already unloads, or a tournament between two games).
        private void HandleStateEntering(AFlowState state)
        {
            if (IsPaused && state is FinishState or ResultsState or EndState)
                Resume();
        }

        private void HandlePauseToggleStarted(InputAction.CallbackContext obj)
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }

        public Action<PauseMenuManager> OnPause;
        public void Pause()
        {
            if (IsPaused || m_isQuitting)
                return;
            var state = m_gameplayFlowMachine.CurrentState;
            if (!state || !state.GetComponent<CanBePausedTagStateComponent>())
                return;

            // Online, the game goes on for the other machines: the menu opens over it, and takes this machine's
            // players' buttons (CharacterPlayerController).
            m_isPausingTheGame = !Netplay.IsOnline;
            if (m_isPausingTheGame)
            {
                Time.timeScale = 0;
                DOTween.defaultTimeScaleIndependent = true;

                m_gameplayFlowMachine.Pause(this);
            }
            OnPause?.Invoke(this);
            
            IsPaused = true;
        }

        public Action<PauseMenuManager> OnResume;
        public void Resume()
        {
            if (!IsPaused)
                return;

            if (m_isPausingTheGame)
            {
                Time.timeScale = 1;
                DOTween.defaultTimeScaleIndependent = false;

                m_gameplayFlowMachine.Unpause(this);
            }
            OnResume?.Invoke(this);
            
            IsPaused = false;
        }

        private bool m_isPausingTheGame;
        private bool m_isQuitting;
        public bool IsQuitting => m_isQuitting;
        /// <summary>Leaves the game for the main menu through the end state, which unloads it. A tournament is abandoned.</summary>
        public void QuitToMainMenu()
        {
            if (m_isQuitting)
                return;
            m_isQuitting = true;

            Resume();
            // Online, leaving the game leaves the room: a host closes it, its guests leave their game too.
            if (Netplay.IsOnline)
                OnlineManager.Instance.LeaveRoom();
            // This machine decides now, even an online guest's.
            m_gameplayFlowMachine.IsRemotelyDriven = false;
            var endState = EndStateGrabber.Instance.EndState;
            endState.IsQuittingToMainMenu = true;
            m_gameplayFlowMachine.RequestState(endState);
        }

        private void OnDestroy()
        {
            if (m_gameplayFlowMachine)
                m_gameplayFlowMachine.OnStateEntering -= HandleStateEntering;
            m_controls.Pause.Toggle.started -= HandlePauseToggleStarted;

            foreach (var playerInput in m_listenedPlayerInputs)
            {
                if (!playerInput)
                    continue;
                playerInput.onDeviceLost -= HandleDeviceLost;
                playerInput.onDeviceRegained -= HandleDeviceRegained;
            }

            m_controls.Disable();
            m_controls.Dispose();
            m_controls = null;
        }
    }
}