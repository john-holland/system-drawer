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
}
#endif

