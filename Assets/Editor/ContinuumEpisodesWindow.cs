#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Episodes browser and editor for continuum DB. Create episodes, bind USC assets, scene path, and engine.
/// Requires episodes schema applied (Scripts/continuum_episodes_schema.sql).
/// </summary>
public class ContinuumEpisodesWindow : EditorWindow
{
    [SerializeField] private string dbPath = "";
    [SerializeField] private string pythonPath = "";
    [SerializeField] private string tenantId = "";
    [SerializeField] private string lastError = "";
    [SerializeField] private Vector2 scroll;

    [MenuItem("Window/Continuum/Continuum Episodes")]
    public static void ShowWindow()
    {
        var w = GetWindow<ContinuumEpisodesWindow>("Continuum Episodes");
        w.minSize = new Vector2(420, 320);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Continuum Episodes", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Browse and manage episodes. Bind USC assets, Unity/Unreal scene path, and engine. Apply Scripts/continuum_episodes_schema.sql to continuum.db first.", MessageType.Info);
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("DB Path", GUILayout.Width(60));
        dbPath = EditorGUILayout.TextField(dbPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string p = EditorUtility.OpenFilePanel("Select continuum.db", string.IsNullOrEmpty(dbPath) ? Application.dataPath : Path.GetDirectoryName(dbPath), "db");
            if (!string.IsNullOrEmpty(p)) dbPath = p;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Python path", GUILayout.Width(80));
        pythonPath = EditorGUILayout.TextField(pythonPath);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tenant", GUILayout.Width(60));
        tenantId = EditorGUILayout.TextField(tenantId);
        EditorGUILayout.LabelField(EditorGUIUtility.IconContent("console.infoicon"), GUILayout.Width(18));
        EditorGUILayout.EndHorizontal();
        if (string.IsNullOrWhiteSpace(tenantId))
            EditorGUILayout.LabelField("Effective tenant: " + ContinuumSettings.GetTenant(), EditorStyles.miniLabel);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Episodes", EditorStyles.boldLabel);
        if (GUILayout.Button("Browse Episodes (SELECT * FROM episodes)"))
            OpenExplorerWithQuery("SELECT * FROM episodes LIMIT 100");
        if (GUILayout.Button("Browse Episode Assets"))
            OpenExplorerWithQuery("SELECT * FROM episode_assets LIMIT 100");

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Schema", EditorStyles.boldLabel);
        string schemaPath = Path.Combine(Application.dataPath, "..", "Scripts", "continuum_episodes_schema.sql");
        EditorGUILayout.LabelField("Schema file: Scripts/continuum_episodes_schema.sql", EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Open Schema File") && File.Exists(schemaPath))
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(schemaPath, 1);
        else if (!File.Exists(schemaPath))
            EditorGUILayout.HelpBox("Schema file not found. Create Scripts/continuum_episodes_schema.sql.", MessageType.Warning);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Create Episode (requires USC write API)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Episode creation requires continuum/USC write API. Use USC migrations or apply schema + insert via SQLite.", MessageType.None);
        if (GUILayout.Button("New Episode (stub)"))
            EditorUtility.DisplayDialog("New Episode", "Episode creation will be implemented via USC API or direct SQLite writes. For now, apply schema and insert manually.", "OK");

        if (!string.IsNullOrEmpty(lastError))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(lastError, MessageType.Error);
        }
        EditorGUILayout.EndScrollView();
    }

    private void OpenExplorerWithQuery(string sql)
    {
        ContinuumExplorerWindow.ShowAndRunQuery(dbPath, pythonPath, tenantId, sql);
    }
}
#endif
