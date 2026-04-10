#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Locomotion.Narrative;

/// <summary>
/// Tests for supplementary Back / Pause / Forward gateway leaf ids (same volume center, sampled at tMin, centerT, tMax)
/// and related causality history DTOs.
/// </summary>
public class SpatialGenerator4DGatewayTerminiTests
{
    static string SlicePrefix(string causalityLeafId)
    {
        if (string.IsNullOrEmpty(causalityLeafId))
            return null;
        int dot = causalityLeafId.IndexOf('.');
        return dot > 0 ? causalityLeafId.Substring(0, dot) : causalityLeafId;
    }

    [Test]
    public void GetClosestGatewayTerminusName_AtTMin_IsBack()
    {
        var vol = new Bounds4(Vector3.zero, Vector3.one * 2f, 0f, 100f);
        Assert.AreEqual("Back", SpatialGenerator4D.GetClosestGatewayTerminusName(vol, 0f));
    }

    [Test]
    public void GetClosestGatewayTerminusName_AtCenterT_IsPause()
    {
        var vol = new Bounds4(Vector3.zero, Vector3.one * 2f, 0f, 100f);
        Assert.AreEqual("Pause", SpatialGenerator4D.GetClosestGatewayTerminusName(vol, 50f));
    }

    [Test]
    public void GetClosestGatewayTerminusName_AtTMax_IsForward()
    {
        var vol = new Bounds4(Vector3.zero, Vector3.one * 2f, 0f, 100f);
        Assert.AreEqual("Forward", SpatialGenerator4D.GetClosestGatewayTerminusName(vol, 100f));
    }

    [Test]
    public void GetClosestGatewayTerminusName_TiePrefersBack()
    {
        var vol = new Bounds4(Vector3.zero, Vector3.one * 2f, 0f, 100f);
        // 25 is equally close to tMin (0) and centerT (50); implementation picks Back first.
        Assert.AreEqual("Back", SpatialGenerator4D.GetClosestGatewayTerminusName(vol, 25f));
    }

    [Test]
    public void ClampNarrativeTime_ClampsToGeneratorRange()
    {
        var go = new GameObject("SG4D_Clamp");
        var sg = go.AddComponent<SpatialGenerator4D>();
        sg.tMin = 10f;
        sg.tMax = 20f;
        Assert.AreEqual(10f, sg.ClampNarrativeTime(0f));
        Assert.AreEqual(20f, sg.ClampNarrativeTime(100f));
        Assert.AreEqual(15f, sg.ClampNarrativeTime(15f));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void TryGetGatewayLeafIds_ReturnsNonNullIds_ForPlacedVolume()
    {
        var go = new GameObject("SG4D_Gateway");
        var sg = go.AddComponent<SpatialGenerator4D>();
        sg.spatialBounds = new Bounds(Vector3.zero, Vector3.one * 100f);
        sg.tMin = 0f;
        sg.tMax = 1000f;
        sg.sliceCount = 10;
        sg.maxObjectsPerNode = 64;
        sg.maxDepth = 4;

        var volume = new Bounds4(new Vector3(1f, 2f, 3f), Vector3.one * 4f, 10f, 90f);
        Assert.IsTrue(sg.Insert(volume, "test-payload"));

        var markers = sg.Search(new Bounds(volume.center, Vector3.one * 0.01f), volume.centerT);
        Assert.Greater(markers.Count, 0);
        var marker = markers[0];

        bool ok = sg.TryGetGatewayLeafIds(marker, volume, out string back, out string pause, out string forward);
        Assert.IsTrue(ok);
        Assert.IsFalse(string.IsNullOrEmpty(back));
        Assert.IsFalse(string.IsNullOrEmpty(pause));
        Assert.IsFalse(string.IsNullOrEmpty(forward));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void TryGetGatewayLeafIds_BackPauseForward_UseDifferentSlices_WhenWindowSpansThem()
    {
        var go = new GameObject("SG4D_MultiSlice");
        var sg = go.AddComponent<SpatialGenerator4D>();
        sg.spatialBounds = new Bounds(Vector3.zero, Vector3.one * 100f);
        sg.tMin = 0f;
        sg.tMax = 1000f;
        sg.sliceCount = 10;
        sg.maxObjectsPerNode = 64;
        sg.maxDepth = 4;

        // tMin -> slice ~0, centerT -> ~4, tMax -> ~9
        var volume = new Bounds4(new Vector3(5f, 0f, 5f), Vector3.one * 2f, 10f, 900f);
        Assert.IsTrue(sg.Insert(volume, "wide-t"));

        var markers = sg.Search(new Bounds(volume.center, Vector3.one * 0.01f), volume.centerT);
        Assert.Greater(markers.Count, 0);

        Assert.IsTrue(sg.TryGetGatewayLeafIds(markers[0], volume, out string back, out string pause, out string forward));

        string sBack = SlicePrefix(back);
        string sPause = SlicePrefix(pause);
        string sForward = SlicePrefix(forward);

        Assert.IsNotNull(sBack);
        Assert.IsNotNull(sPause);
        Assert.IsNotNull(sForward);
        Assert.AreNotEqual(sBack, sForward, "Back and forward samples should land in different time slices for this window.");
        Assert.AreNotEqual(sPause, sBack, "Pause (centerT) should not share the same slice prefix as tMin for this window.");
        Assert.AreNotEqual(sPause, sForward, "Pause should not share the same slice prefix as tMax for this window.");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void GetPlacedEntriesWithGatewayTermini_MatchesTryGetGatewayLeafIds()
    {
        var go = new GameObject("SG4D_Entries");
        var sg = go.AddComponent<SpatialGenerator4D>();
        sg.spatialBounds = new Bounds(Vector3.zero, Vector3.one * 50f);
        sg.tMin = 0f;
        sg.tMax = 500f;
        sg.sliceCount = 5;
        sg.maxObjectsPerNode = 32;
        sg.maxDepth = 6;

        var volume = new Bounds4(new Vector3(2f, 2f, 2f), Vector3.one, 40f, 120f);
        sg.Insert(volume, "payload-a");

        var list = sg.GetPlacedEntriesWithGatewayTermini();
        Assert.AreEqual(1, list.Count);
        var (v, payload, triplet) = list[0];
        Assert.AreEqual("payload-a", payload);
        Assert.AreEqual(volume.center, v.center);
        Assert.IsNotNull(triplet);
        Assert.IsNotNull(triplet.back);
        Assert.IsNotNull(triplet.pause);
        Assert.IsNotNull(triplet.forward);

        var markers = sg.Search(new Bounds(volume.center, Vector3.one * 0.01f), volume.centerT);
        sg.TryGetGatewayLeafIds(markers[0], volume, out string eb, out string ep, out string ef);

        Assert.AreEqual(eb, triplet.back.causalityLeafId);
        Assert.AreEqual(ep, triplet.pause.causalityLeafId);
        Assert.AreEqual(ef, triplet.forward.causalityLeafId);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void TryFindMarkerForPlacedVolume_FindsMarkerAfterInsert()
    {
        var go = new GameObject("SG4D_Find");
        var sg = go.AddComponent<SpatialGenerator4D>();
        sg.spatialBounds = new Bounds(Vector3.zero, Vector3.one * 40f);
        sg.tMin = 0f;
        sg.tMax = 200f;
        sg.sliceCount = 4;
        sg.maxObjectsPerNode = 16;

        var volume = new Bounds4(Vector3.one * 3f, Vector3.one * 2f, 5f, 55f);
        sg.Insert(volume, "find-me");

        Assert.IsTrue(sg.TryFindMarkerForPlacedVolume(volume, "find-me", out GameObject marker));
        Assert.IsNotNull(marker);
        Assert.IsTrue(sg.TryGetGatewayLeafIds(marker, volume, out _, out _, out _));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Spatial4DTerminusTriplet_FromLeafIds_BuildsChildren()
    {
        var t = Spatial4DTerminusTriplet.FromLeafIds("S0.O1", null, "S2.O3");
        Assert.AreEqual("S0.O1", t.back.causalityLeafId);
        Assert.IsNull(t.pause);
        Assert.AreEqual("S2.O3", t.forward.causalityLeafId);
    }

    [Test]
    public void CausalityHistory2D_AppendRow_AndCloneForExport()
    {
        var h = new CausalityHistory2D();
        h.AppendRow("B1", "P1", "F1", 7, 99f, new Vector3(1, 2, 3), "volume_enter", null);
        Assert.AreEqual(1, h.rows.Count);
        Assert.AreEqual("B1", h.rows[0].leafBack);
        Assert.AreEqual(7, h.rows[0].flags);
        Assert.AreEqual(99f, h.rows[0].narrativeT, 0.001f);

        var named = new List<CausalityNamedFlagEntryDto>
        {
            new CausalityNamedFlagEntryDto { key = "door", value = 1 }
        };
        h.AppendRow("B2", "P2", "F2", 0, 100f, Vector3.zero, "mark", named);
        Assert.AreEqual(2, h.rows.Count);
        Assert.AreEqual(1, h.rows[1].namedFlags.Count);
        Assert.AreEqual("door", h.rows[1].namedFlags[0].key);

        var copy = h.CloneForExport();
        Assert.AreEqual(2, copy.rows.Count);
        Assert.AreEqual("B1", copy.rows[0].leafBack);
        copy.rows[0].leafBack = "mutated";
        Assert.AreEqual("B1", h.rows[0].leafBack);
    }

    [Test]
    public void CausalityHistory2D_MergeAppend_AppendsAllRows()
    {
        var a = new CausalityHistory2D();
        a.AppendRow("a", "b", "c", 0, 0f, Vector3.zero, "e1", null);
        var b = new CausalityHistory2D();
        b.AppendRow("x", "y", "z", 1, 1f, Vector3.one, "e2", null);
        CausalityHistory2D.MergeAppend(a, b);
        Assert.AreEqual(2, a.rows.Count);
        Assert.AreEqual("z", a.rows[1].leafForward);
    }
}
#endif
