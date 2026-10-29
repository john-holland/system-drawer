using NUnit.Framework;
using SpatialVolumes;
using UnityEngine;

public class PathingOccupancyCacheTests
{
    GameObject _solverGo;
    GameObject _volumeGo;

    [TearDown]
    public void TearDown()
    {
        if (_volumeGo != null)
            Object.DestroyImmediate(_volumeGo);
        if (_solverGo != null)
            Object.DestroyImmediate(_solverGo);
    }

    HierarchicalPathingSolver CreateSolver()
    {
        _solverGo = new GameObject("PathingOccupancyCacheTests");
        _solverGo.SetActive(false);
        var solver = _solverGo.AddComponent<HierarchicalPathingSolver>();
        solver.autoFindMarkers = false;
        solver.fitToTerrain = false;
        solver.worldBounds = new Bounds(Vector3.zero, new Vector3(4f, 2f, 4f));
        solver.cellSize = 1f;
        solver.rebuildDebounceSeconds = 0f;
        solver.pathingBackend = HierarchicalPathingBackend.Grid2DVolumeXZ;
        _solverGo.SetActive(true);
        return solver;
    }

    [Test]
    public void MarkerDirty_ReusesCapsuleCache()
    {
        HierarchicalPathingSolver solver = CreateSolver();
        solver.RebuildGrid();
        Assert.Greater(solver.LastRebuildCapsuleQueries, 0);
        Assert.IsFalse(solver.LastRebuildReusedPhysicsCache);

        solver.MarkDirty("marker");
        solver.FindPath(Vector3.zero, new Vector3(1f, 0f, 1f));

        Assert.AreEqual(0, solver.LastRebuildCapsuleQueries);
        Assert.IsTrue(solver.LastRebuildReusedPhysicsCache);
        Assert.IsFalse(solver.IsDirty);
    }

    [Test]
    public void UnlistedSpatialVolume_DoesNotFlipOccupancyCache()
    {
        HierarchicalPathingSolver solver = CreateSolver();
        solver.RebuildGrid();
        Assert.IsFalse(solver.IsDirty);
        int ignored = solver.IgnoredVolumeFlips;

        _volumeGo = new GameObject("UnlistedVolume");
        var provider = _volumeGo.AddComponent<SpatialVolumeProvider>();
        provider.SyncSDFTreeShape = true;
        provider.NotifyChanged();

        Assert.Greater(solver.IgnoredVolumeFlips, ignored);
        Assert.IsFalse(solver.IsDirty);

        solver.volumeProviders.Add(provider);
        provider.NotifyChanged();
        Assert.IsTrue(solver.IsDirty);
    }
}
