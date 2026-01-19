using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Portable interface for hierarchical pathing backends (oct-tree, quad-tree, etc.).
/// MVP: this is focused on being a common hook-point for runtime invalidation and basic queries.
/// </summary>
public interface IHierarchicalPathingTree
{
    /// <summary>
    /// Mark cached spatial data as invalid and needing rebuild.
    /// </summary>
    void MarkDirty();

    /// <summary>
    /// True if the tree has pending changes that have not been rebuilt/applied.
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// Get current set of off-limits spaces.
    /// </summary>
    IReadOnlyList<OffLimitsSpace> GetOffLimitsSpaces();

    /// <summary>
    /// Get current set of no-pathing markers.
    /// </summary>
    IReadOnlyList<NoPathing> GetNoPathingMarkers();
}
