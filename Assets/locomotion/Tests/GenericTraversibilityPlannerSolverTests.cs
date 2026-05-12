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
}
#endif
