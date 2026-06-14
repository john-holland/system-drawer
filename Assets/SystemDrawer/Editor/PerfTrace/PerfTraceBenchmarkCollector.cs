#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

/// <summary>Auto-collects benchmark runs when Perf Trace window is open.</summary>
public static class PerfTraceBenchmarkCollector
{
    const string AutoCollectPref = "PerfTrace.AutoCollectBenchmark";

    static readonly HashSet<EditorWindow> RegisteredWindows = new HashSet<EditorWindow>();
    static bool _subscribed;
    static bool _playModeActive;
    static DateTime _playModeStartUtc;
    static readonly List<PerfTraceSession> _playModeSessions = new List<PerfTraceSession>();

    public static bool AutoCollectEnabled
    {
        get => EditorPrefs.GetBool(AutoCollectPref, false);
        set => EditorPrefs.SetBool(AutoCollectPref, value);
    }

    public static event Action RunCollected;

    public static void Register(EditorWindow window)
    {
        if (window == null)
            return;
        RegisteredWindows.Add(window);
        EnsureSubscribed();
    }

    public static void Unregister(EditorWindow window)
    {
        if (window != null)
            RegisteredWindows.Remove(window);
        if (RegisteredWindows.Count == 0)
            Unsubscribe();
    }

    public static bool IsCollecting => RegisteredWindows.Count > 0 && AutoCollectEnabled;

    static void EnsureSubscribed()
    {
        if (_subscribed)
            return;
        PerfTrace.SessionCompleted += OnSessionCompleted;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        _subscribed = true;
    }

    static void Unsubscribe()
    {
        if (!_subscribed)
            return;
        PerfTrace.SessionCompleted -= OnSessionCompleted;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        _subscribed = false;
    }

    static void OnSessionCompleted(PerfTraceSession session)
    {
        if (!IsCollecting || session?.Root == null)
            return;

        if (_playModeActive)
        {
            _playModeSessions.Add(session);
            return;
        }

        PerfTraceSessionEnricher.Enrich(session);
        PerfTraceRunHistory.SaveRun(session, playModeSession: EditorApplication.isPlaying);
        RunCollected?.Invoke();
        RepaintWindows();
    }

    static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            _playModeActive = true;
            _playModeStartUtc = DateTime.UtcNow;
            _playModeSessions.Clear();
            return;
        }

        if (change != PlayModeStateChange.EnteredEditMode)
            return;

        _playModeActive = false;
        if (!IsCollecting || _playModeSessions.Count == 0)
        {
            _playModeSessions.Clear();
            return;
        }

        var bundled = BundlePlayModeSessions();
        if (bundled != null)
        {
            PerfTraceSessionEnricher.Enrich(bundled);
            PerfTraceRunHistory.SaveRun(bundled, playModeSession: true);
            RunCollected?.Invoke();
            RepaintWindows();
        }
        _playModeSessions.Clear();
    }

    static PerfTraceSession BundlePlayModeSessions()
    {
        if (_playModeSessions.Count == 0)
            return null;

        var root = PerfTraceNode.Create(
            $"Play Mode {_playModeStartUtc.ToLocalTime():HH:mm:ss}–{DateTime.Now:HH:mm:ss}",
            "",
            PerfTraceGrade.Fine);
        long total = 0;
        for (int i = 0; i < _playModeSessions.Count; i++)
        {
            var s = _playModeSessions[i];
            if (s?.Root == null)
                continue;
            root.MutableChildren.Add(s.Root);
            total += s.Root.TotalTicks;
        }
        root.TotalTicks = total;
        root.FreezeChildren();
        root.RecomputeRollup();
        root.ApplyPercentOfParent(total > 0 ? total : 1);

        return new PerfTraceSession
        {
            RunId = PerfTraceSession.NewRunId(),
            RunLabel = root.Label,
            CapturedUtc = DateTime.UtcNow.ToString("o"),
            StartedUtc = _playModeStartUtc.ToString("o"),
            Root = root
        };
    }

    static void RepaintWindows()
    {
        foreach (var w in RegisteredWindows)
        {
            if (w != null)
                w.Repaint();
        }
    }
}
#endif
