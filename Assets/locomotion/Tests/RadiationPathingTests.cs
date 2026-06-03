using System.Collections.Generic;
using Locomotion.Spaceship;
using NUnit.Framework;
using UnityEngine;

public class RadiationPathingTests
{
    [Test]
    public void RadiationSolver_ReturnsPathWithAtLeastTwoPoints()
    {
        var go = new GameObject("hpf");
        var hpf = go.AddComponent<HierarchicalPathingSolver>();
        var solver = new RadiationAwarePathingSolver { ignoreRadiation = true };
        var path = solver.FindPath(hpf, Vector3.zero, Vector3.forward * 10f, 5f);
        Assert.IsNotNull(path);
        Assert.GreaterOrEqual(path.Count, 2);
        Object.DestroyImmediate(go);
    }
}
