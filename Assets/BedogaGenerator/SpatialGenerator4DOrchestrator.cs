using UnityEngine;
using Locomotion.Narrative;

/// <summary>
/// Phase 9: Single management component for 4D placement and visualization.
/// Toggles enable/disable the 4D layer, temporal strategy, buffer/padding, SDF, pathfinding coverage, causal overlay, and emergence viz without code changes.
/// </summary>
public class SpatialGenerator4DOrchestrator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("4D spatial generator (placement and grid).")]
    public SpatialGenerator4D spatialGenerator4D;
    [Tooltip("Optional pathfinding coverage component.")]
    public NarrativePathfindingCoverage pathfindingCoverage;
    [Tooltip("Optional narrative calendar (for causal overlay).")]
    public NarrativeCalendarAsset narrativeCalendar;

    [Header("4D Placement & Strategy")]
    [Tooltip("Enable 4D placement (SpatialGenerator4D component active).")]
    public bool use4DPlacement = true;
    [Tooltip("Use temporal strategy for placement order.")]
    public bool useTemporalStrategy = true;
    [Tooltip("Apply schedule buffer and padding.")]
    public bool useBufferPadding = true;

    [Header("Visualization")]
    [Tooltip("Build 4D grid and show SDF slice gizmo.")]
    public bool showSDF = false;
    [Tooltip("Show pathfinding coverage (NarrativePathfindingCoverage enabled).")]
    public bool showPathfindingCoverage = false;
    [Tooltip("Show causal overlay on calendar (event links).")]
    public bool showCausal = false;
    [Tooltip("Show layered emergence visualization.")]
    public bool showEmergence = false;

    private void OnValidate()
    {
        Apply();
    }

    private void Start()
    {
        Apply();
    }

    /// <summary>Push orchestrator toggles to referenced components.</summary>
    public void Apply()
    {
        if (spatialGenerator4D != null)
        {
            spatialGenerator4D.enabled = use4DPlacement;
            spatialGenerator4D.useTemporalStrategy = useTemporalStrategy;
            spatialGenerator4D.useBufferPadding = useBufferPadding;
            spatialGenerator4D.buildGrid = showSDF;
            spatialGenerator4D.showGizmoSlice = showSDF;
            spatialGenerator4D.showEmergenceViz = showEmergence;
        }

        if (pathfindingCoverage != null)
            pathfindingCoverage.enabled = showPathfindingCoverage;

        if (narrativeCalendar != null)
            narrativeCalendar.showCausalOverlay = showCausal;
    }
}
