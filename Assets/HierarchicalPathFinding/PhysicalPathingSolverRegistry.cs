using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves <see cref="PhysicalPathingMedium"/> to stub solvers. Register custom implementations at startup if needed.
/// </summary>
public static class PhysicalPathingSolverRegistry
{
    static readonly Dictionary<PhysicalPathingMedium, IPhysicalPathingSolver> MediumToSolver =
        new Dictionary<PhysicalPathingMedium, IPhysicalPathingSolver>
        {
            { PhysicalPathingMedium.Ground, new GroundPathingSolverStub() },
            { PhysicalPathingMedium.Air, new AirPathingSolverStub() },
            { PhysicalPathingMedium.Water, new WaterPathingSolverStub() },
            { PhysicalPathingMedium.Space, new SpacePathingSolverStub() }
        };

    /// <summary>
    /// Replace or add a solver for the medium (use for tests or custom backends).
    /// </summary>
    public static void Register(PhysicalPathingMedium medium, IPhysicalPathingSolver solver)
    {
        if (solver == null || medium == PhysicalPathingMedium.Unspecified)
            return;
        MediumToSolver[medium] = solver;
    }

    public static bool TryGetSolver(PhysicalPathingMedium medium, out IPhysicalPathingSolver solver)
    {
        solver = null;
        if (medium == PhysicalPathingMedium.Unspecified)
            return false;
        return MediumToSolver.TryGetValue(medium, out solver);
    }

    /// <summary>
    /// Uses registry when medium is set; otherwise uses context.FindPath directly (caller restores mode externally if needed).
    /// </summary>
    public static List<Vector3> FindPathForMedium(
        PhysicalPathingMedium medium,
        HierarchicalPathingSolver context,
        Vector3 startWorld,
        Vector3 goalWorld,
        bool returnBestEffortPathWhenNoPath)
    {
        if (context == null)
            return new List<Vector3>();

        if (medium == PhysicalPathingMedium.Unspecified || !TryGetSolver(medium, out var solver))
        {
            return context.FindPath(startWorld, goalWorld, returnBestEffortPathWhenNoPath);
        }

        if (solver.TryFindPath(context, startWorld, goalWorld, returnBestEffortPathWhenNoPath, out var path) && path != null)
            return path;

        return context.FindPath(startWorld, goalWorld, returnBestEffortPathWhenNoPath);
    }
}
