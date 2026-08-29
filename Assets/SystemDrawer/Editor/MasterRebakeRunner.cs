using System;
using System.Collections.Generic;
using Planetary;
using Planetary.AsteroidBelt;
using Planetary.Bridges;
using Planetary.Rendering;
using Planetary.Tectonics;
using Roads;
using SdfMax;
using UnityEditor;
using UnityEngine;

/// <summary>Scene-wide bake/sync: FindObjectsByType for every bake target, with a cancelable progress bar.</summary>
public static class MasterRebakeRunner
{
    public static bool SuppressProgressBar;
    public static MasterRebakeReport LastReport { get; private set; }

    public sealed class MasterRebakeReport
    {
        public bool Completed;
        public bool Cancelled;
        public readonly Dictionary<string, int> AttemptedByType = new Dictionary<string, int>();

        public int Attempted(string typeName) =>
            AttemptedByType.TryGetValue(typeName, out int n) ? n : 0;
    }

    public static MasterRebakeReport Run()
    {
        var report = new MasterRebakeReport();
        LastReport = report;
        var steps = BuildSteps();
        try
        {
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (!SuppressProgressBar)
                {
                    bool cancel = EditorUtility.DisplayCancelableProgressBar(
                        "Master Rebake",
                        $"{step.Name} ({i + 1}/{steps.Count})",
                        steps.Count <= 1 ? 1f : (float)i / steps.Count);
                    if (cancel)
                    {
                        report.Cancelled = true;
                        return report;
                    }
                }
                int n = 0;
                try
                {
                    n = step.Run();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[MasterRebake] step failed: " + step.Name + " " + ex.Message);
                }
                report.AttemptedByType[step.TypeName] = n;
            }
            report.Completed = !report.Cancelled;
        }
        finally
        {
            if (!SuppressProgressBar)
                EditorUtility.ClearProgressBar();
        }
        return report;
    }

    sealed class MasterRebakeStep
    {
        public string Name;
        public string TypeName;
        public Func<int> Run;
    }

    static List<MasterRebakeStep> BuildSteps()
    {
        return new List<MasterRebakeStep>
        {
            Step("SpatialGenerator4DOrchestrator", typeof(SpatialGenerator4DOrchestrator).Name, BakeSpatial),
            Step("PlanetBody", typeof(PlanetBody).Name, BakePlanetBody),
            Step("PlanetarySdfLodRenderer", typeof(PlanetarySdfLodRenderer).Name, BakeSdfLod),
            Step("PlanetInteriorPhysicsUpdater", typeof(PlanetInteriorPhysicsUpdater).Name, BakeInterior),
            Step("AsteroidBeltDiscRenderer", typeof(AsteroidBeltDiscRenderer).Name, BakeAsteroid),
            Step("RoadGeneratorOrchestratorBridge", typeof(RoadGeneratorOrchestratorBridge).Name, BakeRoadBridge),
            Step("RoadMeshBaker", typeof(RoadMeshBaker).Name, BakeRoadMesh),
            Step("RoadErosionSystem", typeof(RoadErosionSystem).Name, BakeErosion),
            Step("RoadSpline3D", typeof(RoadSpline3D).Name, BakeSpline),
            Step("SdfMaxMeshSurface", typeof(SdfMaxMeshSurface).Name, BakeSdfMesh),
            Step("SdfMaxSkinnedMeshSurface", typeof(SdfMaxSkinnedMeshSurface).Name, BakeSdfSkinned),
            Step("HairPlumePhysicsDriver", typeof(HairPlumePhysicsDriver).Name, BakeHair),
            Step("HouseEaveWaterCache", typeof(HouseEaveWaterCache).Name, BakeEave),
            Step("HeightMapInteriorShaderBuffer", typeof(HeightMapInteriorShaderBuffer).Name, BakeHeightMap),
            Step("CityPixelGridRuntime", typeof(CityPixelGridRuntime).Name, BakeCityGrid),
            Step("PlanetPhysicsManifoldBridge", typeof(PlanetPhysicsManifoldBridge).Name, BakePlanetManifold),
            Step("ServerOrchestrator", typeof(ServerOrchestrator).Name, BakeServer),
            Step("GameSessionHost", typeof(GameSessionHost).Name, BakeSessions),
        };
    }

    static MasterRebakeStep Step(string name, string typeName, Func<int> run) =>
        new MasterRebakeStep { Name = name, TypeName = typeName, Run = run };

    static T[] FindAll<T>() where T : UnityEngine.Object =>
        UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

    static int ForEach<T>(Action<T> action) where T : UnityEngine.Object
    {
        var found = FindAll<T>();
        int n = found != null ? found.Length : 0;
        if (found == null) return 0;
        for (int i = 0; i < found.Length; i++)
        {
            try
            {
                if (found[i] != null)
                    action(found[i]);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MasterRebake] " + typeof(T).Name + " " + ex.Message);
            }
        }
        return n;
    }

    static int BakeSpatial() => ForEach<SpatialGenerator4DOrchestrator>(o =>
    {
        o.ResolveReferences();
        o.Apply();
        if (o.lockSeedDependencyTree)
            o.ApplySeedDependencyTree();
    });

    static int BakePlanetBody() => ForEach<PlanetBody>(p => p.RebuildAll());

    static int BakeSdfLod() => ForEach<PlanetarySdfLodRenderer>(r => r.Rebake());

    static int BakeInterior() => ForEach<PlanetInteriorPhysicsUpdater>(u =>
    {
        if (u.plateSolver != null && u.plateSolver.plates != null && u.plateSolver.plates.Length > 0)
            u.RebakeFromPlates(u.plateSolver.plates);
    });

    static int BakeAsteroid() => ForEach<AsteroidBeltDiscRenderer>(d => d.RebuildMesh());

    static int BakeRoadBridge() => ForEach<RoadGeneratorOrchestratorBridge>(b => b.BakeAllRoads());

    static int BakeRoadMesh() => ForEach<RoadMeshBaker>(b => b.Bake());

    static int BakeErosion() => ForEach<RoadErosionSystem>(e => e.BakeErosion());

    static int BakeSpline() => ForEach<RoadSpline3D>(s =>
    {
        var sampler = s.GetComponent<SplinePathMeshSampler>();
        float spacing = sampler != null ? sampler.sampleSpacingMeters : 1f;
        s.RebuildBakedSamples(spacing);
    });

    static int BakeSdfMesh() => ForEach<SdfMaxMeshSurface>(s => s.RebuildSurfaceMesh());

    static int BakeSdfSkinned() => ForEach<SdfMaxSkinnedMeshSurface>(s => s.RebuildSurfaceMesh());

    static int BakeHair() => ForEach<HairPlumePhysicsDriver>(h => h.BakeFromConfig());

    static int BakeEave() => ForEach<HouseEaveWaterCache>(h => h.Prebake(0f));

    static int BakeHeightMap() => ForEach<HeightMapInteriorShaderBuffer>(h => h.Prebake());

    static int BakeCityGrid() => ForEach<CityPixelGridRuntime>(r =>
    {
        if (r.grid != null)
            CityPixelGridBaker.BakeAllFrames(r.grid);
    });

    static int BakePlanetManifold() => ForEach<PlanetPhysicsManifoldBridge>(b => b.StampFromCompositionBake());

    static int BakeServer() => ForEach<ServerOrchestrator>(s =>
    {
        s.EnsureReady();
        if (s.Settings != null && s.Settings.prefab != null)
        {
            s.ApplyLobbyPrefab(s.Settings.prefab);
            GameLobbyContinuuuumClient.PutPrefab(s.Settings.lobbySessionName, s.Settings.prefab);
        }
        GameLobbyContinuuuumClient.Heartbeat(s);
        s.GameSessions?.SaveAllToLocalClient();
    });

    static int BakeSessions() => ForEach<GameSessionHost>(h => h.SaveAllToLocalClient());
}
