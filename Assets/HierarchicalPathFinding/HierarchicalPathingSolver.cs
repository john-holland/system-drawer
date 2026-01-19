using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MVP hierarchical pathing coordinator.
/// Right now this primarily:
/// - tracks NoPathing + OffLimitsSpace markers
/// - provides a central dirty/rebuild loop that other systems can subscribe to
///
/// Future: oct/quad tree prebakes, capsule math, AABB balancing, traversable-space graph, etc.
/// </summary>
public class HierarchicalPathingSolver : MonoBehaviour, IHierarchicalPathingTree
{
    [Header("Discovery")]
    public bool autoFindMarkers = true;

    [Header("Rebuild")]
    [Tooltip("Debounce interval (seconds) for rebuilds after changes.")]
    public float rebuildDebounceSeconds = 0.1f;

    public event Action Rebuilt;

    private readonly List<OffLimitsSpace> offLimitsSpaces = new List<OffLimitsSpace>(64);
    private readonly List<NoPathing> noPathingMarkers = new List<NoPathing>(64);

    private bool dirty;
    private float lastRebuildRequestTime = -999f;

    public bool IsDirty => dirty;

    private void OnEnable()
    {
        NoPathing.Changed += HandleNoPathingChanged;
        OffLimitsSpace.Changed += HandleOffLimitsChanged;

        if (autoFindMarkers)
        {
            RefreshMarkers();
        }

        MarkDirty();
    }

    private void OnDisable()
    {
        NoPathing.Changed -= HandleNoPathingChanged;
        OffLimitsSpace.Changed -= HandleOffLimitsChanged;
    }

    private void Update()
    {
        if (!dirty)
            return;

        if (Time.time - lastRebuildRequestTime < rebuildDebounceSeconds)
            return;

        RebuildNow();
    }

    public void MarkDirty()
    {
        dirty = true;
        lastRebuildRequestTime = Time.time;
    }

    public IReadOnlyList<OffLimitsSpace> GetOffLimitsSpaces() => offLimitsSpaces;
    public IReadOnlyList<NoPathing> GetNoPathingMarkers() => noPathingMarkers;

    private void HandleNoPathingChanged(NoPathing np)
    {
        MarkDirty();
    }

    private void HandleOffLimitsChanged(OffLimitsSpace ol)
    {
        MarkDirty();
    }

    private void RefreshMarkers()
    {
        offLimitsSpaces.Clear();
        offLimitsSpaces.AddRange(FindObjectsOfType<OffLimitsSpace>());

        noPathingMarkers.Clear();
        noPathingMarkers.AddRange(FindObjectsOfType<NoPathing>());
    }

    private void RebuildNow()
    {
        // MVP: refresh marker lists. Later: rebuild oct/quad tree + traversability graph caches.
        if (autoFindMarkers)
        {
            RefreshMarkers();
        }

        dirty = false;
        Rebuilt?.Invoke();
    }
}

