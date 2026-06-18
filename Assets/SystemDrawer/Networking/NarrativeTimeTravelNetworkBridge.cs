using Locomotion.Narrative;
using UnityEngine;

/// <summary>Bridges NarrativeTimeTravelCoordinator rewind requests to ClientOrchestrator.</summary>
[AddComponentMenu("System Drawer/Networking/Narrative Time Travel Network Bridge")]
public sealed class NarrativeTimeTravelNetworkBridge : MonoBehaviour
{
    public NarrativeTimeTravelCoordinator coordinator;
    public ClientOrchestrator client;

    void Awake()
    {
        if (coordinator == null)
            coordinator = FindAnyObjectByType<NarrativeTimeTravelCoordinator>();
        if (client == null)
            client = ClientOrchestrator.Instance;
        if (coordinator != null)
            coordinator.RewindRequested += OnRewindRequested;
    }

    void OnDestroy()
    {
        if (coordinator != null)
            coordinator.RewindRequested -= OnRewindRequested;
    }

    void OnRewindRequested(float targetTime, RewindAuthorityHint hint)
    {
        if (client == null)
            return;
        if (hint == RewindAuthorityHint.HostPeer && client.Mode != NetworkServerMode.AuthoritativePeerToPeer)
            return;
        client.RequestNarrativeRewind(targetTime, client.ClientId);
    }
}
