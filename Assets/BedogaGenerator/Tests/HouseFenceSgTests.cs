using NUnit.Framework;
using UnityEngine;

public sealed class HouseFenceSgTests
{
    [Test]
    public void FenceRun_CountPostsAndPanels()
    {
        var go = new GameObject("fence");
        var run = go.AddComponent<FenceRunNode>();
        run.postSpacingM = 2.4f;
        Assert.AreEqual(4, run.CountPosts(7.2f));
        Assert.AreEqual(3, run.CountPanels(4));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void FenceRun_ConfigureRepeat_SetsPerParentLimits()
    {
        var go = new GameObject("fence");
        var run = go.AddComponent<FenceRunNode>();
        var postGo = new GameObject("post");
        postGo.transform.SetParent(go.transform, false);
        var post = postGo.AddComponent<FencePostNode>();
        var panelGo = new GameObject("panel");
        panelGo.transform.SetParent(go.transform, false);
        var panel = panelGo.AddComponent<FencePanelNode>();
        run.ConfigureRepeat(5);
        Assert.AreEqual(1, run.placementLimit);
        Assert.IsTrue(post.perParentPlacementLimits);
        Assert.AreEqual(5, post.placementLimit);
        Assert.IsTrue(panel.perParentPlacementLimits);
        Assert.AreEqual(4, panel.placementLimit);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void FenceRun_CompileGateJointIds()
    {
        var go = new GameObject("fence");
        var run = go.AddComponent<FenceRunNode>();
        var splineGo = new GameObject("spline");
        var spline = splineGo.AddComponent<RoadLotBoundarySpline>();
        spline.wallSections.Add(new RoadLotWallSection { isGap = true, gateOpenCloseTopologyId = "gate_a" });
        spline.wallSections.Add(new RoadLotWallSection { isGap = false });
        var ids = run.CompileGateJointIds(spline);
        Assert.AreEqual(1, ids.Count);
        Assert.AreEqual("gate_a", ids[0]);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(splineGo);
    }
}
