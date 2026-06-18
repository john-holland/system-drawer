using UnityEngine;
using Weather.Executor;

/// <summary>Wires WeatherExecutorService to TreeStreamChannel and LOD pre-warm.</summary>
[AddComponentMenu("System Drawer/Networking/Weather LOD Network Bridge")]
public sealed class WeatherLodNetworkBridge : MonoBehaviour
{
    public const string WeatherExecutorTreeId = "weather.executor";

    [SerializeField] ServerOrchestrator serverOrchestrator;
    [SerializeField] ClientOrchestrator clientOrchestrator;
    [SerializeField] Transform observerTransform;
    [SerializeField] float prewarmInterval = 0.5f;

    Weather.Executor.WeatherExecutorService _executor;
    float _lastPrewarmTime = -999f;

    void Awake()
    {
        if (serverOrchestrator == null)
            serverOrchestrator = FindAnyObjectByType<ServerOrchestrator>();
        if (clientOrchestrator == null)
            clientOrchestrator = FindAnyObjectByType<ClientOrchestrator>();
        _executor = Weather.Executor.WeatherExecutorService.Instance
            ?? FindAnyObjectByType<Weather.Executor.WeatherExecutorService>();
    }

    void OnEnable()
    {
        WeatherNetworkSink.SendPush = SendEggPush;
        WeatherNetworkSink.BroadcastApply = BroadcastEggApply;
        WeatherNetworkSink.BroadcastBootstrap = BroadcastEggBootstrap;
        RegisterWeatherTree();
        if (_executor != null && serverOrchestrator != null)
            _executor.SetServerMode(true);
    }

    void OnDisable()
    {
        if (WeatherNetworkSink.SendPush == SendEggPush)
            WeatherNetworkSink.SendPush = null;
        if (WeatherNetworkSink.BroadcastApply == BroadcastEggApply)
            WeatherNetworkSink.BroadcastApply = null;
        if (WeatherNetworkSink.BroadcastBootstrap == BroadcastEggBootstrap)
            WeatherNetworkSink.BroadcastBootstrap = null;
    }

    void Update()
    {
        if (serverOrchestrator == null || observerTransform == null)
            return;
        if (Time.time - _lastPrewarmTime < prewarmInterval)
            return;
        _lastPrewarmTime = Time.time;
        string clientId = clientOrchestrator != null ? clientOrchestrator.ClientId : "local";
        foreach (string treeId in serverOrchestrator.LodScheduler.TreesToPreWarm(observerTransform.position, observerTransform.position))
        {
            if (treeId == WeatherExecutorTreeId)
                serverOrchestrator.StreamTreeDelta(treeId, clientId);
        }
    }

    void RegisterWeatherTree()
    {
        if (serverOrchestrator == null)
            return;
        serverOrchestrator.TreeRegistry.Register(new NetworkTreeDescriptor
        {
            TreeId = WeatherExecutorTreeId,
            Dimension = TreeDimension.Spatial3D,
            TransmitPolicy = TreeTransmitPolicy.ServerAuthoritative,
            StreamForOwnership = true,
            CausalityLeafPrefix = "weather"
        });
    }

    void SendEggPush(WeatherEggClientPayload payload)
    {
        if (payload == null)
            return;
        if (serverOrchestrator != null && clientOrchestrator == null)
        {
            _executor?.HandleClientPush(payload);
            return;
        }
        string json = WeatherEggPayloadSerializer.ToJson(payload);
        clientOrchestrator?.SendWeatherEggPush(json);
    }

    void BroadcastEggApply(WeatherEggApplyPayload payload)
    {
        if (payload == null || serverOrchestrator == null)
            return;
        string json = WeatherEggPayloadSerializer.ToJson(payload);
        serverOrchestrator.BroadcastWeatherEggApply(json);
    }

    void BroadcastEggBootstrap(WeatherEggBootstrapPayload payload)
    {
        if (payload == null || serverOrchestrator == null)
            return;
        string json = WeatherEggPayloadSerializer.ToJson(payload);
        serverOrchestrator.BroadcastWeatherEggBootstrap(json);
    }

    public void HandleEggApplyJson(string json)
    {
        var payload = WeatherEggPayloadSerializer.FromJson<WeatherEggApplyPayload>(json);
        _executor?.ApplyServerPayload(payload);
    }

    public void HandleEggBootstrapJson(string json)
    {
        var payload = WeatherEggPayloadSerializer.FromJson<WeatherEggBootstrapPayload>(json);
        _executor?.BootstrapEgg(payload);
    }

    public void HandleClientPushJson(string json)
    {
        var payload = WeatherEggPayloadSerializer.FromJson<WeatherEggClientPayload>(json);
        _executor?.HandleClientPush(payload);
    }
}
