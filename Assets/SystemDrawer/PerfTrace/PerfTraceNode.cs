using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One node in a PerfTrace scope hierarchy.</summary>
[Serializable]
public sealed class PerfTraceNode
{
    public string Id = "";
    public string Label = "";
    public string Note = "";
    public long TotalTicks;
    public long SelfTicks;
    public int CallCount = 1;
    public PerfTraceGrade Grade = PerfTraceGrade.Fine;
    public string SourceMember = "";
    public string SourceFile = "";
    public int SourceLine;
    public string AssemblyName = "";
    public long GcAllocBytes;
    public PerfTraceNode[] Children = Array.Empty<PerfTraceNode>();

    [NonSerialized] public Rect LayoutRect;
    [NonSerialized] public float PercentOfParent;

    public List<PerfTraceNode> MutableChildren => _mutableChildren ??= new List<PerfTraceNode>();

    [NonSerialized] List<PerfTraceNode> _mutableChildren;

    public void FreezeChildren()
    {
        if (_mutableChildren == null || _mutableChildren.Count == 0)
        {
            Children ??= Array.Empty<PerfTraceNode>();
            return;
        }
        Children = _mutableChildren.ToArray();
        _mutableChildren = null;
    }

    public void ApplyPercentOfParent(long parentTicks)
    {
        PercentOfParent = parentTicks > 0 ? (float)TotalTicks / parentTicks : 0f;
        if (Children == null)
            return;
        for (int i = 0; i < Children.Length; i++)
            Children[i]?.ApplyPercentOfParent(TotalTicks > 0 ? TotalTicks : parentTicks);
    }

    public void RecomputeRollup()
    {
        if (Children == null || Children.Length == 0)
        {
            SelfTicks = TotalTicks;
            return;
        }

        long childSum = 0;
        for (int i = 0; i < Children.Length; i++)
        {
            Children[i].RecomputeRollup();
            childSum += Children[i].TotalTicks;
        }
        SelfTicks = Math.Max(0, TotalTicks - childSum);
    }

    public static PerfTraceNode Create(string label, string note, PerfTraceGrade grade)
    {
        return new PerfTraceNode
        {
            Id = Guid.NewGuid().ToString("N"),
            Label = label ?? "",
            Note = note ?? "",
            Grade = grade
        };
    }

    public PerfTraceNode CloneTree()
    {
        var clone = new PerfTraceNode
        {
            Id = Id,
            Label = Label,
            Note = Note,
            TotalTicks = TotalTicks,
            SelfTicks = SelfTicks,
            CallCount = CallCount,
            Grade = Grade,
            SourceMember = SourceMember,
            SourceFile = SourceFile,
            SourceLine = SourceLine,
            AssemblyName = AssemblyName,
            GcAllocBytes = GcAllocBytes
        };
        if (Children != null && Children.Length > 0)
        {
            clone.Children = new PerfTraceNode[Children.Length];
            for (int i = 0; i < Children.Length; i++)
                clone.Children[i] = Children[i]?.CloneTree();
        }
        return clone;
    }
}
