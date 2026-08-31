using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class UtilitySgTests
{
    [Test]
    public void CircuitBreakerPanel_ConfigureFromBranches_ClonesSecondPanelAt100A()
    {
        var go = new GameObject("panel");
        var panel = go.AddComponent<CircuitBreakerPanelNode>();
        var branchGo = new GameObject("branch");
        branchGo.transform.SetParent(go.transform, false);
        var branch = branchGo.AddComponent<CircuitBranchNode>();
        panel.ConfigureFromBranches(new List<float> { 60f, 60f });
        Assert.AreEqual(2, panel.placementLimit);
        Assert.IsTrue(branch.perParentPlacementLimits);
        Assert.GreaterOrEqual(branch.placementLimit, 1);
        panel.ConfigureFromBranches(new List<float> { 40f, 40f });
        Assert.AreEqual(1, panel.placementLimit);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void UtilityRoomNode_IsHousePart()
    {
        var go = new GameObject("util");
        var room = go.AddComponent<UtilityRoomNode>();
        Assert.IsInstanceOf<HousePartNode>(room);
        Assert.AreEqual(0, HouseFloorIndex.Basement);
        Object.DestroyImmediate(go);
    }
}
