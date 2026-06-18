using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Adapter resolving SpatialGenerator4DOrchestrator via SystemDrawerService.</summary>
public sealed class NetworkSpatialOrchestratorAdapter : INetworkSpatialOrchestrator
{
    readonly NetworkServerMode _mode;
    SpatialGenerator4DOrchestrator _orchestrator;

    public NetworkServerMode Mode => _mode;

    public NetworkSpatialOrchestratorAdapter(NetworkServerMode mode)
    {
        _mode = mode;
        ResolveOrchestrator();
    }

    void ResolveOrchestrator()
    {
        if (_orchestrator != null)
            return;
        var svc = SystemDrawerService.FindInScene();
        if (svc != null)
            _orchestrator = svc.Get<SpatialGenerator4DOrchestrator>(NetworkServiceWizard.SpatialOrchestratorKey);
        if (_orchestrator == null)
            _orchestrator = Object.FindAnyObjectByType<SpatialGenerator4DOrchestrator>();
    }

    public void Apply()
    {
        ResolveOrchestrator();
        _orchestrator?.Apply();
    }

    public void AppendCausalityHistorySnapshot(
        string leafBack,
        string leafPause,
        string leafForward,
        long flags,
        float narrativeT,
        Vector3 position,
        string eventType,
        IReadOnlyList<CausalityNamedFlagEntryDto> namedFlags = null)
    {
        ResolveOrchestrator();
        _orchestrator?.AppendCausalityHistorySnapshot(
            leafBack, leafPause, leafForward, flags, narrativeT, position, eventType, namedFlags as IList<CausalityNamedFlagEntryDto>);
    }

    public void ClearCausalityHistory()
    {
        ResolveOrchestrator();
        _orchestrator?.ClearCausalityHistory();
    }

    public void Reset4DRuntimeState(bool archiveHistory = false)
    {
        ResolveOrchestrator();
        _orchestrator?.Reset4DRuntimeState(
            clearSpatial4DGenerators: true,
            clearTrippedTriggers: true,
            clearMirrorHierarchy: false,
            clearCausalityHistory: true,
            archiveCausalityHistoryBeforeClear: archiveHistory);
    }

    public bool RequestLoadScene(string sceneId)
    {
        if (string.IsNullOrEmpty(sceneId))
            return false;
        if (!Application.CanStreamedLevelBeLoaded(sceneId))
            return false;
        SceneManager.LoadScene(sceneId);
        return true;
    }
}
