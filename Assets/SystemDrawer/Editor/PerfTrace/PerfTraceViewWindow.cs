#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Perf Trace debugger: breadcrumb histogram drill-down for scoped timings.</summary>
public class PerfTraceViewWindow : EditorWindow
{
    [NonSerialized] PerfTraceTreeBuilder.ViewMode _viewMode = PerfTraceTreeBuilder.ViewMode.Live;
    [NonSerialized] PerfTraceNode _root;
    [NonSerialized] List<PerfTraceNode> _focusStack;
    [NonSerialized] PerfTraceNode _hover;
    [NonSerialized] PerfTraceNode _selected;
    [NonSerialized] List<PerfTraceSession> _liveSessions;
    [NonSerialized] List<PerfTraceRunRecord> _runHistory;
    [NonSerialized] List<string> _runDropdownLabels;
    [NonSerialized] int _selectedRunIndex;
    [NonSerialized] string _selectedRunId;
    [NonSerialized] bool _autoRefresh;
    [NonSerialized] double _lastRefresh;
    const double AutoRefreshSeconds = 0.5;
    [NonSerialized] Vector2 _sideScroll;
    [NonSerialized] Rect _histogramArea;
    [NonSerialized] PerfTraceSession _loadedSession;
    [NonSerialized] int _lastAppliedRunIndex = -1;

    public static void Open()
    {
        var w = GetWindow<PerfTraceViewWindow>("Perf Trace");
        w.minSize = new Vector2(720, 480);
        w.Show();
    }

    void OnEnable()
    {
        _focusStack ??= new List<PerfTraceNode>();
        _liveSessions ??= new List<PerfTraceSession>();
        _runHistory ??= new List<PerfTraceRunRecord>();
        _runDropdownLabels ??= new List<string>();
        PerfTraceBenchmarkCollector.Register(this);
        PerfTraceBenchmarkCollector.RunCollected += OnRunCollected;
        PerfTraceRunHistory.HistoryChanged += RefreshRunHistory;
        PerfTraceMemoryCorrelator.CorrelationUpdated += Repaint;
        PerfTrace.SessionCompleted += OnSessionCompleted;
        RefreshRunHistory();
        ApplyRunSelection();
    }

    void OnDisable()
    {
        PerfTraceBenchmarkCollector.Unregister(this);
        PerfTraceBenchmarkCollector.RunCollected -= OnRunCollected;
        PerfTraceRunHistory.HistoryChanged -= RefreshRunHistory;
        PerfTraceMemoryCorrelator.CorrelationUpdated -= Repaint;
        PerfTrace.SessionCompleted -= OnSessionCompleted;
    }

    void OnSessionCompleted(PerfTraceSession session)
    {
        if (ShouldAutoSelectSession(session))
            _selectedRunId = session.RunId;
        RefreshRunHistory();
        if (IsLiveSelection())
            RebuildTree();
        Repaint();
    }
    void OnRunCollected() => RefreshRunHistory();

    void OnGUI()
    {
        DrawToolbar();
        DrawRunHistoryRow();
        PerfTraceBreadcrumbBar.Draw(_focusStack, OnBreadcrumbRoot, OnBreadcrumbBack, OnBreadcrumbJump);

        if (_root != null && _root.Label == "No sessions")
        {
            EditorGUILayout.HelpBox(
                "No trace sessions yet. Run a profiled action (e.g. Rebuild Planet on PlanetBody), then pick a session from Run history.",
                MessageType.Info);
        }

        EditorGUILayout.BeginHorizontal();
        DrawHistogramPanel();
        DrawSidePanel();
        EditorGUILayout.EndHorizontal();

        if (_autoRefresh && IsLiveSelection() &&
            EditorApplication.timeSinceStartup - _lastRefresh > AutoRefreshSeconds)
        {
            _lastRefresh = EditorApplication.timeSinceStartup;
            RebuildTree();
        }
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        var newMode = (PerfTraceTreeBuilder.ViewMode)EditorGUILayout.EnumPopup("Mode", _viewMode, GUILayout.Width(220));
        if (newMode != _viewMode)
        {
            _viewMode = newMode;
            _focusStack.Clear();
            RebuildTree();
        }

        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
            RebuildTree();

        _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto-refresh (Live)", GUILayout.Width(140));

        bool autoCollect = PerfTraceBenchmarkCollector.AutoCollectEnabled;
        bool newAutoCollect = GUILayout.Toggle(autoCollect, "Auto-collect benchmark", GUILayout.Width(160));
        if (newAutoCollect != autoCollect)
            PerfTraceBenchmarkCollector.AutoCollectEnabled = newAutoCollect;

        if (GUILayout.Button("Open Memory Swizzle", GUILayout.Width(140)))
            PerfTraceMemoryCorrelator.OpenMemorySwizzle();

        GUI.enabled = !MemorySwizzleSnapshotService.IsCapturing;
        if (GUILayout.Button("Capture Correlated Memory", GUILayout.Width(180)))
            PerfTraceMemoryCorrelator.CaptureCorrelatedMemorySnapshot(_loadedSession);
        GUI.enabled = true;

        if (GUILayout.Button("Perform GC Pass", GUILayout.Width(120)))
            DiagnosticsGcPass.PerformGcPass();

        GUILayout.FlexibleSpace();
        long total = _root != null ? _root.TotalTicks : 0;
        EditorGUILayout.LabelField(PerfTraceFormat.Ms(total), EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
    }

    void DrawRunHistoryRow()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Run history", GUILayout.Width(72));
        _selectedRunIndex = EditorGUILayout.Popup(_selectedRunIndex, _runDropdownLabels.ToArray());
        if (GUILayout.Button("Delete", GUILayout.Width(56)))
        {
            if (!IsLiveSelection() && TryGetSavedRunIndex(out int savedIdx))
            {
                if (EditorUtility.DisplayDialog("Delete run", "Delete selected benchmark run?", "Delete", "Cancel"))
                {
                    PerfTraceRunHistory.DeleteRun(_runHistory[savedIdx].id);
                    _selectedRunId = null;
                    RefreshRunHistory();
                    RebuildTree();
                }
            }
        }
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("Clear history", "Delete all saved benchmark runs?", "Clear", "Cancel"))
            {
                PerfTraceRunHistory.ClearAll();
                _selectedRunId = null;
                RefreshRunHistory();
                RebuildTree();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (_selectedRunIndex != _lastAppliedRunIndex)
            ApplyRunSelection();

        string corr = PerfTraceMemoryCorrelator.FormatLastCorrelationLabel();
        if (!string.IsNullOrEmpty(corr))
            EditorGUILayout.LabelField(corr, EditorStyles.miniLabel);
    }

    void ApplyRunSelection()
    {
        _lastAppliedRunIndex = _selectedRunIndex;
        _focusStack.Clear();
        if (IsLiveSelection())
        {
            _viewMode = PerfTraceTreeBuilder.ViewMode.Live;
            _loadedSession = _liveSessions[_selectedRunIndex];
            _selectedRunId = _loadedSession?.RunId;
        }
        else if (TryGetSavedRunIndex(out int savedIdx))
        {
            _viewMode = PerfTraceTreeBuilder.ViewMode.SavedRun;
            _selectedRunId = _runHistory[savedIdx].id;
            _loadedSession = PerfTraceRunHistory.LoadSession(_selectedRunId);
        }
        else
        {
            _viewMode = PerfTraceTreeBuilder.ViewMode.Live;
            _loadedSession = null;
            _selectedRunId = null;
        }
        RebuildTree();
    }

    void RefreshRunHistory()
    {
        _liveSessions.Clear();
        PerfTrace.CopyCompletedSessions(_liveSessions);
        _liveSessions.Reverse();

        _runHistory.Clear();
        _runDropdownLabels.Clear();
        for (int i = 0; i < _liveSessions.Count; i++)
            _runDropdownLabels.Add(PerfTraceRunHistory.FormatLiveDropdownLabel(_liveSessions[i]));
        if (_liveSessions.Count == 0)
            _runDropdownLabels.Add("Live — (no sessions yet)");

        var loaded = PerfTraceRunHistory.LoadIndex();
        _runHistory.AddRange(loaded);
        for (int i = 0; i < _runHistory.Count; i++)
            _runDropdownLabels.Add(PerfTraceRunHistory.FormatDropdownLabel(_runHistory[i]));

        _selectedRunIndex = ResolveRunSelectionIndex();
        if (_selectedRunIndex >= _runDropdownLabels.Count)
            _selectedRunIndex = 0;
    }

    int ResolveRunSelectionIndex()
    {
        if (!string.IsNullOrEmpty(_selectedRunId))
        {
            for (int i = 0; i < _liveSessions.Count; i++)
            {
                var live = _liveSessions[i];
                if (live?.RunId == _selectedRunId && live.RunLabel != "SyncRenderComponents")
                    return i;
            }
            for (int i = 0; i < _runHistory.Count; i++)
            {
                if (_runHistory[i]?.id == _selectedRunId)
                    return _liveSessions.Count + i;
            }
        }
        return FindPreferredLiveSessionIndex();
    }

    static bool ShouldAutoSelectSession(PerfTraceSession session)
    {
        if (session?.Root == null)
            return false;
        if (session.RunLabel == "SyncRenderComponents")
            return false;
        return session.RunLabel == "RebuildAll"
            || session.RunLabel == "RebakeComposition"
            || session.Root.Label == "RebuildAll";
    }

    int FindPreferredLiveSessionIndex()
    {
        if (_liveSessions.Count == 0)
            return 0;

        for (int i = 0; i < _liveSessions.Count; i++)
        {
            if (_liveSessions[i]?.RunLabel == "RebuildAll")
                return i;
        }

        int best = -1;
        for (int i = 0; i < _liveSessions.Count; i++)
        {
            if (_liveSessions[i].RunLabel == "SyncRenderComponents")
                continue;
            if (best < 0 || _liveSessions[i].TotalRootMs > _liveSessions[best].TotalRootMs)
                best = i;
        }
        return best >= 0 ? best : 0;
    }

    bool IsLiveSelection() =>
        _liveSessions.Count > 0 && _selectedRunIndex < _liveSessions.Count;

    bool TryGetSavedRunIndex(out int savedIdx)
    {
        savedIdx = _selectedRunIndex - _liveSessions.Count;
        return savedIdx >= 0 && savedIdx < _runHistory.Count;
    }

    void DrawHistogramPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        Rect rect = GUILayoutUtility.GetRect(10, 10000, 200, 10000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (rect.width > 1f && rect.height > 1f &&
            (rect != _histogramArea || Event.current.type == EventType.Layout))
        {
            _histogramArea = rect;
            LayoutCurrent();
        }
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            PerfTraceHistogramPainter.DrawHistogram(GetVisibleNodes(), _hover, _selected);
        }
        HandleHistogramInput(rect);
        EditorGUILayout.EndVertical();
    }

    void HandleHistogramInput(Rect rect)
    {
        var e = Event.current;
        if (!rect.Contains(e.mousePosition))
            return;

        var visible = GetVisibleNodes();
        if (e.type == EventType.MouseMove || e.type == EventType.Repaint)
        {
            _hover = PerfTraceHistogramPainter.HitTest(visible, e.mousePosition);
            if (e.type == EventType.MouseMove)
                Repaint();
        }

        if (e.type == EventType.MouseDown && e.button == 0 && _hover != null)
        {
            _selected = _hover;
            if (_hover.Children != null && _hover.Children.Length > 0)
            {
                _focusStack.Add(_hover);
                LayoutCurrent();
            }
            e.Use();
            Repaint();
        }
    }

    void DrawSidePanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(280), GUILayout.ExpandHeight(true));
        _sideScroll = EditorGUILayout.BeginScrollView(_sideScroll);
        if (_selected != null)
            DrawSelectedDetails(_selected);
        else
            EditorGUILayout.HelpBox("Hover or click a bar.", MessageType.None);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Children", EditorStyles.boldLabel);
        var parent = GetFocusNode();
        if (parent?.Children != null)
        {
            for (int i = 0; i < parent.Children.Length; i++)
            {
                var c = parent.Children[i];
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(c.Label, EditorStyles.label))
                {
                    _selected = c;
                    if (c.Children != null && c.Children.Length > 0)
                    {
                        _focusStack.Add(c);
                        LayoutCurrent();
                    }
                    Repaint();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(PerfTraceFormat.Ms(c.TotalTicks), GUILayout.Width(72));
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawSelectedDetails(PerfTraceNode node)
    {
        EditorGUILayout.LabelField(node.Label, EditorStyles.boldLabel);
        if (!string.IsNullOrEmpty(node.Note))
            EditorGUILayout.LabelField("Note: " + node.Note, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField("Total: " + PerfTraceFormat.Ms(node.TotalTicks));
        EditorGUILayout.LabelField("Self: " + PerfTraceFormat.Ms(node.SelfTicks));
        EditorGUILayout.LabelField(PerfTraceFormat.Percent(node.PercentOfParent) + " of parent");
        EditorGUILayout.LabelField("Calls: " + node.CallCount);
        EditorGUILayout.LabelField("Grade: " + node.Grade);
        if (node.GcAllocBytes > 0)
            EditorGUILayout.LabelField("GC alloc: " + PerfTraceFormat.Bytes(node.GcAllocBytes));

        if (!string.IsNullOrEmpty(node.SourceMember))
            EditorGUILayout.LabelField(node.SourceMember, EditorStyles.miniLabel);
        if (!string.IsNullOrEmpty(node.SourceFile))
        {
            string line = node.SourceLine > 0
                ? node.SourceFile + ":" + node.SourceLine
                : node.SourceFile;
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("Ping source asset"))
                PingSourceAsset(node.SourceFile);
        }
        if (!string.IsNullOrEmpty(node.AssemblyName))
            EditorGUILayout.LabelField(node.AssemblyName, EditorStyles.miniLabel);

        if (_loadedSession != null)
        {
            EditorGUILayout.Space(4);
            if (!string.IsNullOrEmpty(_loadedSession.ScriptingBackend))
                EditorGUILayout.LabelField("Scripting: " + _loadedSession.ScriptingBackend, EditorStyles.miniLabel);
            if (_loadedSession.CpuFrameMs > 0)
                EditorGUILayout.LabelField("CPU frame: " + PerfTraceFormat.Ms(_loadedSession.CpuFrameMs), EditorStyles.miniLabel);
            if (_loadedSession.GpuFrameMs > 0)
                EditorGUILayout.LabelField("GPU frame: " + PerfTraceFormat.Ms(_loadedSession.GpuFrameMs), EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(_loadedSession.MemoryCounters))
                EditorGUILayout.LabelField(_loadedSession.MemoryCounters, EditorStyles.wordWrappedMiniLabel);
        }
    }

    static void PingSourceAsset(string sourceFile)
    {
        if (string.IsNullOrEmpty(sourceFile))
            return;
        string projectRelative = sourceFile;
        int assets = sourceFile.IndexOf("Assets", System.StringComparison.OrdinalIgnoreCase);
        if (assets >= 0)
            projectRelative = sourceFile.Substring(assets).Replace('\\', '/');
        var asset = AssetDatabase.LoadMainAssetAtPath(projectRelative);
        if (asset != null)
            EditorGUIUtility.PingObject(asset);
    }

    void RebuildTree()
    {
        switch (_viewMode)
        {
            case PerfTraceTreeBuilder.ViewMode.RoughSummary:
                _root = PerfTraceTreeBuilder.BuildRoughSummary();
                break;
            case PerfTraceTreeBuilder.ViewMode.SavedRun:
                _root = PerfTraceTreeBuilder.BuildFromSession(_loadedSession);
                break;
            default:
                if (_loadedSession?.Root != null)
                    _root = PerfTraceTreeBuilder.BuildFromSession(_loadedSession);
                else
                    _root = PerfTraceTreeBuilder.BuildLive();
                break;
        }
        _focusStack.Clear();
        LayoutCurrent();
    }

    void LayoutCurrent()
    {
        var focus = GetFocusNode();
        if (focus == null || _histogramArea.width <= 1f)
            return;

        var rows = GetHistogramRows(focus, _focusStack.Count);
        rows.Sort((a, b) => b.TotalTicks.CompareTo(a.TotalTicks));
        PerfTraceHistogramLayout.Apply(rows, _histogramArea);
    }

    static List<PerfTraceNode> GetHistogramRows(PerfTraceNode focus, int focusDepth)
    {
        var list = new List<PerfTraceNode>();
        if (focus == null)
            return list;
        if (focus.Children != null && focus.Children.Length > 0)
            list.AddRange(focus.Children);
        else if (focusDepth == 0 && focus.TotalTicks > 0)
            list.Add(focus);
        return list;
    }

    PerfTraceNode GetFocusNode()
    {
        if (_root == null)
            return null;
        if (_focusStack.Count == 0)
            return _root;
        return _focusStack[_focusStack.Count - 1];
    }

    List<PerfTraceNode> GetVisibleNodes()
    {
        var focus = GetFocusNode();
        var list = GetHistogramRows(focus, _focusStack.Count);
        list.Sort((a, b) => b.TotalTicks.CompareTo(a.TotalTicks));
        return list;
    }

    void OnBreadcrumbRoot()
    {
        _focusStack.Clear();
        LayoutCurrent();
    }

    void OnBreadcrumbBack()
    {
        if (_focusStack.Count > 0)
            _focusStack.RemoveAt(_focusStack.Count - 1);
        LayoutCurrent();
    }

    void OnBreadcrumbJump(int index)
    {
        while (_focusStack.Count > index + 1)
            _focusStack.RemoveAt(_focusStack.Count - 1);
        LayoutCurrent();
    }
}
#endif
