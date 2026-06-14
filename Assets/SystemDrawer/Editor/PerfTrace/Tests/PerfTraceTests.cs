#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PerfTraceTests
{
    [SetUp]
    public void SetUp()
    {
        PerfTrace.Flush();
        PerfTrace.Settings = PerfTraceSettings.Default;
    }

    [Test]
    public void Scope_RollupSelfTicks_MatchesChildren()
    {
        using (PerfTrace.Scope("parent"))
        {
            using (PerfTrace.Scope("child")) { }
        }

        Assert.IsTrue(PerfTrace.TryGetLatestSession(out var session));
        Assert.NotNull(session.Root);
        session.Root.RecomputeRollup();
        long childSum = 0;
        if (session.Root.Children != null)
        {
            for (int i = 0; i < session.Root.Children.Length; i++)
                childSum += session.Root.Children[i].TotalTicks;
        }
        Assert.Greater(session.Root.TotalTicks, 0);
        Assert.AreEqual(session.Root.TotalTicks - childSum, session.Root.SelfTicks);
    }

    [Test]
    public void HistogramLayout_BarWidths_WithinPanel()
    {
        var nodes = new List<PerfTraceNode>
        {
            new PerfTraceNode { Label = "a", TotalTicks = 300 },
            new PerfTraceNode { Label = "b", TotalTicks = 700 }
        };
        var area = new Rect(0, 0, 200, 100);
        PerfTraceHistogramLayout.Apply(nodes, area);
        Assert.Greater(nodes[0].LayoutRect.width, 0f);
        Assert.LessOrEqual(nodes[0].LayoutRect.xMax, area.xMax + 1f);
        Assert.LessOrEqual(nodes[1].LayoutRect.xMax, area.xMax + 1f);
    }

    [Test]
    public void RoughAggregator_Evicts_WhenOverCap()
    {
        var agg = new PerfTraceRoughAggregator(4);
        for (int i = 0; i < 8; i++)
            agg.Record("note-" + i, 100 + i);
        Assert.LessOrEqual(agg.Count, 4);
    }

    [Test]
    public void RunHistory_SaveLoad_PreservesTotals()
    {
        var session = new PerfTraceSession
        {
            RunId = "test-run",
            RunLabel = "unit-test",
            CapturedUtc = System.DateTime.UtcNow.ToString("o"),
            Root = PerfTraceNode.Create("root", "root", PerfTraceGrade.Fine)
        };
        session.Root.TotalTicks = 12345;
        session.Root.SelfTicks = 12345;

        PerfTraceRunHistory.SaveRun(session, false);
        var loaded = PerfTraceRunHistory.LoadSession("test-run");
        Assert.NotNull(loaded);
        Assert.AreEqual(12345, loaded.Root.TotalTicks);
        PerfTraceRunHistory.DeleteRun("test-run");
    }

    [Test]
    public void Buffer_DropsEditorStandaloneRenderSync()
    {
        PerfTrace.Flush();
        var before = new List<PerfTraceSession>();
        PerfTrace.CopyCompletedSessions(before);

        using (PerfTrace.Scope("SyncRenderComponents")) { }

        var after = new List<PerfTraceSession>();
        PerfTrace.CopyCompletedSessions(after);
        Assert.AreEqual(before.Count, after.Count);
    }

    [Test]
    public void Buffer_KeepsRebuildAllSession()
    {
        PerfTrace.Flush();
        using (PerfTrace.Scope("RebuildAll"))
        {
            using (PerfTrace.Scope("RebakeComposition")) { }
        }

        Assert.IsTrue(PerfTrace.TryGetLatestSession(out var session));
        Assert.AreEqual("RebuildAll", session.RunLabel);
        Assert.NotNull(session.Root.Children);
        Assert.Greater(session.Root.Children.Length, 0);
    }

    [Test]
    public void SessionSerialization_DeepTree_DoesNotThrow()
    {
        PerfTraceNode root = PerfTraceNode.Create("root", "root", PerfTraceGrade.Fine);
        root.TotalTicks = 5000;
        PerfTraceNode current = root;
        for (int i = 0; i < 12; i++)
        {
            var child = PerfTraceNode.Create("depth-" + i, "", PerfTraceGrade.Fine);
            child.TotalTicks = 100 + i;
            current.MutableChildren.Add(child);
            current.FreezeChildren();
            current = child;
        }
        current.FreezeChildren();
        root.FreezeChildren();
        root.RecomputeRollup();

        var session = new PerfTraceSession
        {
            RunId = "deep-run",
            RunLabel = "deep-tree",
            CapturedUtc = System.DateTime.UtcNow.ToString("o"),
            Root = root
        };

        string json = PerfTraceSessionSerialization.ToJson(session, false);
        Assert.IsFalse(string.IsNullOrEmpty(json));
        var loaded = PerfTraceSessionSerialization.FromJson(json);
        Assert.NotNull(loaded?.Root);
        Assert.AreEqual(5000, loaded.Root.TotalTicks);
    }
}
#endif
