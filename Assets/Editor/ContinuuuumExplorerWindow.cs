#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DB-connected exploration window for Unified Semantic Archiver continuuuum.
/// Connects to SQLite continuuuum DB via Python CLI bridge; browse tables, run read-only SQL.
/// </summary>
public class ContinuuuumExplorerWindow : EditorWindow
{
    [SerializeField] private string dbPath = "";
    [SerializeField] private string pythonPath = ""; // optional: path to python.exe; empty = use PATH
    [SerializeField] private string tenantId = ""; // empty = use Scripts/continuuuum_tenant.txt or "default"
    [SerializeField] private string selectedTable = "spatial_4d";
    [SerializeField] private string customSql = "SELECT * FROM spatial_4d LIMIT 50";
    [SerializeField] private string lastError = "";
    [SerializeField] private Vector2 scroll;
    [SerializeField] private Vector2 tableScroll;
    private List<Dictionary<string, object>> tableData = new List<Dictionary<string, object>>();
    private string[] tableNames = { "spatial_4d", "document_blobs", "semantic_chunks", "unique_kernels", "compression_runs", "research_suggestions", "continuuuum_meta", "library_documents", "episodes", "episode_assets", "episode_script", "draft_episodes", "draft_episode_script", "narrative_type_detections", "causality_structure", "work_orders", "vocabulary_render_masks", "vocabulary_render_mask_buckets", "languages", "thesaurus_entries", "thesaurus_alternatives", "thesaurus_ast_nodes", "thesaurus_translations", "change_of_basis_rules", "change_of_basis_word_overrides", "script_speech_audio", "script_sound_effects", "script_audio_by_language", "dictionary_definitions", "api_audit_log", "user_presence", "localization_property_specs", "thesaurus_entry_properties", "localization_clause_bindings", "localization_change_lists", "localization_change_list_items", "localization_change_list_reviewers", "comment_topics", "reviewer_comments_archive" };
    private static readonly string PythonHint = "Set path to continuuuum.db (e.g. from continuuuum repo or any path). Requires Python on PATH with unified_semantic_archiver (USC) installed (pip install -e /path/to/unified-semantic-compressor). Uses Python CLI to query.";

    [MenuItem("Window/Continuuuum/Continuuuum Explorer")]
    public static void ShowWindow()
    {
        var w = GetWindow<ContinuuuumExplorerWindow>("Continuuuum Explorer");
        w.minSize = new Vector2(480, 400);
    }

    /// <summary>Open Explorer and run the given query. Used by Episodes and Work Orders windows.</summary>
    public static void ShowAndRunQuery(string db, string py, string tenant, string sql)
    {
        var w = GetWindow<ContinuuuumExplorerWindow>("Continuuuum Explorer");
        w.minSize = new Vector2(480, 400);
        w.dbPath = db ?? "";
        w.pythonPath = py ?? "";
        w.tenantId = tenant ?? "";
        w.customSql = sql ?? "";
        w.QuerySql(sql);
        w.Focus();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Continuuuum Explorer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(PythonHint, MessageType.Info);
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("DB Path", GUILayout.Width(60));
        dbPath = EditorGUILayout.TextField(dbPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string p = EditorUtility.OpenFilePanel("Select continuuuum.db", string.IsNullOrEmpty(dbPath) ? Application.dataPath : Path.GetDirectoryName(dbPath), "db");
            if (!string.IsNullOrEmpty(p)) dbPath = p;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Python path (optional)", GUILayout.Width(120));
        pythonPath = EditorGUILayout.TextField(pythonPath);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tenant", GUILayout.Width(60));
        tenantId = EditorGUILayout.TextField(tenantId);
        EditorGUILayout.EndHorizontal();
        if (selectedTable == "library_documents" && string.IsNullOrWhiteSpace(tenantId))
            EditorGUILayout.LabelField("Effective tenant for library_documents: " + ContinuuuumSettings.GetTenant(), EditorStyles.miniLabel);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Browse Table", EditorStyles.boldLabel);
        int tableIdx = EditorGUILayout.Popup("Table", Array.IndexOf(tableNames, selectedTable) >= 0 ? Array.IndexOf(tableNames, selectedTable) : 0, tableNames);
        selectedTable = tableNames[tableIdx];

        if (GUILayout.Button("Refresh Table"))
            QueryTable(selectedTable);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Custom SQL (read-only)", EditorStyles.boldLabel);
        customSql = EditorGUILayout.TextArea(customSql, GUILayout.Height(60));
        if (GUILayout.Button("Run SQL"))
            QuerySql(customSql);

        if (!string.IsNullOrEmpty(lastError))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(lastError, MessageType.Error);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField($"Results ({tableData.Count} rows)", EditorStyles.boldLabel);
        tableScroll = EditorGUILayout.BeginScrollView(tableScroll, GUILayout.Height(200));
        DrawTableData();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Download .vor (Variable autO Recombobulation)"))
            EditorUtility.DisplayDialog("Download .vor", "Export to .vor format will write manifest.json and audio/video/image/data/source dirs with .script, .weights, .diffs. (Stub: not yet implemented.)", "OK");

        EditorGUILayout.EndScrollView();
    }

    private void QueryTable(string table)
    {
        if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
        {
            lastError = "DB path is empty or file does not exist.";
            return;
        }
        lastError = "";
        string py = !string.IsNullOrWhiteSpace(pythonPath) ? pythonPath.Trim() : FindPython();
        if (string.IsNullOrEmpty(py))
        {
            lastError = "Python not found. Set Python path or ensure python/python3 is on PATH (with USC installed).";
            return;
        }
        string scriptDir = Path.Combine(Application.dataPath, "..", "Scripts");
        string tenant = string.IsNullOrWhiteSpace(tenantId) ? ContinuuuumSettings.GetTenant() : tenantId.Trim();
        string args = $"-m unified_semantic_archiver.cli.query_db --db \"{dbPath}\" --table {table}";
        if (table == "library_documents")
            args += $" --tenant \"{tenant.Replace("\"", "\\\"")}\"";
        RunQuery(py, args, scriptDir);
    }

    private void QuerySql(string sql)
    {
        if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
        {
            lastError = "DB path is empty or file does not exist.";
            return;
        }
        if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            lastError = "Only SELECT queries allowed (read-only).";
            return;
        }
        lastError = "";
        string py = !string.IsNullOrWhiteSpace(pythonPath) ? pythonPath.Trim() : FindPython();
        if (string.IsNullOrEmpty(py))
        {
            lastError = "Python not found. Set Python path or ensure python/python3 is on PATH (with USC installed).";
            return;
        }
        string scriptDir = Path.Combine(Application.dataPath, "..", "Scripts");
        string tmpFile = Path.Combine(Path.GetTempPath(), "continuuuum_query_" + Guid.NewGuid().ToString("N") + ".sql");
        File.WriteAllText(tmpFile, sql);
        try
        {
            string args = $"-m unified_semantic_archiver.cli.query_db --db \"{dbPath}\" --sql-file \"{tmpFile}\"";
            RunQuery(py, args, scriptDir);
        }
        finally
        {
            try { File.Delete(tmpFile); } catch { }
        }
    }

    private static string FindPython()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "--version",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var p = Process.Start(psi))
            {
                p?.WaitForExit(3000);
                if (p != null && p.ExitCode == 0) return "python";
            }
        }
        catch { }
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = "--version",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var p = Process.Start(psi))
            {
                p?.WaitForExit(3000);
                if (p != null && p.ExitCode == 0) return "python3";
            }
        }
        catch { }
        return null;
    }

    private void RunQuery(string pythonExe, string arguments, string workingDir)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = arguments,
                WorkingDirectory = Path.GetFullPath(workingDir),
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            using (var p = Process.Start(psi))
            {
                if (p == null) { lastError = "Failed to start Python."; return; }
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(10000);
                if (p.ExitCode != 0)
                {
                    lastError = stderr.Length > 0 ? stderr : stdout;
                    if (string.IsNullOrEmpty(lastError)) lastError = $"Python exited with code {p.ExitCode}.";
                    tableData.Clear();
                    return;
                }
                lastError = "";
                ParseJsonRows(stdout);
            }
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
            tableData.Clear();
        }
    }

    private void ParseJsonRows(string jsonText)
    {
        tableData.Clear();
        try
        {
            var arr = Newtonsoft.Json.Linq.JArray.Parse(jsonText);
            foreach (var tok in arr)
            {
                var obj = tok as Newtonsoft.Json.Linq.JObject;
                if (obj == null) continue;
                var d = new Dictionary<string, object>();
                foreach (var prop in obj.Properties())
                    d[prop.Name] = prop.Value?.ToString() ?? "";
                tableData.Add(d);
            }
        }
        catch (Exception ex)
        {
            lastError = "JSON parse error: " + ex.Message;
        }
    }

    private void DrawTableData()
    {
        if (tableData.Count == 0)
        {
            EditorGUILayout.LabelField("No data.");
            return;
        }
        var cols = new List<string>();
        foreach (var row in tableData)
            foreach (var k in row.Keys)
                if (!cols.Contains(k)) cols.Add(k);
        // Header
        EditorGUILayout.BeginHorizontal();
        foreach (var c in cols)
            EditorGUILayout.LabelField(c, EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        EditorGUILayout.EndHorizontal();
        foreach (var row in tableData)
        {
            EditorGUILayout.BeginHorizontal();
            foreach (var c in cols)
            {
                object v = row.TryGetValue(c, out var o) ? o : null;
                string s = v?.ToString() ?? "";
                if (s.Length > 20) s = s.Substring(0, 17) + "...";
                EditorGUILayout.LabelField(s, GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
