using System.Collections.Generic;
using UnityEngine;
using Locomotion.Narrative;

/// <summary>
/// Phase 6: Pathfinding-driven coverage volume. Builds SDF max over reachable (x,y,z) by sampling paths
/// from an agent to goals; supports display modes (likelihood, fitness, possible placement) for visualization.
/// </summary>
public class NarrativePathfindingCoverage : MonoBehaviour
{
    public enum CoverageDisplayMode
    {
        PossiblePlacement,
        Likelihood,
        Fitness
    }

    [Header("Pathfinding")]
    [Tooltip("Optional pathfinding solver to sample reachable space. If null, coverage stays empty or uses fallback.")]
    public HierarchicalPathingSolver pathingSolver;
    [Tooltip("Agent transform to start paths from. If null, uses transform.position of this GameObject.")]
    public Transform agent;
    [Tooltip("Number of goal samples when building coverage (paths from agent to goals).")]
    public int coverageSampleCount = 32;
    [Tooltip("Max path length (waypoints) to store per path; limits memory.")]
    public int maxWaypointsPerPath = 256;

    [Header("Bounds")]
    [Tooltip("World bounds to sample goals within. Used when building coverage.")]
    public Bounds worldBounds = new Bounds(Vector3.zero, new Vector3(50f, 10f, 50f));

    [Header("Coverage Grid")]
    [Tooltip("Grid resolution for reachable occupancy (XZ).")]
    public int gridResX = 32;
    public int gridResZ = 32;
    [Tooltip("Vertical slices for 3D coverage (Y). 1 = 2D XZ only.")]
    public int gridResY = 1;

    [Header("Display")]
    [Tooltip("Display mode for visualization: possible placement (binary), likelihood, or fitness/cost.")]
    public CoverageDisplayMode displayMode = CoverageDisplayMode.PossiblePlacement;

    private float[] reachableGrid;
    private Bounds gridBounds;
    private int gx, gy, gz;
    private bool built;

    public bool IsBuilt => built;
    public Bounds GridBounds => gridBounds;

    /// <summary>Build coverage by sampling paths from agent to random goals in worldBounds. Requires pathingSolver and agent.</summary>
    public void BuildCoverage()
    {
        reachableGrid = null;
        built = false;
        if (pathingSolver == null)
            return;

        Vector3 start = agent != null ? agent.position : transform.position;
        gridBounds = worldBounds;
        gx = Mathf.Max(1, gridResX);
        gy = Mathf.Max(1, gridResY);
        gz = Mathf.Max(1, gridResZ);
        int count = gx * gy * gz;
        reachableGrid = new float[count];

        HashSet<(int ix, int iy, int iz)> filled = new HashSet<(int, int, int)>();
        int goalsUsed = 0;
        for (int s = 0; s < coverageSampleCount; s++)
        {
            Vector3 goal = new Vector3(
                Random.Range(gridBounds.min.x, gridBounds.max.x),
                Random.Range(gridBounds.min.y, gridBounds.max.y),
                Random.Range(gridBounds.min.z, gridBounds.max.z));
            List<Vector3> path = pathingSolver.FindPath(start, goal, true);
            if (path == null || path.Count == 0)
                continue;
            goalsUsed++;
            int n = Mathf.Min(path.Count, maxWaypointsPerPath);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = path[i];
                WorldToCell(p, out int ix, out int iy, out int iz);
                if (ix >= 0 && ix < gx && iy >= 0 && iy < gy && iz >= 0 && iz < gz)
                    filled.Add((ix, iy, iz));
            }
        }

        for (int i = 0; i < count; i++)
            reachableGrid[i] = 0f;
        foreach (var cell in filled)
            reachableGrid[Index(cell.ix, cell.iy, cell.iz)] = 1f;
        built = true;
    }

    private void WorldToCell(Vector3 world, out int ix, out int iy, out int iz)
    {
        Vector3 min = gridBounds.min;
        Vector3 size = gridBounds.size;
        ix = (int)((world.x - min.x) / size.x * gx);
        iy = gy > 1 ? (int)((world.y - min.y) / size.y * gy) : 0;
        iz = (int)((world.z - min.z) / size.z * gz);
    }

    private int Index(int ix, int iy, int iz)
    {
        return ix + gx * (iy + gy * iz);
    }

    /// <summary>Sample reachable occupancy at world position (0 = not reachable, 1 = reachable). Returns 0 if not built.</summary>
    public float SampleReachable(Vector3 world)
    {
        if (!built || reachableGrid == null)
            return 0f;
        WorldToCell(world, out int ix, out int iy, out int iz);
        if (ix < 0 || ix >= gx || iy < 0 || iy >= gy || iz < 0 || iz >= gz)
            return 0f;
        return reachableGrid[Index(ix, iy, iz)];
    }

    /// <summary>Get display value at position (depends on displayMode). For PossiblePlacement same as SampleReachable.</summary>
    public float GetDisplayValueAt(Vector3 world)
    {
        float occ = SampleReachable(world);
        switch (displayMode)
        {
            case CoverageDisplayMode.Likelihood:
                return occ;
            case CoverageDisplayMode.Fitness:
                return occ;
            case CoverageDisplayMode.PossiblePlacement:
            default:
                return occ;
        }
    }
}
