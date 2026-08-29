using UnityEditor;
using UnityEngine;

public sealed class GameSessionWindow : EditorWindow
{
    GameSessionHost _host;
    Vector2 _scroll;
    int _tab;
    string _filter = "";

    [MenuItem("Window/System Drawer/Networking/Game Sessions")]
    public static void Open()
    {
        var w = GetWindow<GameSessionWindow>("Game Sessions");
        w.minSize = new Vector2(420, 360);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _host = (GameSessionHost)EditorGUILayout.ObjectField("Host", _host, typeof(GameSessionHost), true);
        if (_host == null)
        {
            EditorGUILayout.HelpBox("Assign a GameSessionHost (on ServerOrchestrator).", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }
        _tab = GUILayout.Toolbar(_tab, new[] { "List", "Graph" });
        _filter = EditorGUILayout.TextField("Filter", _filter);
        EditorGUILayout.LabelField("Lobby", _host.lobbySessionName);
        EditorGUILayout.LabelField("Active index", _host.activeIndex.ToString());
        if (GUILayout.Button("Create session"))
            _host.CreateSession("Session " + (_host.sessions != null ? _host.sessions.Count + 1 : 1));
        if (_tab == 1)
            DrawGraph();
        else
            DrawList();
        EditorGUILayout.EndScrollView();
    }

    bool PassesFilter(GameSession s)
    {
        if (s == null) return false;
        if (string.IsNullOrWhiteSpace(_filter)) return true;
        string q = _filter.Trim();
        return (s.id != null && s.id.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0)
            || (s.displayName != null && s.displayName.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0)
            || (s.parentId != null && s.parentId.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    void DrawList()
    {
        if (_host.sessions == null) return;
        for (int i = 0; i < _host.sessions.Count; i++)
        {
            var s = _host.sessions[i];
            if (!PassesFilter(s)) continue;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(s.displayName, s.id);
            EditorGUILayout.LabelField("Parent", string.IsNullOrEmpty(s.parentId) ? "root" : s.parentId);
            EditorGUILayout.LabelField("Pecking", s.peckingOrder.ToString());
            EditorGUILayout.Toggle("Active", s.active);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Switch"))
                _host.SwitchActive(i);
            if (GUILayout.Button("Save to Local Client"))
                _host.SaveToLocalClient(s.id);
            if (GUILayout.Button("Close"))
                _host.CloseSession(s.id, GameSessionCloseMode.AdoptToHigher);
            if (GUILayout.Button("Umbrella"))
                _host.CloseSession(s.id, GameSessionCloseMode.Umbrella);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }

    void DrawGraph()
    {
        if (_host.sessions == null) return;
        EditorGUILayout.LabelField("parentId tree (lower pecking = higher rank)", EditorStyles.miniLabel);
        for (int i = 0; i < _host.sessions.Count; i++)
        {
            var s = _host.sessions[i];
            if (!PassesFilter(s)) continue;
            if (!string.IsNullOrEmpty(s.parentId)) continue;
            DrawNode(s, 0);
        }
    }

    void DrawNode(GameSession s, int depth)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(depth * 16);
        if (GUILayout.Button((s.active ? "* " : "") + s.displayName + " [" + s.peckingOrder + "]", EditorStyles.linkLabel))
            _host.SwitchActiveById(s.id);
        EditorGUILayout.EndHorizontal();
        var kids = new System.Collections.Generic.List<GameSession>();
        for (int i = 0; i < _host.sessions.Count; i++)
        {
            var c = _host.sessions[i];
            if (c != null && c.parentId == s.id)
                kids.Add(c);
        }
        kids.Sort((a, b) => a.peckingOrder.CompareTo(b.peckingOrder));
        for (int i = 0; i < kids.Count; i++)
            DrawNode(kids[i], depth + 1);
    }
}
