using System;

/// <summary>Persisted benchmark run metadata (stored as JSON).</summary>
[Serializable]
public sealed class PerfTraceRunRecord
{
    public string id = "";
    public string label = "";
    public string startedUtc = "";
    public string endedUtc = "";
    public string sessionFile = "";
    public bool playModeSession;
    public double totalRootMs;
    public string correlationUtc = "";
}
