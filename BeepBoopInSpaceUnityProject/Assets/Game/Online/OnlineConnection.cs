using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Game.Online
{
    /// <summary>
    /// The link between the machines of a room: Netcode for GameObjects' NetworkManager, without network objects nor
    /// scene management, carrying the room's messages. It starts through a Multiplayer Services session, which the
    /// guests join with the session's code and which connects them through Unity Relay, or straight on an address when
    /// <see cref="DirectAddress"/> is set (local network, tests: no Unity services needed).
    /// </summary>
    public class OnlineConnection
    {
        private const string k_MessageName = "BeepBoop";
        private const ushort k_DefaultDirectPort = 7777;
        private const float k_ConnectionTimeout = 15f;
        // A machine silent this long is gone (crash, network cut...). Unity Transport's 30 s leave the others frozen
        // for half a game.
        public const int k_DisconnectTimeoutMs = 10000;
        // How Netcode starts the reason it makes up for a disconnection, when the host didn't give one.
        // What Netcode itself says when a machine goes (a technical description, or the host shutting down), unlike the
        // reasons the host gives: not for the players.
        private static readonly string[] s_netcodeReasonPrefixes = { "[Disconnect Event]", "Disconnected due to " };

        /// <summary>Host and guests connect straight to this address instead of through a session ("ip" or "ip:port").</summary>
        public static string DirectAddress { get; set; } = ReadDirectAddressFromCommandLine();

        public NetworkManager NetworkManager { get; private set; }
        private ISession m_session;
        private bool m_isStopping;

        /// <summary>What guests type to join: the session's code, or the address of a direct connection.</summary>
        public string Code { get; private set; }

        public bool IsConnected => NetworkManager && NetworkManager.IsListening && !NetworkManager.ShutdownInProgress;
        public bool IsHost => IsConnected && NetworkManager.IsHost;
        public ulong LocalClientId => NetworkManager ? NetworkManager.LocalClientId : 0;

        /// <summary>Message from a machine of the room, with the id of the machine that sent it.</summary>
        public event Action<ulong, byte[]> OnMessageReceived;
        /// <summary>Host: a guest's machine is connected (approved first by <see cref="ApproveGuest"/>).</summary>
        public event Action<ulong> OnGuestConnected;
        /// <summary>Host: a guest's machine left, or was lost.</summary>
        public event Action<ulong> OnGuestDisconnected;
        /// <summary>The connection is over without this machine leaving: the host left, the network failed... With the reason.</summary>
        public event Action<string> OnConnectionLost;

        /// <summary>Host: whether a guest's machine may join, from the data it sent. Null, or the reason of the refusal.</summary>
        public Func<ulong, byte[], string> ApproveGuest { get; set; }

        public IEnumerable<ulong> GuestIds => IsHost
            ? NetworkManager.ConnectedClientsIds.Where(id => id != NetworkManager.LocalClientId)
            : Enumerable.Empty<ulong>();

        public async UniTask StartHost(int maxMachines, CancellationToken cancellationToken)
        {
            PrepareNetworkManager(null);
            if (DirectAddress != null)
            {
                var (address, port) = ParseAddress(DirectAddress);
                // On this computer only (tests, two copies of the game), or open to the local network.
                Transport.SetConnectionData(address, port, address == "127.0.0.1" ? "127.0.0.1" : "0.0.0.0");
                if (!NetworkManager.StartHost())
                    throw new OnlineException("Impossible d'ouvrir la salle sur ce réseau.");
                Code = DirectAddress;
            }
            else
            {
                await SignIn(cancellationToken);
                var options = new SessionOptions { MaxPlayers = maxMachines, IsPrivate = true }.WithRelayNetwork();
                try
                {
                    m_session = await MultiplayerService.Instance.CreateSessionAsync(options).AsUniTask()
                        .AttachExternalCancellation(cancellationToken);
                }
                catch (SessionException e)
                {
                    Debug.LogWarning($"[Online] The session couldn't be created: {e}");
                    throw new OnlineException(DescribeCreateError(e), e);
                }
                m_session.RemovedFromSession += HandleRemovedFromSession;
                Code = m_session.Code;
            }
            Debug.Log($"[Online] Room open, code {Code}.");
        }

        /// <param name="connectionData">What the host's <see cref="ApproveGuest"/> receives.</param>
        public async UniTask StartGuest(string code, byte[] connectionData, CancellationToken cancellationToken)
        {
            PrepareNetworkManager(connectionData);
            if (DirectAddress != null)
            {
                var (address, port) = ParseAddress(DirectAddress);
                Transport.SetConnectionData(address, port);
                if (!NetworkManager.StartClient())
                    throw new OnlineException("Impossible de se connecter à la salle.");
                Code = DirectAddress;
            }
            else
            {
                await SignIn(cancellationToken);
                try
                {
                    m_session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code).AsUniTask()
                        .AttachExternalCancellation(cancellationToken);
                }
                catch (SessionException e)
                {
                    throw new OnlineException(DescribeJoinError(e), e);
                }
                catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
                {
                    // The session started the client, which the host refused: its reason says why.
                    throw new OnlineException(RefusalReason(), e);
                }
                m_session.RemovedFromSession += HandleRemovedFromSession;
                Code = m_session.Code;
            }

            var timeout = Time.realtimeSinceStartup + k_ConnectionTimeout;
            await UniTask.WaitUntil(() => NetworkManager.IsConnectedClient || !NetworkManager.IsListening
                                                                            || Time.realtimeSinceStartup > timeout,
                cancellationToken: cancellationToken);
            if (!NetworkManager.IsConnectedClient)
                throw new OnlineException(RefusalReason());
            Debug.Log($"[Online] Joined the room {Code} as machine {NetworkManager.LocalClientId}.");
        }

        public async UniTask Stop()
        {
            if (m_isStopping)
                return;
            m_isStopping = true;
            try
            {
                var session = m_session;
                m_session = null;
                if (session != null)
                {
                    session.RemovedFromSession -= HandleRemovedFromSession;
                    try
                    {
                        // The session stops the NetworkManager itself.
                        await session.LeaveAsync();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Online] Leaving the session failed: {e.Message}");
                    }
                }
                if (NetworkManager && NetworkManager.IsListening && !NetworkManager.ShutdownInProgress)
                    NetworkManager.Shutdown();
                if (NetworkManager)
                    await UniTask.WaitWhile(() => NetworkManager && NetworkManager.ShutdownInProgress);
                Code = null;
                Debug.Log("[Online] Disconnected.");
            }
            finally
            {
                m_isStopping = false;
            }
        }

        public void SendToHost(byte[] message)
        {
            Send(message, new[] { NetworkManager.ServerClientId });
        }

        public void SendToGuests(byte[] message)
        {
            Send(message, GuestIds.ToArray());
        }

        public void SendTo(ulong clientId, byte[] message)
        {
            Send(message, new[] { clientId });
        }

        public void DisconnectGuest(ulong clientId, string reason)
        {
            if (IsHost)
                NetworkManager.DisconnectClient(clientId, reason);
        }

        private void Send(byte[] message, IReadOnlyList<ulong> clientIds)
        {
            if (!IsConnected || clientIds.Count == 0)
                return;
            using var writer = new FastBufferWriter(message.Length + 8, Allocator.Temp);
            writer.WriteValueSafe(message);
            NetworkManager.CustomMessagingManager.SendNamedMessage(k_MessageName, clientIds, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        private UnityTransport Transport => NetworkManager.GetComponent<UnityTransport>();

        // One NetworkManager for the whole run, created the first time: no network object, no scene management (each
        // machine loads the game's scenes through Addressables, the host says which).
        private void PrepareNetworkManager(byte[] connectionData)
        {
            if (!NetworkManager)
            {
                var gameObject = new GameObject("NetworkManager");
                gameObject.SetActive(false);
                var transport = gameObject.AddComponent<UnityTransport>();
                transport.DisconnectTimeoutMS = k_DisconnectTimeoutMs;
                NetworkManager = gameObject.AddComponent<NetworkManager>();
                NetworkManager.NetworkConfig = new NetworkConfig
                {
                    NetworkTransport = transport,
                    EnableSceneManagement = false,
                    ConnectionApproval = true,
                    ForceSamePrefabs = false,
                };
                NetworkManager.ConnectionApprovalCallback = HandleConnectionApproval;
                NetworkManager.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
                NetworkManager.OnTransportFailure += HandleTransportFailure;
                NetworkManager.OnClientStarted += HandleStarted;
                NetworkManager.OnServerStarted += HandleStarted;
                // Root, active: it keeps itself across scenes.
                gameObject.SetActive(true);
            }
            NetworkManager.NetworkConfig.ConnectionData = connectionData ?? Array.Empty<byte>();
        }

        // The messaging manager is created by each start.
        private void HandleStarted()
        {
            NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(k_MessageName, HandleNamedMessage);
        }

        private void HandleNamedMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte[] message);
            if (message == null || message.Length == 0)
                return;
            OnMessageReceived?.Invoke(senderClientId, message);
        }

        private void HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            // The host itself.
            if (request.ClientNetworkId == NetworkManager.ServerClientId)
            {
                response.Approved = true;
                return;
            }
            var refusal = ApproveGuest != null ? ApproveGuest(request.ClientNetworkId, request.Payload) : "La salle n'accepte personne.";
            response.Approved = refusal == null;
            response.Reason = refusal;
            if (refusal != null)
                Debug.Log($"[Online] Machine {request.ClientNetworkId} refused: {refusal}");
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (NetworkManager.IsHost && clientId != NetworkManager.LocalClientId)
                OnGuestConnected?.Invoke(clientId);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (m_isStopping)
                return;
            if (NetworkManager.IsHost)
            {
                if (clientId != NetworkManager.LocalClientId)
                    OnGuestDisconnected?.Invoke(clientId);
                return;
            }
            // A guest only hears about its own connection.
            OnConnectionLost?.Invoke(RefusalReason("La connexion avec l'hôte a été perdue."));
        }

        private void HandleTransportFailure()
        {
            if (!m_isStopping)
                OnConnectionLost?.Invoke("La connexion a été coupée.");
        }

        private void HandleRemovedFromSession()
        {
            if (!m_isStopping && !NetworkManager.IsHost)
                OnConnectionLost?.Invoke("L'hôte a fermé la salle.");
        }

        // The host's reason (a refusal, sent away), else the fallback: Netcode's own description is for the log only.
        private string RefusalReason(string fallback = "Impossible de rejoindre la salle.")
        {
            var reason = NetworkManager ? NetworkManager.DisconnectReason : null;
            if (string.IsNullOrEmpty(reason))
                return fallback;
            if (!Array.Exists(s_netcodeReasonPrefixes, prefix => reason.StartsWith(prefix, StringComparison.Ordinal)))
                return reason;
            Debug.Log($"[Online] {reason}");
            return fallback;
        }

        private static string DescribeCreateError(SessionException exception)
        {
            switch (exception.Error)
            {
                case SessionError.Forbidden:
                case SessionError.NotAuthorized:
                    return $"Les services Unity refusent d'ouvrir une salle : Relay et Lobby sont-ils activés pour ce projet ? ({exception.Error})";
                case SessionError.RateLimitExceeded:
                    return "Trop d'essais : patientez quelques secondes.";
                default:
                    return $"Impossible de créer la salle ({exception.Error}).";
            }
        }

        private static string DescribeJoinError(SessionException exception)
        {
            switch (exception.Error)
            {
                case SessionError.SessionNotFound:
                case SessionError.InvalidSessionIdentifier:
                case SessionError.InvalidParameter:
                    return "Aucune salle n'a ce code.";
                case SessionError.Forbidden:
                    return "Cette salle est pleine ou fermée.";
                case SessionError.RateLimitExceeded:
                    return "Trop d'essais : patientez quelques secondes.";
                default:
                    return $"Impossible de rejoindre la salle ({exception.Error}).";
            }
        }

        private static async UniTask SignIn(CancellationToken cancellationToken)
        {
            // Unity's services only start for a project linked to Unity Cloud, whose id the build carries.
            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                Debug.LogWarning("[Online] The project isn't linked to Unity Cloud (Edit > Project Settings > Services): " +
                                 "rooms with a code need it. -online-direct plays on a local network without it.");
                throw new OnlineException("Salles en ligne indisponibles : le projet n'est pas lié à Unity Cloud (Project Settings > Services).");
            }
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    var options = new InitializationOptions();
                    // Two copies of the game on one computer must sign in as two players.
                    options.SetProfile(InstanceProfile);
                    await UnityServices.InitializeAsync(options).AsUniTask().AttachExternalCancellation(cancellationToken);
                }
                await UniTask.WaitWhile(() => UnityServices.State == ServicesInitializationState.Initializing,
                    cancellationToken: cancellationToken);
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync().AsUniTask().AttachExternalCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Online] Unity services unavailable: {e}");
                throw new OnlineException("Les services en ligne ne répondent pas : vérifiez la connexion à Internet.", e);
            }
        }

        // Held until the game quits: the next copy of the game takes the next profile.
        private static readonly List<Mutex> s_profileMutexes = new();
        private static string s_instanceProfile;

        // "instance0" for the first copy of the game running on the computer, "instance1" for the second...
        private static string InstanceProfile
        {
            get
            {
                if (s_instanceProfile != null)
                    return s_instanceProfile;
                for (int i = 0; i < 8; i++)
                {
                    var mutex = new Mutex(true, $"Local\\BeepBoopInSpace-OnlineProfile-{i}", out var createdNew);
                    if (createdNew)
                    {
                        s_profileMutexes.Add(mutex);
                        return s_instanceProfile = $"instance{i}";
                    }
                    mutex.Dispose();
                }
                return s_instanceProfile = $"instance{UnityEngine.Random.Range(8, 1000)}";
            }
        }

        private static (string address, ushort port) ParseAddress(string address)
        {
            var parts = address.Split(':');
            var port = parts.Length > 1 && ushort.TryParse(parts[1], out var parsedPort) ? parsedPort : k_DefaultDirectPort;
            return (parts[0], port);
        }

        // -online-direct [ip[:port]]: the room on a local network, without Unity services.
        private static string ReadDirectAddressFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != "-online-direct")
                    continue;
                return i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[i + 1] : $"127.0.0.1:{k_DefaultDirectPort}";
            }
            return null;
        }
    }

    /// <summary>A room couldn't be opened or joined, with a message for the players.</summary>
    public class OnlineException : Exception
    {
        public OnlineException(string message, Exception innerException = null) : base(message, innerException)
        {}
    }
}
