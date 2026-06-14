using System;

/// <summary>Completed fine trace session snapshot.</summary>
[Serializable]
public sealed class PerfTraceSession
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
    public PerfTraceNode Root;

    public static string NewRunId() => Guid.NewGuid().ToString("N");

    public double TotalRootMs =>
        Root != null ? PerfTraceFormat.TicksToMs(Root.TotalTicks) : 0d;
}
