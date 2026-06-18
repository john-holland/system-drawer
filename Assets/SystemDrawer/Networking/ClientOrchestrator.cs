using System;
using Locomotion.Narrative;
using UnityEngine;
using Weather.Executor;

/// <summary>Scene-local client networking singleton.</summary>
[AddComponentMenu("System Drawer/Networking/Client Orchestrator")]
[DisallowMultipleComponent]
public sealed class ClientOrchestrator : MonoBehaviour
{
    public const string ServiceKey = SystemDrawerServiceKeys.NetworkClientOrchestrator;

    public static ClientOrchestrator Instance { get; private set; }

    [SerializeField] NetworkSettings settings;
    [SerializeField] NetworkServerMode mode = NetworkServerMode.SinglePlayer;
    [SerializeField] NetworkClientRole clientRole = NetworkClientRole.Player;
    [SerializeField] string clientId = "local";
    [SerializeField] string host = "127.0.0.1";
    [SerializeField] int port = 7777;
    [SerializeField] string lobbyPasswordHash;

    NetworkConnectionState _state = NetworkConnectionState.Disconnected;
    TcpTreeStreamChannel _tcp;
    UdpDecisionChannel _udp;
    INetworkSpatialOrchestrator _orchestrator;
    ImpersonationSession _impersonation;
    NarrativeTimeTravelCoordinator _narrativeCoordinator;

    public NetworkServerMode Mode => mode;
    public NetworkServerMode CurrentMode => mode;
    public NetworkClientRole ClientRole => clientRole;
    public string ClientId => clientId;
    public NetworkConnectionState ConnectionState => _state;
    public INetworkSpatialOrchestrator Orchestrator => _orchestrator ??= new NetworkSpatialOrchestratorAdapter(mode);
    public TcpTreeStreamChannel Tcp => _tcp ??= new TcpTreeStreamChannel();
    public UdpDecisionChannel Udp => _udp ??= new UdpDecisionChannel();
    public ImpersonationSession Impersonation => _impersonation;
    public bool IsSpectator => clientRole == NetworkClientRole.Spectator;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (Instance != null && Instance != this)
            return;
        Instance = this;
        if (settings == null)
            settings = NetworkSettings.Default;
        port = settings.gamePort;
        RegisterService();
        Tcp.MessageReceived -= OnTcpMessage;
        Tcp.MessageReceived += OnTcpMessage;
        Udp.MessageReceived -= OnUdpMessage;
        Udp.MessageReceived += OnUdpMessage;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        Disconnect();
        UnregisterService();
    }

    void RegisterService()
    {
        var svc = SystemDrawerService.FindInScene();
        svc?.Register(ServiceKey, this);
    }

    void UnregisterService()
    {
        var svc = SystemDrawerService.FindInScene();
        svc?.Unregister(ServiceKey);
    }

    public void SetServerMode(NetworkServerMode newMode)
    {
        mode = newMode;
        _orchestrator = new NetworkSpatialOrchestratorAdapter(mode);
    }

    public void SetLobbyPasswordHash(string hash) => lobbyPasswordHash = hash;

    public void Connect(string connectHost, int connectPort) =>
        Connect(connectHost, connectPort, NetworkClientRole.Player);

    public void ConnectAsSpectator(string connectHost, int connectPort) =>
        Connect(connectHost, connectPort, NetworkClientRole.Spectator);

    public void Connect(string connectHost, int connectPort, NetworkClientRole role)
    {
        clientRole = role;
        host = connectHost;
        port = connectPort;
        _state = NetworkConnectionState.Connecting;
        try
        {
            if (_impersonation != null)
            {
                Tcp.Connect(_impersonation.Host, _impersonation.Port);
                if (!IsSpectator)
                    Udp.Connect(_impersonation.Host, _impersonation.Port + 1);
            }
            else
            {
                Tcp.Connect(host, port);
                if (!IsSpectator)
                    Udp.Connect(host, port + 1);
            }
            _state = NetworkConnectionState.Connected;
            SendHello("hello");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[ClientOrchestrator] Connect failed: " + ex.Message);
            _state = NetworkConnectionState.Disconnected;
        }
    }

    public void ConnectLoopback(int loopPort)
    {
        host = "127.0.0.1";
        port = loopPort;
        clientRole = NetworkClientRole.Player;
        _state = NetworkConnectionState.Loopback;
        Tcp.Connect(host, port);
        Udp.Connect(host, port + 1);
        SendHello("loopback");
    }

    void SendHello(string type)
    {
        string payload = NetworkHelloPayload.Build(clientId, clientRole, lobbyPasswordHash);
        Tcp.Send(NetworkMessageEnvelope.Create("TreeStreamChannel", type, payload));
    }

    public void Disconnect()
    {
        _tcp?.Stop();
        _udp?.Stop();
        _state = NetworkConnectionState.Disconnected;
    }

    public void BindImpersonation(ImpersonationSession session)
    {
        _impersonation = session;
        if (session != null)
            clientId = session.ClientId;
    }

    public bool RequestLoadScene(string sceneId) => Orchestrator.RequestLoadScene(sceneId);

    public int SendDecision(NetworkMessageEnvelope envelope)
    {
        if (IsSpectator)
            return -1;
        return Udp.SendDecision(envelope);
    }

    public void RequestNarrativeRewind(float targetNarrativeTime, string requesterId = null)
    {
        if (IsSpectator)
            return;
        string id = requesterId ?? clientId;
        string json = "{\"seq\":" + DateTime.UtcNow.Ticks + ",\"targetTime\":" + targetNarrativeTime.ToString("R") + ",\"requesterId\":\"" + id + "\"}";
        Tcp.Send(NetworkMessageEnvelope.Create("TreeStreamChannel", "narrativeRewindRequest", json));
    }

    void OnTcpMessage(NetworkMessageEnvelope env)
    {
        if (env == null)
            return;
        switch (env.Type)
        {
            case "loadScene":
                Orchestrator.RequestLoadScene(env.PayloadJson);
                WeatherNetworkSink.OnSceneLoad?.Invoke(env.PayloadJson);
                break;
            case "narrativeRewindApply":
                ApplyNarrativeRewind(env.PayloadJson);
                break;
            case "narrativeCheckpointPush":
                ApplyNarrativeCheckpointPush(env.PayloadJson);
                break;
            case "weatherEggApply":
                ApplyWeatherEggApply(env.PayloadJson);
                break;
            case "weatherEggBootstrap":
                ApplyWeatherEggBootstrap(env.PayloadJson);
                break;
        }
    }

    public void SendWeatherEggPush(string json)
    {
        if (string.IsNullOrEmpty(json))
            return;
        Tcp.Send(NetworkMessageEnvelope.Create("TreeStreamChannel", "weatherEggPush", json));
    }

    void ApplyWeatherEggApply(string json)
    {
        FindAnyObjectByType<WeatherLodNetworkBridge>()?.HandleEggApplyJson(json);
    }

    void ApplyWeatherEggBootstrap(string json)
    {
        FindAnyObjectByType<WeatherLodNetworkBridge>()?.HandleEggBootstrapJson(json);
    }

    void ApplyNarrativeRewind(string json)
    {
        if (string.IsNullOrEmpty(json))
            return;
        _narrativeCoordinator ??= FindAnyObjectByType<NarrativeTimeTravelCoordinator>();
        if (_narrativeCoordinator == null)
            return;
        var checkpoint = JsonUtility.FromJson<NarrativeTimeTravelCheckpoint>(json);
        if (checkpoint != null)
            _narrativeCoordinator.ApplyRewindLocal(checkpoint);
        WeatherNetworkSink.OnRewindApplied?.Invoke();
    }

    void ApplyNarrativeCheckpointPush(string json)
    {
        if (string.IsNullOrEmpty(json))
            return;
        _narrativeCoordinator ??= FindAnyObjectByType<NarrativeTimeTravelCoordinator>();
        if (_narrativeCoordinator == null)
            return;
        var checkpoint = JsonUtility.FromJson<NarrativeTimeTravelCheckpoint>(json);
        if (checkpoint != null)
            _narrativeCoordinator.MergeCheckpoint(checkpoint);
    }

    void OnUdpMessage(NetworkMessageEnvelope env)
    {
        if (env == null || env.Type == null)
            return;
        if (env.Type.StartsWith("ownership:", StringComparison.Ordinal))
        {
            if (IsSpectator)
                return;
            var parts = env.Type.Split(':');
            if (parts.Length >= 3)
                Debug.Log($"[ClientOrchestrator] ownership {parts[1]} -> {parts[2]}");
        }
    }
}
