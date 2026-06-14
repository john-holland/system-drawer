#if UNITY_EDITOR
using System;
using UnityEngine;

/// <summary>JsonUtility-safe session persistence (Unity max depth is 10).</summary>
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
        public NodeDto Root;
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
        public NodeDto[] Children = Array.Empty<NodeDto>();
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
            Root = ToNodeDto(session.Root, 0)
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
            Root = FromNodeDto(dto.Root)
        };
    }

    static NodeDto ToNodeDto(PerfTraceNode node, int depth)
    {
        if (node == null)
            return null;

        var dto = new NodeDto
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

        if (node.Children == null || node.Children.Length == 0)
            return dto;

        if (depth >= MaxNodeDepth)
        {
            dto.Children = Array.Empty<NodeDto>();
            return dto;
        }

        dto.Children = new NodeDto[node.Children.Length];
        for (int i = 0; i < node.Children.Length; i++)
            dto.Children[i] = ToNodeDto(node.Children[i], depth + 1);
        return dto;
    }

    static PerfTraceNode FromNodeDto(NodeDto dto)
    {
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

        if (dto.Children == null || dto.Children.Length == 0)
        {
            node.Children = Array.Empty<PerfTraceNode>();
            return node;
        }

        node.Children = new PerfTraceNode[dto.Children.Length];
        for (int i = 0; i < dto.Children.Length; i++)
            node.Children[i] = FromNodeDto(dto.Children[i]);
        return node;
    }
}
#endif
