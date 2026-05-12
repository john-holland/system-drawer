using NUnit.Framework;
using UnityEngine;

public class ConvexTreeMeshColliderServiceTests
{
    GameObject _go;
    MeshCollider _mc;

    [SetUp]
    public void SetUp()
    {
        ConvexTreeMeshColliderService.InvalidateAll();

        _go = new GameObject("ConvexTreeMeshColliderServiceTests_Helper");
        Object.DestroyImmediate(_go.GetComponent<Collider>());

        var mf = _go.GetComponent<MeshFilter>();
        Assert.IsNotNull(mf);
        Assert.IsNotNull(mf.sharedMesh);

        _mc = _go.AddComponent<MeshCollider>();
        _mc.sharedMesh = mf.sharedMesh;
        _mc.convex = true;
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
            Object.DestroyImmediate(_go);
        ConvexTreeMeshColliderService.InvalidateAll();
    }

    [Test]
    public void EnsureBuilt_ConvexMeshCollider_HasLeaves_And_Version()
    {
        Assert.IsTrue(ConvexTreeMeshColliderService.EnsureBuilt(_mc));
        Assert.IsTrue(ConvexTreeMeshColliderService.TryGetCache(_mc, out var cache));
        Assert.IsNotNull(cache);
        Assert.Greater(cache.Leaves.Count, 0);
        Assert.Greater(cache.BuildVersion, 0);
        int v1 = cache.BuildVersion;

        ConvexTreeMeshColliderService.Invalidate(_mc);
        Assert.IsTrue(ConvexTreeMeshColliderService.EnsureBuilt(_mc));
        Assert.IsTrue(ConvexTreeMeshColliderService.TryGetCache(_mc, out var cache2));
        Assert.Greater(cache2.BuildVersion, v1);
    }

    [Test]
    public void EnsureBuilt_NonConvex_ReturnsFalse()
    {
        _mc.convex = false;
        ConvexTreeMeshColliderService.Invalidate(_mc);
        Assert.IsFalse(ConvexTreeMeshColliderService.EnsureBuilt(_mc));
        Assert.IsFalse(ConvexTreeMeshColliderService.TryGetCache(_mc, out _));
    }
}
