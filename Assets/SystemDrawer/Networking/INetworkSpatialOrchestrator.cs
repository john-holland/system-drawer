using System.Collections.Generic;

/// <summary>Facade over SpatialGenerator4DOrchestrator + causality history for client/server.</summary>
public interface INetworkSpatialOrchestrator
{
    NetworkServerMode Mode { get; }
    void Apply();
    void AppendCausalityHistorySnapshot(
        string leafBack,
        string leafPause,
        string leafForward,
        long flags,
        float narrativeT,
        UnityEngine.Vector3 position,
        string eventType,
        IReadOnlyList<CausalityNamedFlagEntryDto> namedFlags = null);
    void ClearCausalityHistory();
    void Reset4DRuntimeState(bool archiveHistory = false);
    bool RequestLoadScene(string sceneId);
}
