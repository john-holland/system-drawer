#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AmbulationAndPathingTests
{
    [Test]
    public void AmbulationClassifier_AllowsTorsoAndLegs_RejectsHand()
    {
        var card = new GoodSection
        {
            impulseStack = new List<ImpulseAction>
            {
                new ImpulseAction { muscleGroup = "left_hip", activation = 1f },
                new ImpulseAction { muscleGroup = "torso", activation = 0.2f },
            }
        };
        Assert.IsTrue(AmbulationCardClassifier.IsAmbulationExtentOnlyCard(card));

        card.impulseStack.Add(new ImpulseAction { muscleGroup = "right_hand", activation = 1f });
        Assert.IsFalse(AmbulationCardClassifier.IsAmbulationExtentOnlyCard(card));
    }

    [Test]
    public void AmbulationRangeStack_Connected_Narrows()
    {
        var stack = new AmbulationRangeStack();
        stack.PushConnectedConstraint(0f, 0.5f);
        var r = stack.CurrentRange;
        Assert.IsFalse(r.IsEmpty);
        Assert.GreaterOrEqual(r.maxValue, r.minValue);
    }

    [Test]
    public void AmbulationRangeStack_Inverse_PicksSide()
    {
        var stack = new AmbulationRangeStack();
        stack.PushInverseConstraint(0f, 0.25f);
        Assert.IsFalse(stack.CurrentRange.IsEmpty);
    }

    [Test]
    public void HierarchicalPathingAStar3D_FindPath_FreeVolume()
    {
        var grid = new HierarchicalPathingVolumeGrid3D(new Bounds(Vector3.zero, new Vector3(5f, 5f, 5f)), 1f);
        Vector3 start = grid.CellCenterWorld(0, 0, 0);
        Vector3 goal = grid.CellCenterWorld(4, 4, 4);
        var path = HierarchicalPathingAStar3D.FindPath(
            grid,
            start,
            goal,
            new HierarchicalPathingAStar3D.Settings
            {
                allowDiagonalSteps = true,
                maxExpandedNodes = 0,
                returnBestEffortPathWhenNoPath = false,
                EdgeCost = null
            });

        Assert.IsNotNull(path);
        Assert.Greater(path.Count, 0);
    }

    [Test]
    public void OctTree_FaceAdjacent_DetectsNeighbors()
    {
        Bounds a = new Bounds(Vector3.zero, Vector3.one);
        Bounds b = new Bounds(new Vector3(1f, 0f, 0f), Vector3.one);
        Assert.IsTrue(HierarchicalPathingOctTree.FaceAdjacent(a, b, 1e-2f));
    }

    [Test]
    public void OctTree_FindPathThroughLeaves_AllWalkable()
    {
        var tree = HierarchicalPathingOctTree.Build(new Bounds(Vector3.zero, new Vector3(4f, 4f, 4f)), maxDepth: 2, minLeafExtent: 0.25f, _ => false);
        Vector3 start = new Vector3(-1f, -1f, -1f);
        Vector3 goal = new Vector3(1f, 1f, 1f);
        var path = HierarchicalPathingOctTree.FindPathThroughLeaves(tree.Leaves, start, goal, maxExpandedNodes: 100000);
        Assert.IsNotNull(path);
        Assert.Greater(path.Count, 0);
    }

    [Test]
    public void VehicleAmbulationSolver_RejectsOverSteeringVersusBudget()
    {
        var go = new GameObject("tmp_vehicle_solver");
        var solver = go.AddComponent<VehicleAmbulationSolver>();
        var ranges = new AmbulationRangeStack();

        bool ok = solver.TrySolveSteeringLeaf(
            steerDemandSigned01: 1f,
            rangePropagator: ranges,
            sampleWorldPosition: Vector3.zero,
            tractionBudget01: 0.15f,
            steerCommandSigned01: out _,
            throttleForward01: out _);

        Assert.IsFalse(ok);

        ok = solver.TrySolveSteeringLeaf(
            steerDemandSigned01: 0.1f,
            rangePropagator: ranges,
            sampleWorldPosition: Vector3.zero,
            tractionBudget01: 0.9f,
            steerCommandSigned01: out float steer,
            throttleForward01: out float throttle);

        Assert.IsTrue(ok);
        Assert.Greater(Mathf.Abs(steer), 0f);
        Assert.Greater(throttle, 0f);

        Object.DestroyImmediate(go);
    }
}
#endif
