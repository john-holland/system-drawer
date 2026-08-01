#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>Window → System Drawer → Stations — list hierarchy + upload level stats to Continuuuum.</summary>
public sealed class StationsEditorWindow : EditorWindow
{
    Vector2 _scroll;
    StationKind _filter = StationKind.Generic;
    bool _filterAll = true;
    string _cityId = "demo-city";
    string _levelId = "default";
    string _status = "";
    List<StationHierarchyNode> _cached = new List<StationHierarchyNode>();

    [MenuItem("Window/System Drawer/Stations", false, 235)]
    public static void ShowWindow() => GetWindow<StationsEditorWindow>("Stations");

    void OnEnable() => Refresh();

    void Refresh()
    {
        _cached.Clear();
        var found = UnityEngine.Object.FindObjectsByType<StationHierarchyNode>(FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
        {
            if (!_filterAll && found[i].kind != _filter) continue;
            _cached.Add(found[i]);
        }
        _cached.Sort((a, b) => string.CompareOrdinal(a.stableId, b.stableId));
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Station Hierarchy", EditorStyles.boldLabel);
        _cityId = EditorGUILayout.TextField("City id", _cityId);
        _levelId = EditorGUILayout.TextField("Level id", _levelId);
        EditorGUILayout.BeginHorizontal();
        _filterAll = EditorGUILayout.ToggleLeft("All kinds", _filterAll, GUILayout.Width(90));
        using (new EditorGUI.DisabledScope(_filterAll))
            _filter = (StationKind)EditorGUILayout.EnumPopup(_filter);
        if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            Refresh();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.Width(120));
        EditorGUILayout.LabelField("Kind", EditorStyles.boldLabel, GUILayout.Width(70));
        EditorGUILayout.LabelField("Building", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField("Vehicle", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField("Leaf", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField("Cmdty", EditorStyles.boldLabel, GUILayout.Width(40));
        EditorGUILayout.LabelField("Wt", EditorStyles.boldLabel, GUILayout.Width(36));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < _cached.Count; i++)
        {
            var n = _cached[i];
            if (n == null) continue;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(n.displayName ?? n.name, EditorStyles.linkLabel, GUILayout.Width(120)))
                Selection.activeGameObject = n.gameObject;
            EditorGUILayout.LabelField(n.kind.ToString(), GUILayout.Width(70));
            EditorGUILayout.LabelField(n.config?.buildingStableId ?? "", GUILayout.Width(100));
            EditorGUILayout.LabelField(n.config?.vehicleId ?? "", GUILayout.Width(80));
            EditorGUILayout.LabelField(n.causalityLeafId ?? "", GUILayout.Width(100));
            int cmd = n.config?.commodities?.Count ?? 0;
            EditorGUILayout.LabelField(cmd.ToString(), GUILayout.Width(40));
            EditorGUILayout.LabelField((n.config?.staffingWeight ?? 1f).ToString("0.##"), GUILayout.Width(36));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Upload level stats to Continuuuum"))
            _ = UploadAsync();
        if (GUILayout.Button("Pull placards from Continuuuum"))
            _ = PullAsync();

        if (!string.IsNullOrEmpty(_status))
            EditorGUILayout.HelpBox(_status, MessageType.Info);
    }

    async Task UploadAsync()
    {
        _status = "Uploading…";
        Repaint();
        var registry = UnityEngine.Object.FindFirstObjectByType<StationRegistry>();
        if (registry == null)
        {
            var go = new GameObject("StationRegistry");
            registry = go.AddComponent<StationRegistry>();
            Undo.RegisterCreatedObjectUndo(go, "StationRegistry");
        }
        registry.defaultCityId = _cityId;
        registry.defaultLevelId = _levelId;
        registry.RefreshFromScene();
        // Bind leaf ids if orchestrator present
        var orch = UnityEngine.Object.FindFirstObjectByType<SpatialGenerator4DOrchestrator>();
        orch?.EnumerateStationHierarchy(true);

        string json = BuildUploadJson(registry);
        var result = await ContinuuuumEditorApiClient.RequestAsync("PUT", "/api/stations/level-stats", json);
        _status = result.success
            ? $"Uploaded {_cached.Count} stations → {_cityId}/{_levelId}"
            : $"Upload failed: {result.error}";
        Repaint();
    }

    async Task PullAsync()
    {
        _status = "Pulling…";
        Repaint();
        var result = await ContinuuuumEditorApiClient.RequestAsync(
            "GET",
            $"/api/stations?cityId={UnityEngine.Networking.UnityWebRequest.EscapeURL(_cityId)}");
        _status = result.success
            ? $"Pulled placards ({result.json?.Length ?? 0} bytes). Apply to scene nodes manually or via StationRegistry."
            : $"Pull failed: {result.error}";
        Repaint();
    }

    static string BuildUploadJson(StationRegistry registry)
    {
        var body = registry.BuildUploadBody();
        // Lightweight dictionary JSON (no Newtonsoft dependency in this assembly path)
        return DictToJson(body);
    }

    static string DictToJson(object obj)
    {
        var sb = new StringBuilder();
        WriteValue(sb, obj);
        return sb.ToString();
    }

    static void WriteValue(StringBuilder sb, object obj)
    {
        if (obj == null)
        {
            sb.Append("null");
            return;
        }
        switch (obj)
        {
            case string s:
                sb.Append('"').Append(Escape(s)).Append('"');
                break;
            case bool b:
                sb.Append(b ? "true" : "false");
                break;
            case int or long or float or double or decimal:
                sb.Append(Convert.ToString(obj, System.Globalization.CultureInfo.InvariantCulture));
                break;
            case System.Collections.IDictionary dict:
                sb.Append('{');
                bool first = true;
                foreach (System.Collections.DictionaryEntry e in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append('"').Append(Escape(e.Key?.ToString() ?? "")).Append("\":");
                    WriteValue(sb, e.Value);
                }
                sb.Append('}');
                break;
            case System.Collections.IEnumerable list when obj is not string:
                sb.Append('[');
                bool firstI = true;
                foreach (var item in list)
                {
                    if (!firstI) sb.Append(',');
                    firstI = false;
                    WriteValue(sb, item);
                }
                sb.Append(']');
                break;
            default:
                sb.Append('"').Append(Escape(obj.ToString())).Append('"');
                break;
        }
    }

    static string Escape(string s) =>
        (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
}
#endif
