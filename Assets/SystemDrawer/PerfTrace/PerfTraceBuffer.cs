using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Profiling;
using UnityEngine;

/// <summary>Thread-local scope stacks and completed session ring buffer.</summary>
public sealed class PerfTraceBuffer
{
    sealed class ScopeFrame
    {
        public PerfTraceNode Node;
        public string Note;
        public long StartTicks;
        public long StartGcBytes;
        public ProfilerMarker.AutoScope ProfilerScope;
    }

    [ThreadStatic] static Stack<ScopeFrame> _stack;
    [ThreadStatic] static int _nextToken;

    readonly object _sessionLock = new object();
    readonly List<PerfTraceSession> _completed = new List<PerfTraceSession>();
    readonly PerfTraceRoughAggregator _rough;
    readonly PerfTraceSettings _settings;
    int _nodeBudget;
    string _benchmarkLabel;
    DateTime _benchmarkStartedUtc;
    bool _benchmarkActive;

    public PerfTraceBuffer(PerfTraceSettings settings)
    {
        _settings = settings ?? PerfTraceSettings.Default;
        _rough = new PerfTraceRoughAggregator(_settings.maxRoughNotes);
        _nodeBudget = _settings.maxNodesPerSession;
    }

    static Stack<ScopeFrame> Stack => _stack ??= new Stack<ScopeFrame>();

    public bool IsScopeActive => Stack.Count > 0 && Stack.Peek().Node != null;

    public PerfTraceRoughAggregator Rough => _rough;

    public void BeginBenchmark(string label)
    {
        _benchmarkLabel = string.IsNullOrEmpty(label) ? "Benchmark" : label;
        _benchmarkStartedUtc = DateTime.UtcNow;
        _benchmarkActive = true;
    }

    public void EndBenchmark()
    {
        _benchmarkActive = false;
    }

    public string CurrentBenchmarkLabel => _benchmarkActive ? _benchmarkLabel : null;

    public int BeginScope(string note, PerfTraceGrade grade, string member, string file, int line)
    {
        if (grade == PerfTraceGrade.Rough)
        {
            if (!_settings.roughMetricsEnabled)
                return -1;
            Stack.Push(new ScopeFrame { StartTicks = Stopwatch.GetTimestamp(), Note = note ?? member ?? "scope" });
            return ++_nextToken;
        }
        else if (!_settings.fineMetricsEnabled)
        {
            return -1;
        }

        int token = ++_nextToken;
        long start = Stopwatch.GetTimestamp();

        if (_nodeBudget <= 0)
            return -1;

        string label = string.IsNullOrEmpty(note) ? member : note;
        var node = PerfTraceNode.Create(label, note ?? "", grade);
        node.SourceMember = member ?? "";
        node.SourceFile = file ?? "";
        node.SourceLine = line;
        node.TotalTicks = 0;
        node.SelfTicks = 0;
        _nodeBudget--;

        var parentFrame = Stack.Count > 0 ? Stack.Peek() : null;
        if (parentFrame?.Node != null)
            parentFrame.Node.MutableChildren.Add(node);

        long gcStart = 0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD || ENABLE_PERF_TRACE
        gcStart = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
#endif

        var frame = new ScopeFrame
        {
            Node = node,
            StartTicks = start,
            StartGcBytes = gcStart,
            ProfilerScope = PerfTraceMarkerCache.Get(note ?? label).Auto()
        };
        Stack.Push(frame);
        return token;
    }

    public void EndScope(int token, PerfTraceGrade grade)
    {
        if (Stack.Count == 0)
            return;

        var frame = Stack.Pop();
        long elapsed = Stopwatch.GetTimestamp() - frame.StartTicks;
        if (elapsed < 0)
            elapsed = 0;

        if (grade == PerfTraceGrade.Rough)
        {
            _rough.Record(frame.Note ?? "scope", elapsed);
            return;
        }

        frame.ProfilerScope.Dispose();

        if (frame.Node == null)
            return;

        frame.Node.TotalTicks = elapsed;
        frame.Node.SelfTicks = elapsed;
        frame.Node.FreezeChildren();
        frame.Node.RecomputeRollup();

#if UNITY_EDITOR || DEVELOPMENT_BUILD || ENABLE_PERF_TRACE
        long gcEnd = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        frame.Node.GcAllocBytes = Math.Max(0, gcEnd - frame.StartGcBytes);
#endif

        if (Stack.Count == 0)
            CompleteRootSession(frame.Node);
    }

    public void MarkInstant(string note, PerfTraceGrade grade, string member, string file, int line)
    {
        if (grade == PerfTraceGrade.Rough)
        {
            if (_settings.roughMetricsEnabled)
                _rough.Record(note ?? member ?? "mark", 0);
            return;
        }

        if (!_settings.fineMetricsEnabled || _nodeBudget <= 0)
            return;

        var node = PerfTraceNode.Create(note ?? member ?? "mark", note ?? "", grade);
        node.SourceMember = member ?? "";
        node.SourceFile = file ?? "";
        node.SourceLine = line;
        node.TotalTicks = 0;
        node.SelfTicks = 0;
        _nodeBudget--;

        if (Stack.Count > 0 && Stack.Peek().Node != null)
            Stack.Peek().Node.MutableChildren.Add(node);
        else
            CompleteRootSession(node);
    }

    void CompleteRootSession(PerfTraceNode root)
    {
        root.FreezeChildren();
        root.RecomputeRollup();
        root.ApplyPercentOfParent(root.TotalTicks);

        if (!ShouldPersistRootSession(root))
        {
            _nodeBudget = _settings.maxNodesPerSession;
            return;
        }

        var session = new PerfTraceSession
        {
            RunId = PerfTraceSession.NewRunId(),
            RunLabel = ResolveRunLabel(root),
            CapturedUtc = DateTime.UtcNow.ToString("o"),
            StartedUtc = _benchmarkActive ? _benchmarkStartedUtc.ToString("o") : DateTime.UtcNow.ToString("o"),
            Root = root.CloneTree(),
            FrameIndex = Time.frameCount,
            Platform = Application.platform.ToString()
        };

        lock (_sessionLock)
        {
            _completed.Add(session);
            while (_completed.Count > _settings.maxCompletedSessions)
                EvictOneCompletedSession();
        }

        _nodeBudget = _settings.maxNodesPerSession;
        PerfTrace.RaiseSessionCompleted(session);
    }

    static bool ShouldPersistRootSession(PerfTraceNode root)
    {
        if (root == null)
            return false;

        string label = string.IsNullOrEmpty(root.Label) ? root.Note : root.Label;
#if UNITY_EDITOR
        if (!Application.isPlaying && label == "SyncRenderComponents")
            return false;
#endif
        return true;
    }

    void EvictOneCompletedSession()
    {
        if (_completed.Count == 0)
            return;

        int victim = 0;
        long smallest = _completed[0].Root?.TotalTicks ?? long.MaxValue;
        for (int i = 1; i < _completed.Count; i++)
        {
            long ticks = _completed[i].Root?.TotalTicks ?? long.MaxValue;
            if (ticks < smallest)
            {
                smallest = ticks;
                victim = i;
            }
        }
        _completed.RemoveAt(victim);
    }

    string ResolveRunLabel(PerfTraceNode root)
    {
        if (!string.IsNullOrEmpty(_benchmarkLabel) && _benchmarkActive)
            return _benchmarkLabel;
        if (!string.IsNullOrEmpty(root.Note))
            return root.Note;
        if (!string.IsNullOrEmpty(root.Label))
            return root.Label;
        return "Unnamed run";
    }

    public bool TryGetLatestSession(out PerfTraceSession session)
    {
        lock (_sessionLock)
        {
            if (_completed.Count == 0)
            {
                session = null;
                return false;
            }
            session = _completed[_completed.Count - 1];
            return session != null;
        }
    }

    public void CopyCompletedSessions(List<PerfTraceSession> output)
    {
        output.Clear();
        lock (_sessionLock)
        {
            output.AddRange(_completed);
        }
    }

    public void Flush()
    {
        Stack.Clear();
        _nodeBudget = _settings.maxNodesPerSession;
        lock (_sessionLock)
        {
            _completed.Clear();
        }
    }
}
