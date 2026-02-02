using System.Collections.Generic;
using UnityEngine;
using Locomotion.Narrative;

/// <summary>
/// Inserts narrative calendar event volumes into SpatialGenerator4D. Optional calendar and 4D generator; resolves from orchestrator when null.
/// On enable or when calendar events change, inserts each event's spatiotemporalVolume into the 4D generator.
/// </summary>
public class Narrative4DPlacer : MonoBehaviour
{
    [Tooltip("Optional narrative calendar. When null, resolved from SpatialGenerator4DOrchestrator in hierarchy.")]
    public NarrativeCalendarAsset calendar;
    [Tooltip("Optional 4D spatial generator. When null, resolved from orchestrator list.")]
    public SpatialGenerator4D spatialGenerator4D;

    private HashSet<string> _insertedEventIds = new HashSet<string>();

    private void OnEnable()
    {
        ResolveReferences();
        SyncVolumes();
    }

    /// <summary>Resolve calendar and 4D generator from hierarchy when null.</summary>
    public void ResolveReferences()
    {
        if (calendar == null)
        {
            var orch = GetComponentInParent<SpatialGenerator4DOrchestrator>();
            if (orch != null)
                calendar = orch.narrativeCalendar;
            if (calendar == null)
                calendar = GetComponentInChildren<NarrativeCalendarAsset>(true);
        }
        if (spatialGenerator4D == null)
        {
            var orch = GetComponentInParent<SpatialGenerator4DOrchestrator>();
            if (orch != null && orch.spatialGenerators != null)
            {
                foreach (var g in orch.spatialGenerators)
                {
                    if (g is SpatialGenerator4D sg4) { spatialGenerator4D = sg4; break; }
                }
            }
            if (spatialGenerator4D == null)
                spatialGenerator4D = GetComponentInParent<SpatialGenerator4D>();
        }
    }

    /// <summary>Insert all calendar event volumes with spatiotemporalVolume into the 4D generator (if not already present).</summary>
    public void SyncVolumes()
    {
        if (calendar == null || spatialGenerator4D == null) return;
        if (calendar.events == null) return;
        foreach (var evt in calendar.events)
        {
            if (evt == null || !evt.spatiotemporalVolume.HasValue) continue;
            if (_insertedEventIds.Contains(evt.id)) continue;
            var vol = evt.spatiotemporalVolume.Value;
            spatialGenerator4D.Insert(vol, evt);
            _insertedEventIds.Add(evt.id);
        }
    }
}
