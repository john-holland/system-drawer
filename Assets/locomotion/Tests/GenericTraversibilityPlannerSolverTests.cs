#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GenericTraversibilityPlannerSolverTests
{
    sealed class StraightAirSolver : IPhysicalPathingSolver
    {
        public PhysicalPathingMedium Medium => PhysicalPathingMedium.Air;

        public bool TryFindPath(
            HierarchicalPathingSolver context,
            Vector3 startWorld,
            Vector3 goalWorld,
            bool returnBestEffortPathWhenNoPath,
            out List<Vector3> path)
        {
            path = new List<Vector3> { startWorld, goalWorld };
            return true;
        }
    }

    [Test]
    public void BuildPlan_WalkAvailable_ReturnsWalkSegment()
    {
        var go = new GameObject("solver_walk");
        var solver = go.AddComponent<HierarchicalPathingSolver>();
        solver.worldBounds = new Bounds(Vector3.zero, new Vector3(50f, 10f, 50f));
        solver.cellSize = 1f;
        solver.autoFindMarkers = false;

        Vector3 start = Vector3.zero;
        Vector3 goal = new Vector3(5f, 0f, 0f);
        var hints = new GenericTraversibilityPlannerSolver.PlannerHints();

        GenericMultiModalPathPlan plan = GenericTraversibilityPlannerSolver.BuildPlan(
            start,
            goal,
            solver,
            new List<GoodSection>(),
            new List<GoodSection>(),
            start,
            0f,
            hints);

        Assert.IsFalse(plan.IsEmpty);
        Assert.AreEqual(1, plan.segments.Count);
        Assert.AreEqual(TravelLegMode.Walk, plan.segments[0].mode);
        Assert.Greater(plan.segments[0].waypoints.Count, 0);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void BuildPlan_WalkFails_UsesAirRegistry_ReturnsFlySegment()
    {
        IPhysicalPathingSolver previousAir = null;
        if (PhysicalPathingSolverRegistry.TryGetSolver(PhysicalPathingMedium.Air, out var existing))
            previousAir = existing;
        PhysicalPathingSolverRegistry.Register(PhysicalPathingMedium.Air, new StraightAirSolver());

        try
        {
            var go = new GameObject("solver_air");
            var solver = go.AddComponent<HierarchicalPathingSolver>();
            solver.worldBounds = new Bounds(Vector3.zero, new Vector3(20f, 10f, 20f));
            solver.cellSize = 1f;
            solver.autoFindMarkers = false;

            Vector3 start = Vector3.zero;
            Vector3 goal = new Vector3(100f, 0f, 0f);
            var hints = new GenericTraversibilityPlannerSolver.PlannerHints();

            GenericMultiModalPathPlan plan = GenericTraversibilityPlannerSolver.BuildPlan(
                start,
                goal,
                solver,
                new List<GoodSection>(),
                new List<GoodSection>(),
                start,
                0f,
                hints);

            Assert.IsFalse(plan.IsEmpty);
            Assert.AreEqual(TravelLegMode.Fly, plan.segments[0].mode);
            Assert.AreEqual(2, plan.segments[0].waypoints.Count);

            Object.DestroyImmediate(go);
        }
        finally
        {
            if (previousAir != null)
                PhysicalPathingSolverRegistry.Register(PhysicalPathingMedium.Air, previousAir);
            else
                PhysicalPathingSolverRegistry.Register(PhysicalPathingMedium.Air, new AirPathingSolverStub());
        }
    }

    [Test]
    public void SolveFromSyntheticEdges_WalkDriveWalkFlyWalk_Sequence()
    {
        var nodes = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(10f, 0f, 0f),
            new Vector3(20f, 0f, 0f),
            new Vector3(30f, 0f, 0f),
            new Vector3(40f, 0f, 0f),
            new Vector3(50f, 0f, 0f)
        };
        var edges = new List<TimelineMultiModalPlanner.SyntheticEdge>
        {
            new TimelineMultiModalPlanner.SyntheticEdge { fromIndex = 0, toIndex = 1, mode = TravelLegMode.Walk, pathLengthMeters = 10f },
            new TimelineMultiModalPlanner.SyntheticEdge { fromIndex = 1, toIndex = 2, mode = TravelLegMode.Drive, pathLengthMeters = 10f },
            new TimelineMultiModalPlanner.SyntheticEdge { fromIndex = 2, toIndex = 3, mode = TravelLegMode.Walk, pathLengthMeters = 10f },
            new TimelineMultiModalPlanner.SyntheticEdge { fromIndex = 3, toIndex = 4, mode = TravelLegMode.Fly, pathLengthMeters = 10f },
            new TimelineMultiModalPlanner.SyntheticEdge { fromIndex = 4, toIndex = 5, mode = TravelLegMode.Walk, pathLengthMeters = 10f }
        };

        var timeline = PlannerTimelineOptions.DefaultLegacy();
        timeline.enableMultiLegTimelineSearch = true;
        timeline.modeChangePenaltySec = 0f;
        timeline.minDriveLegLength = 2f;
        timeline.minFlyLegLength = 2f;

        var hints = new GenericTraversibilityPlannerSolver.PlannerHints();
        GenericMultiModalPathPlan plan = TimelineMultiModalPlanner.SolveFromSyntheticEdges(nodes, edges, in timeline, in hints);

        Assert.IsFalse(plan.IsEmpty);
        Assert.AreEqual(5, plan.segments.Count);
        Assert.AreEqual(TravelLegMode.Walk, plan.segments[0].mode);
        Assert.AreEqual(TravelLegMode.Drive, plan.segments[1].mode);
        Assert.AreEqual(TravelLegMode.Walk, plan.segments[2].mode);
        Assert.AreEqual(TravelLegMode.Fly, plan.segments[3].mode);
        Assert.AreEqual(TravelLegMode.Walk, plan.segments[4].mode);
    }

    [Test]
    public void PlannerTimelineOptions_GetEffectiveHintEffectiveness_MediumPreset_Is055()
    {
        var o = PlannerTimelineOptions.DefaultLegacy();
        o.useDifficultyPresetForHints = true;
        o.hintDifficulty = PlannerHintDifficulty.Medium;
        Assert.AreEqual(0.55f, o.GetEffectiveHintEffectiveness(), 0.001f);
    }

    [Test]
    public void SolveFromSyntheticEdges_HardVsEasy_HintChangesDrivePreference()
    {
        var nodes = new List<Vector3>
        {
            Vector3.zero,
            new Vector3(10f, 0f, 0f),
            new Vector3(20f, 0f, 0f)
        };
        var edges = new List<TimelineMultiModalPlanner.SyntheticEdge>
        {
            new TimelineMultiModalPlanner.SyntheticEdge { fromIndex = 0, toIndex = 1, mode = TravelLegMode.Walk, pathLengthMeters = 10f },
            new TimelineMultiModalPlanner.SyntheticEdge { fromIndex = 1, toIndex = 2, mode = TravelLegMode.Walk, pathLengthMeters = 10f },
            new TimelineMultiModalPlanner.SyntheticEdge { fromIndex = 0, toIndex = 1, mode = TravelLegMode.Drive, pathLengthMeters = 2f },
            new TimelineMultiModalPlanner.SyntheticEdge { fromIndex = 1, toIndex = 2, mode = TravelLegMode.Walk, pathLengthMeters = 10f }
        };

        var vehGo = new GameObject("veh_hint");
        var hints = new GenericTraversibilityPlannerSolver.PlannerHints
        {
            requireAsset01 = 0f,
            requireType01 = 0f,
            preferredVehicle = vehGo.AddComponent<VehicleActor>()
        };

        var easy = PlannerTimelineOptions.DefaultLegacy();
        easy.modeChangePenaltySec = 2f;
        easy.walkSpeed = 5f;
        easy.driveSpeed = 8f;
        easy.flySpeed = 15f;
        easy.useDifficultyPresetForHints = true;
        easy.hintDifficulty = PlannerHintDifficulty.Easy;

        var hard = PlannerTimelineOptions.DefaultLegacy();
        hard.modeChangePenaltySec = 2f;
        hard.walkSpeed = 5f;
        hard.driveSpeed = 8f;
        hard.flySpeed = 15f;
        hard.useDifficultyPresetForHints = true;
        hard.hintDifficulty = PlannerHintDifficulty.Hard;

        GenericMultiModalPathPlan easyPlan = TimelineMultiModalPlanner.SolveFromSyntheticEdges(nodes, edges, in easy, in hints);
        GenericMultiModalPathPlan hardPlan = TimelineMultiModalPlanner.SolveFromSyntheticEdges(nodes, edges, in hard, in hints);

        Assert.IsFalse(easyPlan.IsEmpty);
        Assert.IsFalse(hardPlan.IsEmpty);
        Assert.AreEqual(TravelLegMode.Drive, easyPlan.segments[0].mode, "Easy applies full negative drive hint so drive-walk beats walk-walk on cost.");
        Assert.AreEqual(TravelLegMode.Walk, hardPlan.segments[0].mode, "Hard dampens drive hint so walk-walk is cheaper.");

        Object.DestroyImmediate(vehGo);
    }
}
#endif
