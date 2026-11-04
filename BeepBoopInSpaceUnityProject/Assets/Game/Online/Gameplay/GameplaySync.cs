using System;
using System.Collections.Generic;
using System.IO;

using Game.ArchitectureTools.FlowMachine;
using Game.Gameplay;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.Death;
using Game.Gameplay.CharactersManagement.Movement;
using Game.Gameplay.CharactersManagement.ReferencesHolding;
using Game.Gameplay.CharactersManagement.Scoring;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem.SniperRifle;
using Game.Gameplay.Flows._0_Load;
using Game.Gameplay.Flows.End;
using Game.Gameplay.Flows.Tutorial;
using Game.Gameplay.GameModes.GameplayModifiers.Meteorites;
using Game.Gameplay.GameModes.ObjectiveManagement.PointsRushObjectiveManagement;
using Game.Gameplay.GridSystem;
using Game.Gameplay.Timer;
using Game.Global;
using UnityEngine;

namespace Game.Online.Gameplay
{
    /// <summary>
    /// A game of an online room. The host plays it as offline, and tells the guests each thing that happens (state of
    /// the flow, moves, deaths, shots, meteorites, scores...) through the Notify methods the gameplay code calls, in
    /// the order it happens. A guest's game decides nothing (<see cref="Netplay.HasAuthority"/>): it replays these
    /// messages, and sends the host its players' buttons. Robots are named by their rank in the game, cells by their
    /// coordinates in the grid, states by their path under the flow machine: every machine loads the same scenes.
    /// </summary>
    public class GameplaySync : MonoBehaviour
    {
        private const float k_TimerSyncPeriod = 1f;

        private static GameplaySync s_instance;

        private OnlineManager m_online;
        private GameplayContext m_gameplayContext;
        private FlowMachine m_flowMachine;
        private bool m_isHost;
        private bool m_isLoaded;
        private bool m_isLeaving;
        private bool m_hasRemovedLeftPlayers;
        private float m_nextTimerSync;
        private float m_guestsLoadingDeadline;
        private TimerManager m_timer;

        private static bool IsHosting => s_instance && s_instance.m_isHost && s_instance.m_online.IsInRoom;

        /// <summary>Plays the game of the gameplay context online, if this machine is in a room.</summary>
        public static void AttachTo(GameplayContext gameplayContext)
        {
            var online = OnlineManager.Instance;
            if (!online || !online.IsInGame || (!online.IsInRoom && !online.IsGuest))
                return;
            var sync = gameplayContext.gameObject.AddComponent<GameplaySync>();
            sync.Initialize(online, gameplayContext);
        }

        private void Initialize(OnlineManager online, GameplayContext gameplayContext)
        {
            s_instance = this;
            m_online = online;
            m_gameplayContext = gameplayContext;
            m_flowMachine = gameplayContext.FlowMachine;
            m_isHost = online.IsHost;
            m_online.OnRoomLost += HandleRoomLost;
            if (m_isHost)
            {
                m_flowMachine.OnStateEntering += HandleStateEntering;
                m_online.OnGuestGameMessage += HandleGuestMessage;
                m_online.OnGuestLeftGame += HandleGuestLeftGame;
                // The game starts once every guest has loaded it too, or has been sent away for taking too long.
                LoadEventsHooker.RegisterPostInitializationCallback(hooker => hooker.AddLoadingRequirement(() => m_online.AreGuestsLoaded));
                m_guestsLoadingDeadline = Time.unscaledTime + OnlineManager.GuestLoadingTimeout;
            }
            else
            {
                // The host says which state comes next.
                m_flowMachine.IsRemotelyDriven = true;
                // Lost before the game even started.
                if (!m_online.IsInRoom)
                    HandleRoomLost(null);
            }
            Debug.Log($"[Online] Game {m_online.GameNumber} played as {(m_isHost ? "host" : "guest")}.");
        }

        private void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
            if (!m_online)
                return;
            m_online.OnRoomLost -= HandleRoomLost;
            m_online.OnGuestGameMessage -= HandleGuestMessage;
            m_online.OnGuestLeftGame -= HandleGuestLeftGame;
            if (m_flowMachine)
                m_flowMachine.OnStateEntering -= HandleStateEntering;
        }

        private void Update()
        {
            if (m_isHost)
            {
                SendAwaySlowGuests();
                RemoveLeftPlayers();
                SyncTimer();
                return;
            }
            if (m_isLeaving)
                return;
            if (!m_isLoaded)
            {
                if (!m_gameplayContext.LoadingManager.HasLoadedLevel)
                    return;
                m_isLoaded = true;
                m_online.SendGameLoaded();
            }
            while (!m_isLeaving && m_online.TryTakeGameMessage(out var message))
            {
                try
                {
                    Apply(message);
                }
                catch (Exception e) when (e is IOException or ArgumentException or IndexOutOfRangeException)
                {
                    Debug.LogWarning($"[Online] Unreadable message {OnlineMessage.TypeOf(message)}: {e.Message}");
                }
            }
        }

        // The room is over during the game: back to the main menu, which says why.
        private void HandleRoomLost(string reason)
        {
            if (m_isLeaving)
                return;
            m_isLeaving = true;
            m_flowMachine.IsRemotelyDriven = false;
            // Already on its way out (the host ended the game): leaving again would unload it twice. A tournament's
            // results see the room is lost.
            if (m_flowMachine.CurrentState is EndState)
                return;
            m_gameplayContext.PauseMenuManager.QuitToMainMenu();
        }

        #region Host: what happened

        private void HandleStateEntering(AFlowState state)
        {
            // Every machine starts in the load state, and loads on its own. The host quitting the game from the pause
            // leaves the room: the guests hear it, and quit on their side.
            if (state is LoadState || m_gameplayContext.PauseMenuManager.IsQuitting)
                return;
            var path = PathOf(state);
            if (path == null)
            {
                Debug.LogWarning($"[Online] The state {state.name} isn't under the flow machine: the guests can't follow it.");
                return;
            }
            m_online.SendGameMessage(OnlineMessage.Write(EOnlineMessage.FlowState, writer => writer.Write(path)));
        }

        // A guest that never loads (stuck, or frozen without losing its connection) mustn't hold the game up forever.
        private void SendAwaySlowGuests()
        {
            if (m_gameplayContext.LoadingManager.HasLoadedLevel || Time.unscaledTime < m_guestsLoadingDeadline || m_online.AreGuestsLoaded)
                return;
            m_guestsLoadingDeadline = float.PositiveInfinity;
            m_online.SendAwayGuestsStillLoading();
        }

        // A tournament's next games keep the players who left the room: their robots are out from the start.
        private void RemoveLeftPlayers()
        {
            if (m_hasRemovedLeftPlayers || !m_gameplayContext.LoadingManager.HasLoadedLevel)
                return;
            m_hasRemovedLeftPlayers = true;
            var players = GlobalContext.Instance.PlayerManager.GamePlayers;
            var pawns = CharactersManager.Instance.CharacterPawns;
            for (int i = 0; i < players.Count && i < pawns.Count; i++)
            {
                if (m_online.HasLeftRoom(players[i]) && !pawns[i].HasLeft)
                    pawns[i].Leave();
            }
        }

        private void SyncTimer()
        {
            if (!m_timer)
            {
                if (!m_gameplayContext.LoadingManager.HasLoadedLevel)
                    return;
                m_timer = FindAnyObjectByType<TimerManager>();
                if (!m_timer)
                    return;
            }
            if (!m_timer.IsTicking || Time.unscaledTime < m_nextTimerSync)
                return;
            m_nextTimerSync = Time.unscaledTime + k_TimerSyncPeriod;
            m_online.SendGameMessage(OnlineMessage.Write(EOnlineMessage.Timer, writer => writer.Write(m_timer.TimeLeft)));
        }

        public static void NotifyMove(CharacterMovementController controller, Cell cell)
        {
            Send(EOnlineMessage.Move, controller.ReferencesHolder, writer => WriteCell(writer, cell));
        }

        public static void NotifyBump(CharacterMovementController controller)
        {
            Send(EOnlineMessage.Bump, controller.ReferencesHolder);
        }

        /// <param name="turn">A turn of the advanced controls (jump, sound), else the simplified controls' facing.</param>
        public static void NotifyDirection(CharacterMovementController controller, CharacterMovementController.EDirection direction, bool turn)
        {
            Send(turn ? EOnlineMessage.Turn : EOnlineMessage.Face, controller.ReferencesHolder, writer => writer.Write((byte)direction));
        }

        public static void NotifyTeleport(CharacterMovementController controller, Cell cell)
        {
            Send(EOnlineMessage.Teleport, controller.ReferencesHolder, writer => WriteCell(writer, cell));
        }

        public static void NotifyDetach(CharacterMovementController controller)
        {
            Send(EOnlineMessage.Detach, controller.ReferencesHolder);
        }

        /// <summary>A robot slides on ice towards a cell, or back onto the ice.</summary>
        public static void NotifySlide(CharacterMovementController controller, Cell cell)
        {
            Send(EOnlineMessage.Slide, controller.ReferencesHolder, writer => WriteCell(writer, cell));
        }

        public static void NotifyKill(DeathController deathController, float waitDurationToResurrect)
        {
            Send(EOnlineMessage.Kill, deathController.CharacterReferencesHolder, writer =>
            {
                writer.Write(waitDurationToResurrect);
                writer.Write(deathController.CanResurrect);
            });
        }

        public static void NotifyResurrect(DeathController deathController)
        {
            Send(EOnlineMessage.Resurrect, deathController.CharacterReferencesHolder);
        }

        public static void NotifyRespawnTarget(DeathController deathController, Cell cell, float travelDuration)
        {
            Send(EOnlineMessage.RespawnTarget, deathController.CharacterReferencesHolder, writer =>
            {
                WriteCell(writer, cell);
                writer.Write(travelDuration);
            });
        }

        public static void NotifyShot(SniperBulletShooter shooter)
        {
            Send(EOnlineMessage.Shot, shooter.ReferencesHolder);
        }

        public static void NotifyScore(CharacterScoringController scoringController)
        {
            if (!IsHosting)
                return;
            var pawns = CharactersManager.Instance.CharacterPawns;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].ReferencesHolder.ScoringController == scoringController)
                    Send(EOnlineMessage.Score, pawns[i].ReferencesHolder, writer => writer.Write(scoringController.Score));
            }
        }

        public static void NotifyMeteorite(Cell target, float dropTime)
        {
            SendWithoutRobot(EOnlineMessage.Meteorite, writer =>
            {
                WriteCell(writer, target);
                writer.Write(dropTime);
            });
        }

        /// <summary>Points Rush: the cell that gives the next point, null for none.</summary>
        public static void NotifyObjective(Cell cell)
        {
            SendWithoutRobot(EOnlineMessage.Objective, writer => WriteCell(writer, cell));
        }

        public static void NotifyPointCollected(CharacterMovementController controller, Cell cell)
        {
            Send(EOnlineMessage.PointCollected, controller.ReferencesHolder, writer => WriteCell(writer, cell));
        }

        public static void NotifyRound(int roundIndex)
        {
            SendWithoutRobot(EOnlineMessage.Round, writer => writer.Write(roundIndex));
        }

        /// <param name="slot">The player's rank in the game.</param>
        public static void NotifyTutorialStatus(int slot, bool isReady)
        {
            SendWithoutRobot(EOnlineMessage.TutorialStatus, writer =>
            {
                writer.Write((byte)slot);
                writer.Write(isReady);
            });
        }

        private static void Send(EOnlineMessage type, CharacterReferencesHolder robot, Action<BinaryWriter> writeContent = null)
        {
            if (!IsHosting)
                return;
            var robotIndex = CharactersManager.Instance ? CharactersManager.Instance.IndexOf(robot) : -1;
            if (robotIndex < 0)
                return;
            s_instance.m_online.SendGameMessage(OnlineMessage.Write(type, writer =>
            {
                writer.Write((byte)robotIndex);
                writeContent?.Invoke(writer);
            }));
        }

        private static void SendWithoutRobot(EOnlineMessage type, Action<BinaryWriter> writeContent)
        {
            if (IsHosting)
                s_instance.m_online.SendGameMessage(OnlineMessage.Write(type, writeContent));
        }

        #endregion

        #region Host: the guests' players

        private void HandleGuestMessage(ulong machineId, byte[] message)
        {
            using var reader = OnlineMessage.Read(message);
            var slot = reader.ReadByte();
            if (!m_online.IsPlayerOf(machineId, slot))
                return;
            switch (OnlineMessage.TypeOf(message))
            {
                case EOnlineMessage.PlayerAction:
                {
                    var actionIndex = reader.ReadByte();
                    var charactersManager = CharactersManager.Instance;
                    if (!charactersManager || slot >= charactersManager.CharacterPawns.Count)
                        return;
                    var controller = charactersManager.ControllerOf(charactersManager.CharacterPawns[slot]);
                    if (controller)
                        controller.PerformRemoteAction(actionIndex);
                    break;
                }
                case EOnlineMessage.TutorialStatusRequest:
                {
                    var isReady = reader.ReadBoolean();
                    var players = GlobalContext.Instance.PlayerManager.GamePlayers;
                    if (slot < players.Count)
                        m_gameplayContext.UIManager.GetPanel<TutorialPanel>().SetPlayerStatus(players[slot], isReady);
                    break;
                }
            }
        }

        // Their robots die for good, and are ready in the tutorial: the others play on.
        private void HandleGuestLeftGame()
        {
            var charactersManager = CharactersManager.Instance;
            // Still loading: their robots are taken out once loaded (RemoveLeftPlayers).
            if (!charactersManager || !m_gameplayContext.LoadingManager.HasLoadedLevel)
                return;
            var players = GlobalContext.Instance.PlayerManager.GamePlayers;
            var pawns = charactersManager.CharacterPawns;
            var tutorialPanel = m_gameplayContext.UIManager.GetPanel<TutorialPanel>();
            for (int i = 0; i < players.Count && i < pawns.Count; i++)
            {
                if (!m_online.HasLeftRoom(players[i]) || pawns[i].HasLeft)
                    continue;
                pawns[i].Leave();
                if (tutorialPanel && tutorialPanel.PlayerStatus.ContainsKey(players[i]))
                    tutorialPanel.SetPlayerStatus(players[i], true);
            }
        }

        #endregion

        #region Guest

        /// <summary>Guest: a button of a player of this machine, for the host to play.</summary>
        public static void SendPlayerAction(CharacterPawn pawn, int actionIndex)
        {
            if (!s_instance || s_instance.m_isHost || s_instance.m_isLeaving)
                return;
            var slot = CharactersManager.Instance.IndexOf(pawn.ReferencesHolder);
            if (slot >= 0)
                s_instance.m_online.SendToHost(OnlineMessage.Write(EOnlineMessage.PlayerAction, writer =>
                {
                    writer.Write((byte)slot);
                    writer.Write((byte)actionIndex);
                }));
        }

        /// <summary>Guest: a player of this machine is ready in the tutorial, or not anymore.</summary>
        public static void SendTutorialStatusRequest(int slot, bool isReady)
        {
            if (!s_instance || s_instance.m_isHost || s_instance.m_isLeaving)
                return;
            s_instance.m_online.SendToHost(OnlineMessage.Write(EOnlineMessage.TutorialStatusRequest, writer =>
            {
                writer.Write((byte)slot);
                writer.Write(isReady);
            }));
        }

        private void Apply(byte[] message)
        {
            var type = OnlineMessage.TypeOf(message);
            using var reader = OnlineMessage.Read(message);
            switch (type)
            {
                case EOnlineMessage.FlowState:
                {
                    var path = reader.ReadString();
                    var state = StateAt(path);
                    if (state)
                        m_flowMachine.SwitchToStateNow(state);
                    else
                        Debug.LogWarning($"[Online] No state at {path}.");
                    return;
                }
                case EOnlineMessage.Meteorite:
                {
                    var cell = ReadCell(reader);
                    var dropTime = reader.ReadSingle();
                    var meteorites = m_gameplayContext.GetGameplayModifier<MeteoritesModifier>();
                    if (meteorites && cell)
                        meteorites.ReplicateMeteorite(cell, dropTime);
                    return;
                }
                case EOnlineMessage.Objective:
                {
                    var cell = ReadCell(reader);
                    if (m_gameplayContext.ObjectiveManager is PointsRushObjectiveManager pointsRush)
                        pointsRush.ReplicateObjective(cell);
                    return;
                }
                case EOnlineMessage.Round:
                    m_gameplayContext.RoundsManager.SetRoundIndex(reader.ReadInt32());
                    return;
                case EOnlineMessage.Timer:
                {
                    var timeLeft = reader.ReadSingle();
                    var timer = m_timer ? m_timer : m_timer = FindAnyObjectByType<TimerManager>();
                    if (timer)
                        timer.SetTimeLeft(timeLeft);
                    return;
                }
                case EOnlineMessage.TutorialStatus:
                {
                    var slot = reader.ReadByte();
                    var isReady = reader.ReadBoolean();
                    var players = GlobalContext.Instance.PlayerManager.GamePlayers;
                    if (slot < players.Count)
                        m_gameplayContext.UIManager.GetPanel<TutorialPanel>().ApplyPlayerStatus(players[slot], isReady);
                    return;
                }
            }

            var pawns = CharactersManager.Instance.CharacterPawns;
            var robotIndex = reader.ReadByte();
            if (robotIndex >= pawns.Count)
                return;
            var robot = pawns[robotIndex].ReferencesHolder;
            switch (type)
            {
                case EOnlineMessage.Move:
                {
                    var cell = ReadCell(reader);
                    if (cell)
                        robot.MovementController.ReplicateMove(cell);
                    break;
                }
                case EOnlineMessage.Bump:
                    robot.MovementController.ReplicateBump();
                    break;
                case EOnlineMessage.Turn:
                    robot.MovementController.ReplicateDirection((CharacterMovementController.EDirection)reader.ReadByte(), true);
                    break;
                case EOnlineMessage.Face:
                    robot.MovementController.ReplicateDirection((CharacterMovementController.EDirection)reader.ReadByte(), false);
                    break;
                case EOnlineMessage.Teleport:
                {
                    var cell = ReadCell(reader);
                    if (cell)
                        robot.MovementController.ReplicateTeleport(cell);
                    break;
                }
                case EOnlineMessage.Detach:
                    robot.MovementController.DistachFromCurrentCell();
                    break;
                case EOnlineMessage.Slide:
                {
                    var cell = ReadCell(reader);
                    if (cell)
                        robot.MovementController.ReplicateSlide(cell);
                    break;
                }
                case EOnlineMessage.Kill:
                {
                    var waitDuration = reader.ReadSingle();
                    robot.DeathController.CanResurrect = reader.ReadBoolean();
                    robot.DeathController.ReplicateKill(waitDuration);
                    break;
                }
                case EOnlineMessage.Resurrect:
                    robot.DeathController.Resurrect();
                    break;
                case EOnlineMessage.RespawnTarget:
                {
                    var cell = ReadCell(reader);
                    var travelDuration = reader.ReadSingle();
                    if (cell && robot.DeathController.TryGetComponent(out PlacementOnResurrectionHandler placement))
                        placement.ReplicateRespawnTarget(cell, travelDuration);
                    break;
                }
                case EOnlineMessage.Shot:
                    if (robot.SpecialAction && robot.SpecialAction.TryGetComponent(out SniperBulletShooter shooter))
                        shooter.ReplicateShot();
                    break;
                case EOnlineMessage.Score:
                    robot.ScoringController.SetScore(reader.ReadInt32());
                    break;
                case EOnlineMessage.PointCollected:
                {
                    var cell = ReadCell(reader);
                    if (cell && m_gameplayContext.ObjectiveManager is PointsRushObjectiveManager pointsRush)
                        pointsRush.ReplicatePointCollected(robot.MovementController, cell);
                    break;
                }
            }
        }

        #endregion

        #region Names of states and cells

        private string PathOf(AFlowState state)
        {
            var root = m_flowMachine.transform;
            var names = new List<string>();
            for (var transform = state.transform; transform; transform = transform.parent)
            {
                if (transform == root)
                {
                    names.Reverse();
                    return string.Join("/", names);
                }
                names.Add(transform.name);
            }
            return null;
        }

        private AFlowState StateAt(string path)
        {
            var transform = m_flowMachine.transform.Find(path);
            return transform ? transform.GetComponent<AFlowState>() : null;
        }

        private static void WriteCell(BinaryWriter writer, Cell cell)
        {
            var gridBuilder = GridBuilder.Instance;
            if (cell && gridBuilder && gridBuilder.TryGetCoordinates(cell, out var coordinates))
            {
                writer.Write((short)coordinates.x);
                writer.Write((short)coordinates.y);
                return;
            }
            writer.Write((short)-1);
            writer.Write((short)-1);
        }

        private static Cell ReadCell(BinaryReader reader)
        {
            var coordinates = new Vector2Int(reader.ReadInt16(), reader.ReadInt16());
            var gridBuilder = GridBuilder.Instance;
            if (coordinates.x < 0 || !gridBuilder)
                return null;
            return gridBuilder.TryGetCell(coordinates, out var cell) ? cell : null;
        }

        #endregion
    }
}
