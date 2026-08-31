#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using NUnit.Framework;
using Planetary;
using Planetary.AsteroidBelt;
using Planetary.Bridges;
using Planetary.Rendering;
using Planetary.Tectonics;
using Roads;
using SdfMax;
using UnityEngine;

public sealed class MasterRebakeRunnerTests
{
    [SetUp]
    public void SetUp()
    {
        MasterRebakeRunner.SuppressProgressBar = true;
        GameLobbyContinuuuumClient.TransportOverride = (m, p, b) => "{}";
        GameSessionLocalSave.RootOverride = Path.Combine(Path.GetTempPath(), "rebake-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        MasterRebakeRunner.SuppressProgressBar = false;
        GameLobbyContinuuuumClient.TransportOverride = null;
        if (!string.IsNullOrEmpty(GameSessionLocalSave.RootOverride) && Directory.Exists(GameSessionLocalSave.RootOverride))
            Directory.Delete(GameSessionLocalSave.RootOverride, true);
        GameSessionLocalSave.RootOverride = null;
    }

    [Test]
    public void EmptyScene_CompletesAndClearsProgressBar()
    {
        var report = MasterRebakeRunner.Run();
        Assert.IsTrue(report.Completed);
        Assert.IsFalse(report.Cancelled);
        Assert.IsNotNull(MasterRebakeRunner.LastReport);
    }

    [Test] public void Attempts_SpatialGenerator4DOrchestrator() => AssertAttempted<SpatialGenerator4DOrchestrator>();
    [Test] public void Attempts_PlanetBody() => AssertAttempted<PlanetBody>();
    [Test] public void Attempts_PlanetarySdfLodRenderer() => AssertAttempted<PlanetarySdfLodRenderer>();
    [Test] public void Attempts_PlanetInteriorPhysicsUpdater() => AssertAttempted<PlanetInteriorPhysicsUpdater>();
    [Test] public void Attempts_AsteroidBeltDiscRenderer() => AssertAttempted<AsteroidBeltDiscRenderer>();
    [Test] public void Attempts_RoadGeneratorOrchestratorBridge() => AssertAttempted<RoadGeneratorOrchestratorBridge>();
    [Test] public void Attempts_RoadMeshBaker() => AssertAttempted<RoadMeshBaker>();
    [Test] public void Attempts_RoadErosionSystem() => AssertAttempted<RoadErosionSystem>();
    [Test] public void Attempts_RoadSpline3D() => AssertAttempted<RoadSpline3D>();
    [Test] public void Attempts_SdfMaxMeshSurface() => AssertAttempted<SdfMaxMeshSurface>();
    [Test] public void Attempts_SdfMaxSkinnedMeshSurface() => AssertAttempted<SdfMaxSkinnedMeshSurface>();
    [Test] public void Attempts_HairPlumePhysicsDriver() => AssertAttempted<HairPlumePhysicsDriver>();
    [Test] public void Attempts_HouseEaveWaterCache() => AssertAttempted<HouseEaveWaterCache>();
    [Test] public void Attempts_HouseBasementFloodCache() => AssertAttempted<HouseBasementFloodCache>();
    [Test] public void Attempts_HeightMapInteriorShaderBuffer() => AssertAttempted<HeightMapInteriorShaderBuffer>();
    [Test] public void Attempts_CityPixelGridRuntime() => AssertAttempted<CityPixelGridRuntime>();
    [Test] public void Attempts_PlanetPhysicsManifoldBridge() => AssertAttempted<PlanetPhysicsManifoldBridge>();
    [Test] public void Attempts_ServerOrchestrator() => AssertAttempted<ServerOrchestrator>();
    [Test] public void Attempts_GameSessionHost() => AssertAttempted<GameSessionHost>();

    static void AssertAttempted<T>() where T : Component
    {
        var go = new GameObject(typeof(T).Name);
        go.AddComponent<T>();
        try
        {
            var report = MasterRebakeRunner.Run();
            Assert.IsTrue(report.Completed);
            Assert.GreaterOrEqual(report.Attempted(typeof(T).Name), 1);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
#endif
