using System.Collections.Generic;
using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

namespace Roads.Tests
{
    public class RoadLayoutPlacementTests
    {
        [Test]
        public void HandAuthoredMode_UsesControlPoints()
        {
            var go = new GameObject("road-test");
            var node = go.AddComponent<RoadLayoutPlacementNode>();
            var spline = go.AddComponent<RoadSpline3D>();
            node.placementMode = RoadLayoutPlacementMode.HandAuthored;
            node.handPlacedControlPoints = new List<Vector3> { Vector3.zero, Vector3.forward * 10f };
            node.roadSpline = spline;
            var solver = go.AddComponent<RoadLayoutPlacementSolver>();
            solver.roadSpline = spline;

            PathReplacementGate.Unlock();
            var inst = new LayoutPlacementInstruction
            {
                startWorld = Vector3.zero,
                goalWorld = Vector3.forward * 10f,
                requiresPathSolve = true
            };
            Assert.IsTrue(solver.TryPlaceRoad(inst, out var id));
            Assert.IsNotNull(id);
            Assert.AreEqual(2, spline.controlPoints.Count);
            Object.DestroyImmediate(go);
        }
    }
}
