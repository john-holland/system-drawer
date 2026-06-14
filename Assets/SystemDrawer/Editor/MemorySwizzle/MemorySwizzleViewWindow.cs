#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>WinDirStat-style memory treemap for Mono/engine attribution.</summary>
public class MemorySwizzleViewWindow : EditorWindow
{
    MemorySwizzleViewMode _mode = MemorySwizzleViewMode.UnitySystems;
    MemorySwizzleNode _root;
    readonly List<MemorySwizzleNode> _focusStack = new List<MemorySwizzleNode>();
    MemorySwizzleNode _hover;
    MemorySwizzleNode _selected;
    List<MemorySwizzleObjectRecord> _records = new List<MemorySwizzleObjectRecord>();
    string _snapshotPath;
    bool _autoRefresh;
    double _lastRefresh;
    const double AutoRefreshSeconds = 2.0;
    bool _registeredEntitiesOnly;
    Vector2 _sideScroll;
    Rect _treemapArea;

    [MenuItem("Window/System Drawer/Diagnostics/Memory Swizzle View", false, 50)]
    public static void Open()
    {
        OpenMemorySwizzle();
    }

    public static void OpenMemorySwizzle()
    {
        var w = GetWindow<MemorySwizzleViewWindow>("Diagnostics — Memory");
        w.minSize = new Vector2(720, 480);
        w.Show();
    }

    void OnEnable()
    {
        MemorySwizzleSnapshotService.CaptureFinished += OnCaptureFinished;
        _snapshotPath = MemorySwizzleSnapshotService.LastSnapshotPath;
        RebuildTree();
    }

    void OnDisable()
    {
        MemorySwizzleSnapshotService.CaptureFinished -= OnCaptureFinished;
    }

    void OnCaptureFinished(bool success, string path)
    {
        if (success && !string.IsNullOrEmpty(path))
            _snapshotPath = path;
        _records = MemorySwizzleSnapshotReader.LoadOrScan(_snapshotPath);
        RebuildTree();
        Repaint();
    }

    void OnGUI()
    {
        DrawToolbar();
        DrawBreadcrumb();
        if (!EditorApplication.isPlaying && MemorySwizzleTreeBuilderRegistry.RequiresSnapshot(_mode))
        {
            EditorGUILayout.HelpBox(
                "Unity Systems mode works in Edit Mode. Object attribution modes are most accurate after a Play Mode snapshot.",
                MessageType.Info);
        }

        EditorGUILayout.BeginHorizontal();
        DrawTreemapPanel();
        DrawSidePanel();
        EditorGUILayout.EndHorizontal();

        if (_autoRefresh && _mode == MemorySwizzleViewMode.UnitySystems &&
            EditorApplication.timeSinceStartup - _lastRefresh > AutoRefreshSeconds)
        {
            _lastRefresh = EditorApplication.timeSinceStartup;
            RebuildTree();
        }
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        var newMode = (MemorySwizzleViewMode)EditorGUILayout.EnumPopup("Mode", _mode, GUILayout.Width(280));
        if (newMode != _mode)
        {
            _mode = newMode;
            _focusStack.Clear();
            RebuildTree();
        }

        GUI.enabled = !MemorySwizzleSnapshotService.IsCapturing;
        if (GUILayout.Button("Capture Snapshot", GUILayout.Width(130)))
            MemorySwizzleSnapshotService.CaptureAsync();
        GUI.enabled = true;

        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
            RefreshData();

        EditorGUILayout.LabelField("|", GUILayout.Width(10));
        if (GUILayout.Button("Perf Trace View", GUILayout.Width(110)))
            DiagnosticsWindowLauncher.TryOpenPerfTrace();

        string perfCorr = ReadPerfTraceCorrelationLabel();
        if (!string.IsNullOrEmpty(perfCorr))
            EditorGUILayout.LabelField(perfCorr, EditorStyles.miniLabel, GUILayout.Width(120));

        _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto-refresh (Systems)", GUILayout.Width(160));
        if (_mode == MemorySwizzleViewMode.EntityTotals)
            _registeredEntitiesOnly = GUILayout.Toggle(_registeredEntitiesOnly, "Registered only", GUILayout.Width(120));

        GUILayout.FlexibleSpace();
        long total = _root != null ? _root.SizeBytes : 0;
        EditorGUILayout.LabelField(MemorySwizzleFormat.Bytes(total), EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
    }

    void DrawBreadcrumb()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Root", EditorStyles.miniButton, GUILayout.Width(50)))
        {
            _focusStack.Clear();
            LayoutCurrent();
        }
        for (int i = 0; i < _focusStack.Count; i++)
        {
            EditorGUILayout.LabelField("›", GUILayout.Width(12));
            var node = _focusStack[i];
            if (GUILayout.Button(node.Label, EditorStyles.miniButton))
            {
                while (_focusStack.Count > i + 1)
                    _focusStack.RemoveAt(_focusStack.Count - 1);
                LayoutCurrent();
            }
        }
        if (_focusStack.Count > 0 && GUILayout.Button("Back", EditorStyles.miniButton, GUILayout.Width(50)))
        {
            _focusStack.RemoveAt(_focusStack.Count - 1);
            LayoutCurrent();
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawTreemapPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        Rect rect = GUILayoutUtility.GetRect(10, 10000, 200, 10000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (rect.width > 1f && rect.height > 1f &&
            (rect != _treemapArea || Event.current.type == EventType.Layout))
        {
            _treemapArea = rect;
            LayoutCurrent();
        }
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            var visible = GetVisibleNodes();
            MemorySwizzleTreemapPainter.DrawTreemap(visible, _hover, _selected);
        }

        HandleTreemapInput(rect);
        EditorGUILayout.EndVertical();
    }

    void HandleTreemapInput(Rect rect)
    {
        var e = Event.current;
        if (!rect.Contains(e.mousePosition))
            return;

        var visible = GetVisibleNodes();
        if (e.type == EventType.MouseMove || e.type == EventType.Repaint)
        {
            _hover = MemorySwizzleTreemapPainter.HitTest(visible, e.mousePosition);
            if (e.type == EventType.MouseMove)
                Repaint();
        }

        if (e.type == EventType.MouseDown && e.button == 0 && _hover != null)
        {
            _selected = _hover;
            if (_hover.Children.Count > 0)
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
        EditorGUILayout.BeginVertical(GUILayout.Width(260), GUILayout.ExpandHeight(true));
        _sideScroll = EditorGUILayout.BeginScrollView(_sideScroll);
        if (_selected != null)
        {
            EditorGUILayout.LabelField(_selected.Label, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(MemorySwizzleFormat.Bytes(_selected.SizeBytes));
            EditorGUILayout.LabelField(MemorySwizzleFormat.Percent(_selected.PercentOfParent) + " of parent");
            if (!string.IsNullOrEmpty(_selected.Path))
                EditorGUILayout.LabelField(_selected.Path, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(6);
            if (_selected.InstanceId != 0 && GUILayout.Button("Ping"))
            {
                var obj = EditorUtility.EntityIdToObject(_selected.InstanceId);
                if (obj != null)
                    EditorGUIUtility.PingObject(obj);
            }
            if (GUILayout.Button("Copy TSV (children)"))
                CopyChildrenTsv(_selected);
        }
        else
            EditorGUILayout.HelpBox("Hover or click a tile.", MessageType.None);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Children", EditorStyles.boldLabel);
        var parent = GetFocusNode();
        if (parent != null)
        {
            for (int i = 0; i < parent.Children.Count; i++)
            {
                var c = parent.Children[i];
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(c.Label, EditorStyles.label))
                {
                    _selected = c;
                    if (c.Children.Count > 0)
                    {
                        _focusStack.Add(c);
                        LayoutCurrent();
                    }
                    Repaint();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(MemorySwizzleFormat.Bytes(c.SizeBytes), GUILayout.Width(72));
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void RefreshData()
    {
        if (MemorySwizzleTreeBuilderRegistry.RequiresSnapshot(_mode))
            _records = MemorySwizzleSnapshotReader.LoadOrScan(_snapshotPath);
        RebuildTree();
    }

    void RebuildTree()
    {
        var recordsForBuild = _mode == MemorySwizzleViewMode.UnitySystems
            ? new List<MemorySwizzleObjectRecord>()
            : _records;

        var ctx = new MemorySwizzleBuildContext
        {
            Mode = _mode,
            Records = recordsForBuild,
            RegisteredEntitiesOnly = _registeredEntitiesOnly,
            InstanceIdToRegistryKey = MemorySwizzleRegistryLookup.BuildInstanceIdToKeyMap()
        };

        var builder = MemorySwizzleTreeBuilderRegistry.Get(_mode);
        _root = builder.Build(ctx);
        _focusStack.Clear();
        LayoutCurrent();
    }

    void LayoutCurrent()
    {
        var focus = GetFocusNode();
        if (focus == null || _treemapArea.width <= 1f || _treemapArea.height <= 1f)
            return;
        SquarifiedTreemapLayout.ApplyFlat(focus.Children, _treemapArea);
    }

    MemorySwizzleNode GetFocusNode()
    {
        if (_root == null)
            return null;
        if (_focusStack.Count == 0)
            return _root;
        return _focusStack[_focusStack.Count - 1];
    }

    List<MemorySwizzleNode> GetVisibleNodes()
    {
        var focus = GetFocusNode();
        var list = new List<MemorySwizzleNode>();
        if (focus == null)
            return list;
        if (_focusStack.Count == 0)
            list.AddRange(focus.Children);
        else
            list.AddRange(focus.Children);
        return list;
    }

    static string ReadPerfTraceCorrelationLabel()
    {
        string utc = EditorPrefs.GetString("PerfTrace.LastCorrelationUtc", "");
        if (string.IsNullOrEmpty(utc))
            return "";
        if (!System.DateTime.TryParse(utc, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            return "";
        return "Perf @ " + parsed.ToLocalTime().ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
    }

    static void CopyChildrenTsv(MemorySwizzleNode node)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Label\tBytes\tPercent");
        long total = node.SizeBytes > 0 ? node.SizeBytes : 1;
        for (int i = 0; i < node.Children.Count; i++)
        {
            var c = node.Children[i];
            sb.Append(c.Label).Append('\t').Append(c.SizeBytes).Append('\t')
                .Append(((float)c.SizeBytes / total).ToString("0.####")).AppendLine();
        }
        EditorGUIUtility.systemCopyBuffer = sb.ToString();
    }
}
#endif
