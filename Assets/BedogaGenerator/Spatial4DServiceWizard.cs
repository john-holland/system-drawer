using UnityEngine;

/// <summary>
/// Service wizard component for Spatial 4D Orchestrator. Add to any GameObject; use slots and buttons to open orchestrator tools or create examples.
/// Registers with SystemDrawerService when present. Use TryCompleteFromService to fill slot from the conglomerator.
/// </summary>
public class Spatial4DServiceWizard : MonoBehaviour
{
    public const string ServiceKey = "Spatial4DOrchestrator";

    [Tooltip("4D Orchestrator this wizard configures.")]
    public SpatialGenerator4DOrchestrator orchestrator;

    /// <summary>Assign slot from SystemDrawerService if empty. Returns true if assigned.</summary>
    public bool TryCompleteFromService()
    {
        var service = SystemDrawerService.Instance;
        if (service == null) return false;
        var orch = service.Get<SpatialGenerator4DOrchestrator>(ServiceKey);
        if (orch != null)
        {
            orchestrator = orch;
            return true;
        }
        return false;
    }

    private void OnEnable()
    {
        if (orchestrator != null && SystemDrawerService.Instance != null)
            SystemDrawerService.Instance.Register(ServiceKey, orchestrator);
    }

    private void OnDisable()
    {
        if (SystemDrawerService.Instance != null)
            SystemDrawerService.Instance.Unregister(ServiceKey);
    }
}
