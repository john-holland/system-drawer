using System.Collections.Generic;

/// <summary>Builds display roots from live sessions or rough aggregates.</summary>
public static class PerfTraceTreeBuilder
{
    public enum ViewMode
    {
        Live = 0,
        RoughSummary = 1,
        SavedRun = 2
    }

    public static PerfTraceNode BuildLive()
    {
        if (!PerfTrace.TryGetLatestSession(out var session) || session?.Root == null)
            return PerfTraceNode.Create("No sessions", "", PerfTraceGrade.Fine);
        var root = session.Root.CloneTree();
        root.ApplyPercentOfParent(root.TotalTicks > 0 ? root.TotalTicks : 1);
        return root;
    }

    public static PerfTraceNode BuildFromSession(PerfTraceSession session)
    {
        if (session?.Root == null)
            return PerfTraceNode.Create("Empty run", session?.RunLabel ?? "", PerfTraceGrade.Fine);
        return WrapSessionRoot(session.Root.CloneTree(), session.RunLabel);
    }

    public static PerfTraceNode BuildRoughSummary()
    {
        var root = PerfTraceNode.Create("Rough aggregates", "", PerfTraceGrade.Rough);
        var nodes = new List<PerfTraceNode>();
        PerfTrace.CopyRoughNodes(nodes);
        long total = 0;
        for (int i = 0; i < nodes.Count; i++)
            total += nodes[i].TotalTicks;
        root.TotalTicks = total;
        root.MutableChildren.AddRange(nodes);
        root.FreezeChildren();
        root.ApplyPercentOfParent(total > 0 ? total : 1);
        return root;
    }

    static PerfTraceNode WrapSessionRoot(PerfTraceNode sessionRoot, string runLabel)
    {
        var root = PerfTraceNode.Create(runLabel ?? "Session", runLabel ?? "", PerfTraceGrade.Fine);
        root.TotalTicks = sessionRoot.TotalTicks;
        root.SelfTicks = sessionRoot.SelfTicks;
        root.Note = sessionRoot.Note;
        if (sessionRoot.Children != null && sessionRoot.Children.Length > 0)
            root.MutableChildren.AddRange(sessionRoot.Children);
        root.FreezeChildren();
        root.ApplyPercentOfParent(root.TotalTicks > 0 ? root.TotalTicks : 1);
        return root;
    }
}
