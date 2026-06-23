using Locomotion.Camera;
using NUnit.Framework;
using UnityEngine;

public class FrustumAlignedOctreeBasisTests
{
    [Test]
    public void CameraBasisRotation_MapsForwardToZ()
    {
        var rot = FrustumAlignedOctreeBasis.CameraBasisRotation(Vector3.forward, Vector3.up);
        Vector3 local = FrustumAlignedOctreeBasis.ToCameraLocal(Vector3.forward, Vector3.zero, rot);
        Assert.That(local.z, Is.GreaterThan(0.9f));
    }

    [Test]
    public void TopologyVector_HasFixedDim()
    {
        var tree = HierarchicalPathingOctTree.Build(new Bounds(Vector3.zero, Vector3.one * 10f), 2, 1f, _ => false);
        var camGo = new GameObject("cam");
        var cam = camGo.AddComponent<Camera>();
        cam.transform.position = new Vector3(0, 0, -5);
        cam.transform.LookAt(Vector3.zero);
        float[] vec = FrustumAlignedOctreeBasis.BuildTopologyVector(cam, tree.Leaves);
        Assert.AreEqual(FrustumAlignedOctreeBasis.TopologyDim, vec.Length);
        Object.DestroyImmediate(camGo);
    }
}
