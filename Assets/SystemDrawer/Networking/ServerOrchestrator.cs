using System;
using System.Collections;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>Game/server orchestrator: client registry, trees, lobby, LOD.</summary>
[AddComponentMenu("System Drawer/Networking/Server Orchestrator")]
[DisallowMultipleComponent]
public sealed class ServerOrchestrator : MonoBehaviour
{
    public const string ServiceKey = SystemDrawerServiceKeys.NetworkServerOrchestrator;

    [SerializeField] NetworkSettings settings;
    [SerializeField] NetworkServerMode mode = NetworkServerMode.SinglePlayer;
    [SerializeField] string bindAddress = "0.0.0.0";
    [SerializeField] int listenPort = 7777;
    [SerializeField] bool lobbyLockedByLaunchArgs;
    [SerializeField] bool allowSpectators = true;
    [SerializeField] int maxSpectators = 4;
    [SerializeField] string lobbyPassword;

    readonly NetworkTreeRegistry _treeRegistry = new NetworkTreeRegistry();
    readonly Dictionary<string, string> _clients = new Dictionary<string, string>();
    readonly Dictionary<string, NetworkClientRole> _clientRoles = new Dictionary<string, NetworkClientRole>();
    LockstepDecisionValidator _lockstep;

    TcpTreeStreamChannel _tcp;
    UdpDecisionChannel _udp;
    LobbyServerHost _lobby;
    NetworkLodScheduler _lod;
    INetworkSpatialOrchestrator _orchestrator;
    ClientOrchestrator _boundClient;
    ImpersonationSession _impersonation;
    NarrativeTimeTravelCoordinator _narrativeCoordinator;
    bool _isListening;
    int _listeningPort = -1;
    int _activeLobbyPort = -1;
    string _lobbyPasswordHash = "";
    int _maxPlayers = 8;
    long _lastRewindSeq;
    Coroutine _lobbyHeartbeat;

    public NetworkSettings Settings => settings;
    public NetworkServerMode Mode => mode;
    public bool IsDedicated { get; private set; }
    public bool AllowSpectators => allowSpectators;
    public NetworkTreeRegistry TreeRegistry => _treeRegistry;
    public NetworkLodScheduler LodScheduler => _lod ??= new NetworkLodScheduler(settings, _treeRegistry);
    public INetworkSpatialOrchestrator Orchestrator => _orchestrator ??= new NetworkSpatialOrchestratorAdapter(mode);
    public bool IsLobbyHosting => _lobby != null && _lobby.IsRunning;
    public string LobbyAdvertiseAddress => _lobby?.AdvertiseAddress ?? bindAddress;
    public int ClientCount => _clients.Count;
    public int MaxPlayers => _maxPlayers;
    public int ListenPort => _listeningPort > 0 ? _listeningPort : listenPort;
    public int ActiveLobbyPort => _activeLobbyPort;
    public bool LobbyLockedByLaunchArgs => lobbyLockedByLaunchArgs;
    public string LobbyPasswordPlaintext => lobbyPassword ?? "";
    public GameSessionHost GameSessions { get; private set; }

    public int PlayerCount
    {
        get
        {
            int n = 0;
            foreach (var pair in _clientRoles)
            {
                if (pair.Value != NetworkClientRole.Spectator)
                    n++;
            }
            return n;
        }
    }

    public string LastHelloRejectReason { get; private set; }

    public void SetLobbyPassword(string password)
    {
        EnsureReady();
        lobbyPassword = password ?? "";
        _lobbyPasswordHash = LobbyPasswordHash.Hash(lobbyPassword, settings.lobbySessionName);
    }

    public void EnsureReady()
    {
        if (settings == null)
            settings = NetworkSettings.Default;
        allowSpectators = settings.allowSpectators;
        maxSpectators = settings.maxSpectators;
        _maxPlayers = settings.maxPlayers;
        if (_lockstep == null)
            _lockstep = new LockstepDecisionValidator(_treeRegistry);
        if (GameSessions == null)
            GameSessions = GetComponent<GameSessionHost>() ?? gameObject.AddComponent<GameSessionHost>();
        GameSessions.lobbySessionName = settings != null ? settings.lobbySessionName : GameSessions.lobbySessionName;
        GameSessions.treeRegistry = _treeRegistry;
        GameSessions.lockstep = _lockstep;
        if (settings != null && settings.prefab != null)
            GameSessions.prefab = settings.prefab;
        GameSessions.SessionsChanged -= OnGameSessionsChanged;
        GameSessions.SessionsChanged += OnGameSessionsChanged;
    }

    void OnGameSessionsChanged()
    {
        if (IsLobbyHosting)
            GameLobbyContinuuuumClient.Heartbeat(this);
    }

    public void ApplyLobbyPrefab(LobbyPrefabParameters prefab)
    {
        EnsureReady();
        if (prefab == null)
            return;
        if (settings.prefab == null)
            settings.prefab = new LobbyPrefabParameters();
        settings.prefab = prefab.Clone();
        _maxPlayers = prefab.gameSize > 0 ? prefab.gameSize : settings.maxPlayers;
        settings.maxPlayers = _maxPlayers;
        maxSpectators = prefab.maxSpectators;
        allowSpectators = prefab.allowSpectators;
        settings.maxSpectators = prefab.maxSpectators;
        settings.allowSpectators = prefab.allowSpectators;
        SetMode(prefab.mode);
        if (GameSessions != null)
            GameSessions.prefab = settings.prefab;
    }

    public void SetLobbyLockedByLaunchArgs(bool locked) => lobbyLockedByLaunchArgs = locked;

    public void ApplyHostOptions(LobbyHostOptions options)
    {
        EnsureReady();
        if (options == null)
            return;
        if (!string.IsNullOrEmpty(options.sessionName))
            settings.lobbySessionName = options.sessionName;
        _maxPlayers = options.maxPlayers > 0 ? options.maxPlayers : settings.maxPlayers;
        maxSpectators = options.maxSpectators >= 0 ? options.maxSpectators : settings.maxSpectators;
        allowSpectators = options.allowSpectators;
        if (settings.allowLobbyPassword && !string.IsNullOrEmpty(options.password))
            lobbyPassword = options.password;
        else if (string.IsNullOrEmpty(options.password))
            lobbyPassword = "";
        _lobbyPasswordHash = LobbyPasswordHash.Hash(lobbyPassword, settings.lobbySessionName);
        if (options.prefab != null)
        {
            if (options.prefab.gameSize <= 0)
                options.prefab.gameSize = _maxPlayers;
            if (options.minPlayersToStart > 0)
                options.prefab.minPlayersToStart = options.minPlayersToStart;
            ApplyLobbyPrefab(options.prefab);
        }
        else if (options.minPlayersToStart > 0 && settings.prefab != null)
            settings.prefab.minPlayersToStart = options.minPlayersToStart;
    }

    void Awake()
    {
        EnsureReady();
        listenPort = settings.gamePort;
        RegisterService();
        NetworkLaunchArgs.Parse();
        NetworkLaunchArgs.ApplyTo(this);
    }

    void OnDestroy()
    {
        StopLobbyHost();
        StopListening();
        UnregisterService();
    }

    void RegisterService()
    {
        var svc = SystemDrawerService.FindInScene();
        svc?.Register(ServiceKey, this);
        svc?.Register(SystemDrawerServiceKeys.NetworkServerMode, this);
    }

    void UnregisterService()
    {
        var svc = SystemDrawerService.FindInScene();
        svc?.Unregister(ServiceKey);
        svc?.Unregister(SystemDrawerServiceKeys.NetworkServerMode);
        if (_lobby != null)
            svc?.Unregister(SystemDrawerServiceKeys.NetworkLobbyServer);
    }

    public void ConfigureDedicated(string address, int port, NetworkServerMode serverMode, bool noLobby)
    {
        bindAddress = address;
        listenPort = port;
        mode = serverMode;
        IsDedicated = true;
        lobbyLockedByLaunchArgs = noLobby;
        StartListening(port);
    }

    public void SetMode(NetworkServerMode newMode)
    {
        mode = newMode;
        _orchestrator = new NetworkSpatialOrchestratorAdapter(mode);
    }

    public void StartListening(int port = 0)
    {
        EnsureReady();
        if (port > 0)
            listenPort = port;
        if (_isListening && _listeningPort == listenPort)
            return;
        StopListening();
        _tcp ??= new TcpTreeStreamChannel();
        _udp ??= new UdpDecisionChannel();
        _tcp.MessageReceived -= OnTcpMessage;
        _tcp.MessageReceived += OnTcpMessage;
        _udp.MessageReceived -= OnUdpMessage;
        _udp.MessageReceived += OnUdpMessage;
        _tcp.Listen(bindAddress, listenPort);
        _udp.Bind(bindAddress, listenPort + 1);
        _isListening = true;
        _listeningPort = listenPort;
    }

    public void StopListening()
    {
        _tcp?.Stop();
        _udp?.Stop();
        _isListening = false;
        _listeningPort = -1;
    }

    public void StartLobbyHost(int lobbyPort = 0, string sessionName = null)
    {
        var options = new LobbyHostOptions
        {
            sessionName = sessionName ?? settings.lobbySessionName,
            maxPlayers = _maxPlayers,
            maxSpectators = maxSpectators,
            allowSpectators = allowSpectators,
            password = lobbyPassword,
            lobbyPort = lobbyPort
        };
        StartLobbyHost(options);
    }

    public void StartLobbyHost(LobbyHostOptions options)
    {
        EnsureReady();
        if (lobbyLockedByLaunchArgs)
        {
            if (Application.isPlaying)
                Debug.LogWarning("[ServerOrchestrator] Lobby disabled by --no-lobby");
            return;
        }
        ApplyHostOptions(options);
        int lp = options != null && options.lobbyPort > 0 ? options.lobbyPort : settings.lobbyPort;
        string name = settings.lobbySessionName;
        if (_lobby != null && _lobby.IsRunning && _activeLobbyPort == lp)
            return;
        _lobby ??= new LobbyServerHost();
        _lobby.Start(bindAddress, lp, listenPort, name, _maxPlayers, maxSpectators, allowSpectators, _lobbyPasswordHash);
        _activeLobbyPort = lp;
        var svc = SystemDrawerService.FindInScene();
        svc?.Register(SystemDrawerServiceKeys.NetworkLobbyServer, this);
        if (GameSessions != null && (GameSessions.sessions == null || GameSessions.sessions.Count == 0))
            GameSessions.CreateSession(name);
        GameLobbyContinuuuumClient.Heartbeat(this);
        if (Application.isPlaying)
        {
            if (_lobbyHeartbeat != null)
                StopCoroutine(_lobbyHeartbeat);
            _lobbyHeartbeat = StartCoroutine(LobbyHeartbeatLoop());
        }
    }

    IEnumerator LobbyHeartbeatLoop()
    {
        var wait = new WaitForSeconds(5f);
        while (_lobby != null && _lobby.IsRunning)
        {
            yield return GameLobbyContinuuuumClient.HeartbeatRoutine(this);
            yield return wait;
        }
        _lobbyHeartbeat = null;
    }

    public void StopLobbyHost()
    {
        if (_lobbyHeartbeat != null)
        {
            StopCoroutine(_lobbyHeartbeat);
            _lobbyHeartbeat = null;
        }
        bool wasRunning = _lobby != null && _lobby.IsRunning;
        string name = settings != null ? settings.lobbySessionName : GameSessions != null ? GameSessions.lobbySessionName : "";
        if (wasRunning && !string.IsNullOrEmpty(name))
            GameLobbyContinuuuumClient.CloseLobby(name);
        _lobby?.Stop();
        _activeLobbyPort = -1;
        var svc = SystemDrawerService.FindInScene();
        svc?.Unregister(SystemDrawerServiceKeys.NetworkLobbyServer);
    }

    public void CopyHeartbeatPlayerIds(List<string> dest)
    {
        CopyNonSpectatorPlayerIds(dest);
        if (dest.Count == 0)
            _lobby?.CopyPendingPlayerIds(dest);
    }

    public void CopyNonSpectatorPlayerIds(List<string> dest)
    {
        if (dest == null) return;
        dest.Clear();
        foreach (var pair in _clientRoles)
        {
            if (pair.Value != NetworkClientRole.Spectator)
                dest.Add(pair.Key);
        }
    }

    public void RegisterClient(string clientId, string endpoint = "", NetworkClientRole role = NetworkClientRole.Player)
    {
        if (string.IsNullOrEmpty(clientId))
            return;
        _clients[clientId] = endpoint ?? "";
        _clientRoles[clientId] = role;
    }

    public bool TryGetClientRole(string clientId, out NetworkClientRole role) =>
        _clientRoles.TryGetValue(clientId, out role);

    public void KickClient(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
            return;
        _clients.Remove(clientId);
        _clientRoles.Remove(clientId);
        _tcp?.Send(NetworkMessageEnvelope.Create("TreeStreamChannel", "kick", clientId));
    }

    public bool TransferTreeOwnership(string treeId, string clientId)
    {
        if (TryGetClientRole(clientId, out NetworkClientRole role) && role == NetworkClientRole.Spectator)
            return false;
        if (!_treeRegistry.TransferOwnership(treeId, clientId))
            return false;
        _udp?.SendDecision(NetworkMessageEnvelope.Create("DecisionChannel", "ownership:" + treeId + ":" + clientId, clientId));
        return true;
    }

    public bool RequestLoadScene(string sceneId, string clientId = null)
    {
        if (string.IsNullOrEmpty(sceneId))
            return false;
        _tcp?.Send(NetworkMessageEnvelope.Create("TreeStreamChannel", "loadScene", sceneId));
        return Orchestrator.RequestLoadScene(sceneId);
    }

    public bool ValidateLockstepDecision(string clientId, string causalityLeafId, out string reason)
    {
        if (TryGetClientRole(clientId, out NetworkClientRole role) && role == NetworkClientRole.Spectator)
        {
            reason = "spectator cannot decide";
            return false;
        }
        return _lockstep.TryValidateDecision(clientId, causalityLeafId, out reason);
    }

    public CausalityFamilyAudit.AuditResult AuditClientTrees() => CausalityFamilyAudit.ValidateTreeRegistry(_treeRegistry);

    public void BindClientOrchestrator(ClientOrchestrator client)
    {
        _boundClient = client;
        if (client == null)
            return;
        RegisterClient(client.ClientId, "", client.ClientRole);
    }

    public void StartSinglePlayerLoopback()
    {
        EnsureReady();
        var client = ClientOrchestrator.Instance;
        if (client == null)
            client = FindAnyObjectByType<ClientOrchestrator>();
        if (client == null)
        {
            var go = new GameObject("ClientOrchestrator");
            client = go.AddComponent<ClientOrchestrator>();
        }
        client.EnsureInitialized();
        mode = NetworkServerMode.SinglePlayer;
        StartListening(listenPort);
        BindClientOrchestrator(client);
        client.SetServerMode(NetworkServerMode.SinglePlayer);
        client.ConnectLoopback(listenPort);
    }

    public ImpersonationSession ImpersonateClient(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
            return null;
        _impersonation = new ImpersonationSession(clientId, "127.0.0.1", listenPort);
        var client = ClientOrchestrator.Instance ?? FindAnyObjectByType<ClientOrchestrator>();
        client?.BindImpersonation(_impersonation);
        client?.ConnectLoopback(listenPort);
        return _impersonation;
    }

    public void StreamTreeDelta(string treeId, string clientId)
    {
        if (!_treeRegistry.TryGet(treeId, out var desc))
            return;
        if (!LodScheduler.MarkWarmed(treeId))
            return;
        if (TryGetClientRole(clientId, out NetworkClientRole role) && role == NetworkClientRole.Spectator)
            desc.TransmitPolicy = TreeTransmitPolicy.SpectatorReadOnly;
        _tcp?.Send(NetworkMessageEnvelope.Create("TreeStreamChannel", "treeDelta", treeId));
    }

    void OnTcpMessage(NetworkMessageEnvelope env)
    {
        if (env == null)
            return;
        switch (env.Type)
        {
            case "hello":
            case "loopback":
                HandleHello(env);
                break;
            case "save":
                HandleSave(env.PayloadJson);
                break;
            case "load":
                HandleLoad(env.PayloadJson);
                break;
            case "narrativeRewindRequest":
                HandleNarrativeRewindRequest(env.PayloadJson);
                break;
            case "weatherEggPush":
                HandleWeatherEggPush(env.PayloadJson);
                break;
        }
    }

    public void BroadcastWeatherEggApply(string json)
    {
        if (string.IsNullOrEmpty(json))
            return;
        _tcp?.Send(NetworkMessageEnvelope.Create("TreeStreamChannel", "weatherEggApply", json));
    }

    public void BroadcastWeatherEggBootstrap(string json)
    {
        if (string.IsNullOrEmpty(json))
            return;
        _tcp?.Send(NetworkMessageEnvelope.Create("TreeStreamChannel", "weatherEggBootstrap", json));
    }

    void HandleWeatherEggPush(string json)
    {
        if (string.IsNullOrEmpty(json))
            return;
        var bridge = FindAnyObjectByType<WeatherLodNetworkBridge>();
        bridge?.HandleClientPushJson(json);
    }

    void HandleHello(NetworkMessageEnvelope env)
    {
        NetworkHelloPayload.Parse(env.PayloadJson, out string clientId, out NetworkClientRole role, out string passwordHash);
        if (!TryAcceptHello(clientId, role, passwordHash, out string reason))
        {
            Debug.LogWarning("[ServerOrchestrator] hello rejected: " + reason);
            KickClient(clientId);
        }
    }

    public bool TryAcceptHello(string clientId, NetworkClientRole role, string passwordHash, out string reason)
    {
        LastHelloRejectReason = "";
        reason = "";
        if (!string.IsNullOrEmpty(_lobbyPasswordHash))
        {
            if (string.IsNullOrEmpty(passwordHash) || passwordHash != _lobbyPasswordHash)
            {
                reason = "password";
                LastHelloRejectReason = reason;
                return false;
            }
        }
        if (role == NetworkClientRole.Spectator)
        {
            if (!allowSpectators)
            {
                reason = "spectators disabled";
                LastHelloRejectReason = reason;
                return false;
            }
            int specCount = 0;
            foreach (var pair in _clientRoles)
            {
                if (pair.Value == NetworkClientRole.Spectator)
                    specCount++;
            }
            if (specCount >= maxSpectators)
            {
                reason = "spectator cap";
                LastHelloRejectReason = reason;
                return false;
            }
        }
        else
        {
            if (PlayerCount >= _maxPlayers)
            {
                reason = "player cap";
                LastHelloRejectReason = reason;
                return false;
            }
        }
        RegisterClient(clientId, "", role);
        return true;
    }

    void OnUdpMessage(NetworkMessageEnvelope env)
    {
        if (env == null || env.Type == null || !env.Type.StartsWith("decision:", StringComparison.Ordinal))
            return;
        string leaf = env.PayloadJson;
        string clientId = env.PayloadJson;
        if (!ValidateLockstepDecision(clientId, leaf, out string reason))
            Debug.LogWarning("[ServerOrchestrator] decision rejected: " + reason);
    }

    void HandleNarrativeRewindRequest(string json)
    {
        if (string.IsNullOrEmpty(json))
            return;
        var req = JsonUtility.FromJson<NarrativeRewindRequestDto>(json);
        if (req == null)
            return;
        if (req.seq <= _lastRewindSeq)
            return;
        if (mode == NetworkServerMode.ClassicLockstep)
        {
            var audit = AuditClientTrees();
            if (!audit.Ok)
            {
                Debug.LogWarning("[ServerOrchestrator] narrative rewind rejected: " + audit.Reason);
                return;
            }
        }
        _narrativeCoordinator ??= FindAnyObjectByType<NarrativeTimeTravelCoordinator>();
        if (_narrativeCoordinator == null)
            return;
        var checkpoint = _narrativeCoordinator.BuildCheckpointForTime(req.targetTime, req.requesterId);
        if (checkpoint == null)
            return;
        checkpoint.rewindSeq = req.seq;
        checkpoint.authorityClientId = req.requesterId;
        _lastRewindSeq = req.seq;
        string payload = JsonUtility.ToJson(checkpoint);
        _tcp?.Send(NetworkMessageEnvelope.Create("TreeStreamChannel", "narrativeRewindApply", payload));
        _narrativeCoordinator.ApplyRewindLocal(checkpoint);
    }

    public void BroadcastNarrativeCheckpoint(NarrativeTimeTravelCheckpoint checkpoint)
    {
        if (checkpoint == null)
            return;
        _tcp?.Send(NetworkMessageEnvelope.Create("TreeStreamChannel", "narrativeCheckpointPush", JsonUtility.ToJson(checkpoint)));
    }

    public void HandleSavePublic(string slotId) => HandleSave(slotId);
    public void HandleLoadPublic(string slotId) => HandleLoad(slotId);

    void HandleSave(string slotId)
    {
        Debug.Log("[ServerOrchestrator] save slot=" + slotId);
    }

    void HandleLoad(string slotId)
    {
        Debug.Log("[ServerOrchestrator] load slot=" + slotId);
        RequestLoadScene(slotId);
    }
}

[Serializable]
public sealed class NarrativeRewindRequestDto
{
    public long seq;
    public float targetTime;
    public string requesterId;
}
