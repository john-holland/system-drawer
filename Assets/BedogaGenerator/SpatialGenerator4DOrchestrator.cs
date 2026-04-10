using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Locomotion.Narrative;
using Newtonsoft.Json;

/// <summary>Output format for in-game spatial 4D editor flat file.</summary>
public enum Spatial4DOutputFormat
{
    Json,
    Yaml,
    Xml
}

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

    [Header("In-Game Spatial 4D Editor")]
    [Tooltip("When true, the in-game UI is shown in Play mode for placing markers, start/stop, and saving to file.")]
    public bool showInGameSpatial4DEditor = false;
    [Tooltip("When true, recording (and minute bar) auto-starts when player position enters a narrative volume (causality).")]
    public bool autoStartWithCausality = false;
    [Tooltip("When true, record each causality trigger (entry into narrative volume) in causalityTriggersTripped.")]
    public bool collectCausalityEvents = false;
    [Tooltip("List of causality triggers tripped (when collectCausalityEvents is true).")]
    public List<CausalityTriggerTrippedDto> causalityTriggersTripped = new List<CausalityTriggerTrippedDto>();
    [Tooltip("Append-only gateway triplet history (rows × Back/Pause/Forward + flags).")]
    public CausalityHistory2D causalityHistory = new CausalityHistory2D();
    [Tooltip("When clearing causality history via Reset4DRuntimeState, write JSON archive under persistentDataPath first.")]
    public bool archiveCausalityHistoryOnReset = false;
    [Tooltip("File path to write or append to. Relative paths resolve against persistentDataPath at runtime.")]
    public string inGameUIOutputFilePath = "Spatial4DExpressions.json";
    [Tooltip("If true, append new entries to existing file; else overwrite.")]
    public bool inGameUIAppendToFile = false;
    [Tooltip("Output format for the flat file.")]
    public Spatial4DOutputFormat inGameUIOutputFormat = Spatial4DOutputFormat.Json;

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

    /// <summary>Push orchestrator toggles to referenced components. When a SpatialGeneratorSkinController with skins is present, enabled state is driven from the active skin (editor or runtime index).</summary>
    public void Apply()
    {
        MigrateLegacyIfNeeded();
        var skinController = GetComponent<SpatialGeneratorSkinController>();
        if (skinController != null && skinController.skins != null && skinController.skins.Count > 0)
        {
            int idx = Application.isPlaying ? skinController.activeSkinIndex : skinController.editorActiveSkinIndex;
            if (idx >= 0 && idx < skinController.skins.Count)
                skinController.ApplySkin(idx);
        }
        if (spatialGenerators == null)
            return;
        foreach (var gen in spatialGenerators)
        {
            if (gen == null) continue;
            if (gen is SpatialGenerator4D sg4d)
            {
                if (skinController == null || skinController.skins == null || skinController.skins.Count == 0)
                    sg4d.enabled = use4DPlacement;
                sg4d.useTemporalStrategy = useTemporalStrategy;
                sg4d.useBufferPadding = useBufferPadding;
                sg4d.buildGrid = showSDF;
                sg4d.showGizmoSlice = showSDF;
                sg4d.showEmergenceViz = showEmergence;
            }
            else if (gen is SpatialGenerator sg3d)
            {
                if (skinController == null || skinController.skins == null || skinController.skins.Count == 0)
                    sg3d.enabled = use3DPlacement;
                sg3d.showTreeVisualization = showTreeVisualization;
            }
        }
        if (pathfindingCoverage != null)
            pathfindingCoverage.enabled = showPathfindingCoverage;
        if (narrativeCalendar != null)
            narrativeCalendar.showCausalOverlay = showCausal;
    }

    /// <summary>Append one observation row (e.g. volume entry); prior rows are never modified.</summary>
    public void AppendCausalityHistorySnapshot(string leafBack, string leafPause, string leafForward, long flags,
        float narrativeT, Vector3 position, string eventType, IList<CausalityNamedFlagEntryDto> namedFlags = null)
    {
        if (causalityHistory == null)
            causalityHistory = new CausalityHistory2D();
        causalityHistory.AppendRow(leafBack, leafPause, leafForward, flags, narrativeT, position, eventType, namedFlags);
    }

    public void ClearCausalityHistory()
    {
        causalityHistory?.rows?.Clear();
    }

    /// <summary>
    /// Clear 4D runtime caches: optional generator Clear, tripped triggers, mirror children, causality history.
    /// </summary>
    public void Reset4DRuntimeState(bool clearSpatial4DGenerators = true, bool clearTrippedTriggers = true,
        bool clearMirrorHierarchy = false, bool clearCausalityHistory = true, bool archiveCausalityHistoryBeforeClear = false)
    {
        bool archive = archiveCausalityHistoryBeforeClear || archiveCausalityHistoryOnReset;
        if (archive && causalityHistory != null && causalityHistory.rows != null && causalityHistory.rows.Count > 0)
            ArchiveCausalityHistoryToPersistentData();

        if (clearCausalityHistory)
            ClearCausalityHistory();

        if (clearTrippedTriggers && causalityTriggersTripped != null)
            causalityTriggersTripped.Clear();

        if (clearSpatial4DGenerators && spatialGenerators != null)
        {
            foreach (var gen in spatialGenerators)
            {
                if (gen is SpatialGenerator4D sg4)
                    sg4.Clear();
            }
        }

        if (clearMirrorHierarchy)
        {
            Transform mirrorRoot = transform.Find("4DTreeMirror");
            if (mirrorRoot != null)
            {
                for (int i = mirrorRoot.childCount - 1; i >= 0; i--)
                    Destroy(mirrorRoot.GetChild(i).gameObject);
            }
        }
    }

    void ArchiveCausalityHistoryToPersistentData()
    {
        if (causalityHistory == null || causalityHistory.rows == null || causalityHistory.rows.Count == 0)
            return;
        try
        {
            string dir = Application.persistentDataPath;
            string name = "Spatial4D_causality_history_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".json";
            string path = Path.Combine(dir, name);
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
            File.WriteAllText(path, JsonConvert.SerializeObject(causalityHistory, settings));
            Debug.Log("[Spatial4D] Archived causality history to " + path);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Spatial4D] Failed to archive causality history: " + e.Message);
        }
    }

    [ContextMenu("Reset 4D runtime state (generators + triggers + history)")]
    void ContextReset4DRuntimeState()
    {
        Reset4DRuntimeState(clearSpatial4DGenerators: true, clearTrippedTriggers: true, clearMirrorHierarchy: false,
            clearCausalityHistory: true, archiveCausalityHistoryBeforeClear: archiveCausalityHistoryOnReset);
    }

    [ContextMenu("Reset 4D — clear history only")]
    void ContextClearCausalityHistoryOnly()
    {
        ClearCausalityHistory();
    }
}
