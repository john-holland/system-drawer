using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

/// <summary>Static performance tracing API with rough and fine service grades.</summary>
public static class PerfTrace
{
    static PerfTraceBuffer _buffer;
    static PerfTraceSettings _settings;

    public static event Action<PerfTraceSession> SessionCompleted;

    public static PerfTraceSettings Settings
    {
        get => _settings ??= PerfTraceSettings.Default;
        set
        {
            _settings = value ?? PerfTraceSettings.Default;
            _buffer = new PerfTraceBuffer(_settings);
        }
    }

    static PerfTraceBuffer Buffer => _buffer ??= new PerfTraceBuffer(Settings);

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    [System.Diagnostics.Conditional("ENABLE_PERF_TRACE")]
    public static void Mark(
        string note,
        [CallerMemberName] string member = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        Buffer.MarkInstant(note, PerfTraceGrade.Fine, member, file, line);
        PerfTraceMarkerCache.Get(note ?? member ?? "mark").Begin();
        PerfTraceMarkerCache.Get(note ?? member ?? "mark").End();
    }

    public static void MarkRough(string note)
    {
        if (!Settings.roughMetricsEnabled)
            return;
        Buffer.Rough.Record(note ?? "mark", 0);
    }

    public static PerfTraceScope Scope(
        string note,
        [CallerMemberName] string member = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || ENABLE_PERF_TRACE
        int token = Buffer.BeginScope(note, PerfTraceGrade.Fine, member, file, line);
        return new PerfTraceScope(token, PerfTraceGrade.Fine, token >= 0);
#else
        return default;
#endif
    }

    public static PerfTraceScope ScopeRough(string note)
    {
        if (!Settings.roughMetricsEnabled)
            return default;
        int token = Buffer.BeginScope(note, PerfTraceGrade.Rough, null, null, 0);
        return new PerfTraceScope(token, PerfTraceGrade.Rough, token >= 0);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    [System.Diagnostics.Conditional("ENABLE_PERF_TRACE")]
    public static void BeginBenchmark(string label) => Buffer.BeginBenchmark(label);

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    [System.Diagnostics.Conditional("ENABLE_PERF_TRACE")]
    public static void EndBenchmark() => Buffer.EndBenchmark();

    public static void Flush() => Buffer.Flush();

    public static bool IsScopeActive => Buffer.IsScopeActive;

    public static bool TryGetLatestSession(out PerfTraceSession session) =>
        Buffer.TryGetLatestSession(out session);

    public static void CopyCompletedSessions(List<PerfTraceSession> output) =>
        Buffer.CopyCompletedSessions(output);

    public static void CopyRoughNodes(List<PerfTraceNode> output) =>
        Buffer.Rough.CopyToNodes(output);

    internal static void EndScopeInternal(int token, PerfTraceGrade grade)
    {
        if (token < 0)
            return;
        Buffer.EndScope(token, grade);
    }

    internal static void RaiseSessionCompleted(PerfTraceSession session) =>
        SessionCompleted?.Invoke(session);
}
