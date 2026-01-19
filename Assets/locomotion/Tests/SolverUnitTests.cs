#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class SolverUnitTests
{
    [Test]
    public void AudioPathingSolver_StdDev_ZeroForConstantSamples()
    {
        // Mirror AudioPathingSolver.StdDev math for a constant array.
        float[] s = { 0.5f, 0.5f, 0.5f, 0.5f };
        float mean = 0f;
        for (int i = 0; i < s.Length; i++) mean += s[i];
        mean /= s.Length;

        float variance = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float d = s[i] - mean;
            variance += d * d;
        }
        variance /= s.Length;
        float std = Mathf.Sqrt(variance);

        Assert.AreEqual(0f, std, 1e-6f);
    }

    [Test]
    public void HierarchicalPathingGrid2D_WorldToCell_IsInBounds()
    {
        var grid = new HierarchicalPathingGrid2D(new Bounds(Vector3.zero, new Vector3(10f, 1f, 10f)), 1f);
        Assert.IsTrue(grid.TryWorldToCell(Vector3.zero, out int x, out int z));
        Assert.IsTrue(grid.IsInBounds(x, z));
    }

    [Test]
    public void HierarchicalPathingAStar2D_FindPath_ReturnsNonEmptyPath()
    {
        // 5x5 grid centered at origin (cellSize=1 => width=5 height=5).
        var grid = new HierarchicalPathingGrid2D(new Bounds(Vector3.zero, new Vector3(5f, 1f, 5f)), 1f);

        Vector3 start = grid.CellCenterWorld(0, 0, 0f);
        Vector3 goal = grid.CellCenterWorld(4, 4, 0f);

        var path = HierarchicalPathingAStar2D.FindPath(
            grid,
            start,
            goal,
            0f,
            new HierarchicalPathingAStar2D.Settings { allowDiagonals = true, maxExpandedNodes = 0, returnBestEffortPathWhenNoPath = false });

        Assert.IsNotNull(path);
        Assert.Greater(path.Count, 0);
        Assert.AreEqual(start.x, path[0].x, 1e-4f);
        Assert.AreEqual(start.z, path[0].z, 1e-4f);
        Assert.AreEqual(goal.x, path[^1].x, 1e-4f);
        Assert.AreEqual(goal.z, path[^1].z, 1e-4f);
    }
}
#endif

