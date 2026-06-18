using UnityEngine;

/// <summary>Registers networking orchestrators on SystemDrawerService.</summary>
[AddComponentMenu("System Drawer/Networking/Network Service Wizard")]
public sealed class NetworkServiceWizard : MonoBehaviour
{
    public const string ServiceKey = "network.wizard";
    public const string SpatialOrchestratorKey = "Spatial4DOrchestrator";

    [Tooltip("Scene client orchestrator.")]
    public ClientOrchestrator clientOrchestrator;

    [Tooltip("Scene server orchestrator (null on pure client).")]
    public ServerOrchestrator serverOrchestrator;

    [Tooltip("When true, auto-start SP loopback on play.")]
    public bool autoStartSinglePlayerLoopback = true;

    public bool TryCompleteFromService()
    {
        var svc = SystemDrawerService.Instance;
        if (svc == null)
            return false;
        bool any = false;
        if (clientOrchestrator == null)
        {
            clientOrchestrator = svc.Get<ClientOrchestrator>(SystemDrawerServiceKeys.NetworkClientOrchestrator);
            any |= clientOrchestrator != null;
        }
        if (serverOrchestrator == null)
        {
            serverOrchestrator = svc.Get<ServerOrchestrator>(SystemDrawerServiceKeys.NetworkServerOrchestrator);
            any |= serverOrchestrator != null;
        }
        return any;
    }

    void Awake()
    {
        if (clientOrchestrator == null)
            clientOrchestrator = GetComponentInChildren<ClientOrchestrator>();
        if (serverOrchestrator == null)
            serverOrchestrator = GetComponentInChildren<ServerOrchestrator>();
    }

    void Start()
    {
        RegisterAll();
        if (autoStartSinglePlayerLoopback && serverOrchestrator != null &&
            !NetworkLaunchArgs.DedicatedServer)
        {
            serverOrchestrator.StartSinglePlayerLoopback();
        }
    }

    void OnEnable() => RegisterAll();

    void OnDisable() => UnregisterAll();

    public void RegisterAll()
    {
        var svc = SystemDrawerService.FindInScene();
        if (svc == null)
            return;
        svc.Register(ServiceKey, this);
        if (clientOrchestrator != null)
            svc.Register(SystemDrawerServiceKeys.NetworkClientOrchestrator, clientOrchestrator);
        if (serverOrchestrator != null)
            svc.Register(SystemDrawerServiceKeys.NetworkServerOrchestrator, serverOrchestrator);
    }

    public void UnregisterAll()
    {
        var svc = SystemDrawerService.FindInScene();
        if (svc == null)
            return;
        svc.Unregister(ServiceKey);
        svc.Unregister(SystemDrawerServiceKeys.NetworkClientOrchestrator);
        svc.Unregister(SystemDrawerServiceKeys.NetworkServerOrchestrator);
    }
}
