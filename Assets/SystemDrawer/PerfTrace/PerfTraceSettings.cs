using UnityEngine;

/// <summary>Runtime configuration for PerfTrace collection.</summary>
[CreateAssetMenu(menuName = "System Drawer/Perf Trace Settings", fileName = "PerfTraceSettings")]
public sealed class PerfTraceSettings : ScriptableObject
{
    static PerfTraceSettings _default;

    [Tooltip("Collect coarse note aggregates in production builds.")]
    public bool roughMetricsEnabled = true;

    [Tooltip("Allow fine scoped trees when ENABLE_PERF_TRACE_FINE is defined.")]
    public bool fineMetricsEnabled = true;

    [Min(16)] public int maxNodesPerSession = 4096;
    [Min(1)] public int maxCompletedSessions = 32;
    [Min(8)] public int maxRoughNotes = 256;
    [Min(1)] public int maxRetainedRuns = 50;

    public static PerfTraceSettings Default
    {
        get
        {
            if (_default == null)
            {
                _default = CreateInstance<PerfTraceSettings>();
                _default.hideFlags = HideFlags.HideAndDontSave;
            }
            return _default;
        }
    }
}
