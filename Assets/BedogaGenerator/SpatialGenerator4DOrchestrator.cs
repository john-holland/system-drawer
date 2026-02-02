using System.Collections.Generic;
using UnityEngine;
using Locomotion.Narrative;

/// <summary>
/// Orchestrator: single management component for 4D placement and visualization.
/// Toggles enable/disable the 4D layer, temporal strategy, buffer/padding, SDF, pathfinding coverage, causal overlay, and emergence viz without code changes.
/// Holds a unified list of spatial generators (3D and 4D).
/// </summary>
public class SpatialGenerator4DOrchestrator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Spatial generators (3D and/or 4D). Use Find generators in hierarchy or Add 3D/4D generator in the editor.")]
    public List<SpatialGeneratorBase> spatialGenerators = new List<SpatialGeneratorBase>();
    [Tooltip("Optional pathfinding coverage component.")]
    public NarrativePathfindingCoverage pathfindingCoverage;
    [Tooltip("Optional narrative calendar (for causal overlay).")]
    public NarrativeCalendarAsset narrativeCalendar;
    [Tooltip("Optional weather system for narrative time alignment (e.g. WeatherSystem GameObject).")]
    public GameObject weatherSystemObject;
    [Tooltip("Optional bounds provider (e.g. WeatherPhysicsManifold). When set, generator bounds can be aligned with this.")]
    public MonoBehaviour boundsProvider;

    [SerializeField, HideInInspector]
    [System.Obsolete("Use spatialGenerators list; migrated automatically.")]
    private SpatialGenerator4D spatialGenerator4D;

    [Header("4D Placement & Strategy")]
    [Tooltip("Enable 4D placement (SpatialGenerator4D components active).")]
    public bool use4DPlacement = true;
    [Tooltip("Use temporal strategy for placement order.")]
    public bool useTemporalStrategy = true;
    [Tooltip("Apply schedule buffer and padding.")]
    public bool useBufferPadding = true;

    [Header("3D Layout")]
    [Tooltip("Enable 3D layout (SpatialGenerator components active).")]
    public bool use3DPlacement = true;
    [Tooltip("Show tree visualization for 3D generators.")]
    public bool showTreeVisualization = false;

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
        MigrateLegacyIfNeeded();
        Apply();
    }

    private void Start()
    {
        ResolveReferences();
        Apply();
    }

    /// <summary>If spatialGenerators is null or empty, find all SpatialGeneratorBase in hierarchy and assign to the list. Only runs when list is empty.</summary>
    public void ResolveReferences()
    {
        if (spatialGenerators == null)
            spatialGenerators = new List<SpatialGeneratorBase>();
        if (spatialGenerators.Count > 0)
            return;
        var found = GetComponentsInChildren<SpatialGeneratorBase>(true);
        if (found != null && found.Length > 0)
        {
            spatialGenerators.Clear();
            foreach (var g in found)
                if (g != null && !spatialGenerators.Contains(g))
                    spatialGenerators.Add(g);
        }
        var inParent = GetComponentsInParent<SpatialGeneratorBase>(true);
        if (inParent != null)
        {
            foreach (var g in inParent)
                if (g != null && !spatialGenerators.Contains(g))
                    spatialGenerators.Insert(0, g);
        }
    }

    /// <summary>If legacy single 4D reference exists and list is empty, copy it into spatialGenerators and clear the legacy field.</summary>
    public void MigrateLegacyIfNeeded()
    {
#pragma warning disable CS0618
        if (spatialGenerator4D == null)
            return;
#pragma warning restore CS0618
        if (spatialGenerators == null)
            spatialGenerators = new List<SpatialGeneratorBase>();
        if (spatialGenerators.Count > 0)
            return;
#pragma warning disable CS0618
        spatialGenerators.Add(spatialGenerator4D);
        spatialGenerator4D = null;
#pragma warning restore CS0618
    }

    /// <summary>Push orchestrator toggles to referenced components.</summary>
    public void Apply()
    {
        MigrateLegacyIfNeeded();
        if (spatialGenerators == null)
            return;
        foreach (var gen in spatialGenerators)
        {
            if (gen == null) continue;
            if (gen is SpatialGenerator4D sg4d)
            {
                sg4d.enabled = use4DPlacement;
                sg4d.useTemporalStrategy = useTemporalStrategy;
                sg4d.useBufferPadding = useBufferPadding;
                sg4d.buildGrid = showSDF;
                sg4d.showGizmoSlice = showSDF;
                sg4d.showEmergenceViz = showEmergence;
            }
            else if (gen is SpatialGenerator sg3d)
            {
                sg3d.enabled = use3DPlacement;
                sg3d.showTreeVisualization = showTreeVisualization;
            }
        }
        if (pathfindingCoverage != null)
            pathfindingCoverage.enabled = showPathfindingCoverage;
        if (narrativeCalendar != null)
            narrativeCalendar.showCausalOverlay = showCausal;
    }
}
