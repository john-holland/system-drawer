using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pluggable path query for a <see cref="PhysicalPathingMedium"/>; used by <see cref="PhysicalPathingSolverRegistry"/>.
/// </summary>
public interface IPhysicalPathingSolver
{
    PhysicalPathingMedium Medium { get; }

    /// <summary>
    /// Attempt to find a world-space path. Implementations may temporarily adjust <see cref="HierarchicalPathingSolver.pathingMode"/>; must restore on return.
    /// </summary>
    bool TryFindPath(
        HierarchicalPathingSolver context,
        Vector3 startWorld,
        Vector3 goalWorld,
        bool returnBestEffortPathWhenNoPath,
        out List<Vector3> path);
}

/// <summary>
/// Air: delegates to existing Fly 2D grid path (Y interpolated) as a stub.
/// </summary>
public sealed class AirPathingSolverStub : IPhysicalPathingSolver
{
    public PhysicalPathingMedium Medium => PhysicalPathingMedium.Air;

    public bool TryFindPath(
        HierarchicalPathingSolver context,
        Vector3 startWorld,
        Vector3 goalWorld,
        bool returnBestEffortPathWhenNoPath,
        out List<Vector3> path)
    {
        path = null;
        if (context == null)
            return false;

        PathingMode saved = context.pathingMode;
        context.pathingMode = PathingMode.Fly;
        try
        {
            path = context.FindPath(startWorld, goalWorld, returnBestEffortPathWhenNoPath);
        }
        finally
        {
            context.pathingMode = saved;
        }

        return path != null && path.Count > 0;
    }
}

/// <summary>
/// Water: stub straight segment from start to goal (replace with volume/swim graph later).
/// </summary>
public sealed class WaterPathingSolverStub : IPhysicalPathingSolver
{
    public PhysicalPathingMedium Medium => PhysicalPathingMedium.Water;

    public bool TryFindPath(
        HierarchicalPathingSolver context,
        Vector3 startWorld,
        Vector3 goalWorld,
        bool returnBestEffortPathWhenNoPath,
        out List<Vector3> path)
    {
        path = new List<Vector3> { startWorld, goalWorld };
        return true;
    }
}

/// <summary>
/// Space: stub straight segment (replace with orbit/free-flight volume later).
/// </summary>
public sealed class SpacePathingSolverStub : IPhysicalPathingSolver
{
    public PhysicalPathingMedium Medium => PhysicalPathingMedium.Space;

    public bool TryFindPath(
        HierarchicalPathingSolver context,
        Vector3 startWorld,
        Vector3 goalWorld,
        bool returnBestEffortPathWhenNoPath,
        out List<Vector3> path)
    {
        path = new List<Vector3> { startWorld, goalWorld };
        return true;
    }
}

/// <summary>
/// Ground: thin wrapper around default solver path (Walk/Fly already handled by context.pathingMode).
/// </summary>
public sealed class GroundPathingSolverStub : IPhysicalPathingSolver
{
    public PhysicalPathingMedium Medium => PhysicalPathingMedium.Ground;

    public bool TryFindPath(
        HierarchicalPathingSolver context,
        Vector3 startWorld,
        Vector3 goalWorld,
        bool returnBestEffortPathWhenNoPath,
        out List<Vector3> path)
    {
        path = null;
        if (context == null)
            return false;

        path = context.FindPath(startWorld, goalWorld, returnBestEffortPathWhenNoPath);
        return path != null && path.Count > 0;
    }
}
