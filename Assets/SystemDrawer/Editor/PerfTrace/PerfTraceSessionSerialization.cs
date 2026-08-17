#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>JsonUtility-safe session persistence. Nodes are flattened so nested Children never hit Unity's depth-10 limit.</summary>
public static class PerfTraceSessionSerialization
{
    const int MaxNodeDepth = 8;

    [Serializable]
    sealed class SessionDto
    {
        public string RunId = "";
        public string RunLabel = "";
        public string CapturedUtc = "";
        public string StartedUtc = "";
        public string CorrelationUtc = "";
        public int FrameIndex;
        public double CpuFrameMs;
        public double GpuFrameMs;
        public string ScriptingBackend = "";
        public string Platform = "";
        public string MemoryCounters = "";
        public int RootIndex;
        public NodeDto[] Nodes = Array.Empty<NodeDto>();
    }

    [Serializable]
    sealed class NodeDto
    {
        public string Id = "";
        public string Label = "";
        public string Note = "";
        public long TotalTicks;
        public long SelfTicks;
        public int CallCount = 1;
        public int Grade;
        public string SourceMember = "";
        public string SourceFile = "";
        public int SourceLine;
        public string AssemblyName = "";
        public long GcAllocBytes;
        public int[] ChildIndices = Array.Empty<int>();
    }

    public static string ToJson(PerfTraceSession session, bool prettyPrint)
    {
        if (session == null)
            return "{}";
        return JsonUtility.ToJson(ToDto(session), prettyPrint);
    }

    public static PerfTraceSession FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
        try
        {
            var dto = JsonUtility.FromJson<SessionDto>(json);
            return dto == null ? null : FromDto(dto);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PerfTrace: failed to parse session JSON. " + ex.Message);
            return null;
        }
    }

    static SessionDto ToDto(PerfTraceSession session)
    {
        var nodes = new List<NodeDto>();
        int rootIndex = Flatten(session.Root, nodes, 0);
        return new SessionDto
        {
            RunId = session.RunId ?? "",
            RunLabel = session.RunLabel ?? "",
            CapturedUtc = session.CapturedUtc ?? "",
            StartedUtc = session.StartedUtc ?? "",
            CorrelationUtc = session.CorrelationUtc ?? "",
            FrameIndex = session.FrameIndex,
            CpuFrameMs = session.CpuFrameMs,
            GpuFrameMs = session.GpuFrameMs,
            ScriptingBackend = session.ScriptingBackend ?? "",
            Platform = session.Platform ?? "",
            MemoryCounters = session.MemoryCounters ?? "",
            RootIndex = rootIndex,
            Nodes = nodes.ToArray()
        };
    }

    static PerfTraceSession FromDto(SessionDto dto)
    {
        return new PerfTraceSession
        {
            RunId = dto.RunId,
            RunLabel = dto.RunLabel,
            CapturedUtc = dto.CapturedUtc,
            StartedUtc = dto.StartedUtc,
            CorrelationUtc = dto.CorrelationUtc,
            FrameIndex = dto.FrameIndex,
            CpuFrameMs = dto.CpuFrameMs,
            GpuFrameMs = dto.GpuFrameMs,
            ScriptingBackend = dto.ScriptingBackend,
            Platform = dto.Platform,
            MemoryCounters = dto.MemoryCounters,
            Root = FromFlat(dto.Nodes, dto.RootIndex)
        };
    }

    static int Flatten(PerfTraceNode node, List<NodeDto> nodes, int depth)
    {
        if (node == null)
            return -1;

        var dto = CopyNode(node);
        int index = nodes.Count;
        nodes.Add(dto);

        if (node.Children == null || node.Children.Length == 0 || depth >= MaxNodeDepth)
            return index;

        var childIndices = new int[node.Children.Length];
        for (int i = 0; i < node.Children.Length; i++)
            childIndices[i] = Flatten(node.Children[i], nodes, depth + 1);
        dto.ChildIndices = childIndices;
        return index;
    }

    static NodeDto CopyNode(PerfTraceNode node)
    {
        return new NodeDto
        {
            Id = node.Id ?? "",
            Label = node.Label ?? "",
            Note = node.Note ?? "",
            TotalTicks = node.TotalTicks,
            SelfTicks = node.SelfTicks,
            CallCount = node.CallCount,
            Grade = (int)node.Grade,
            SourceMember = node.SourceMember ?? "",
            SourceFile = node.SourceFile ?? "",
            SourceLine = node.SourceLine,
            AssemblyName = node.AssemblyName ?? "",
            GcAllocBytes = node.GcAllocBytes
        };
    }

    static PerfTraceNode FromFlat(NodeDto[] nodes, int index)
    {
        if (nodes == null || index < 0 || index >= nodes.Length)
            return null;

        var dto = nodes[index];
        if (dto == null)
            return null;

        var node = new PerfTraceNode
        {
            Id = dto.Id,
            Label = dto.Label,
            Note = dto.Note,
            TotalTicks = dto.TotalTicks,
            SelfTicks = dto.SelfTicks,
            CallCount = dto.CallCount,
            Grade = (PerfTraceGrade)dto.Grade,
            SourceMember = dto.SourceMember,
            SourceFile = dto.SourceFile,
            SourceLine = dto.SourceLine,
            AssemblyName = dto.AssemblyName,
            GcAllocBytes = dto.GcAllocBytes
        };

        if (dto.ChildIndices == null || dto.ChildIndices.Length == 0)
        {
            node.Children = Array.Empty<PerfTraceNode>();
            return node;
        }

        node.Children = new PerfTraceNode[dto.ChildIndices.Length];
        for (int i = 0; i < dto.ChildIndices.Length; i++)
            node.Children[i] = FromFlat(nodes, dto.ChildIndices[i]);
        return node;
    }
}
#endif
