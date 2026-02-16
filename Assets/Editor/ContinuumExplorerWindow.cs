#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DB-connected exploration window for Unified Semantic Archiver continuum.
/// Connects to SQLite continuum DB via Python CLI bridge; browse tables, run read-only SQL.
/// </summary>
public class ContinuumExplorerWindow : EditorWindow
{
    [SerializeField] private string dbPath = "";
    [SerializeField] private string selectedTable = "spatial_4d";
    [SerializeField] private string customSql = "SELECT * FROM spatial_4d LIMIT 50";
    [SerializeField] private string lastError = "";
    [SerializeField] private Vector2 scroll;
    [SerializeField] private Vector2 tableScroll;
    private List<Dictionary<string, object>> tableData = new List<Dictionary<string, object>>();
    private string[] tableNames = { "spatial_4d", "document_blobs", "semantic_chunks", "unique_kernels", "compression_runs", "research_suggestions", "continuum_meta", "library_documents" };
    private static readonly string PythonHint = "Set path to continuum.db (e.g. Scripts/unified_semantic_archiver/continuum.db). Uses Python to query.";

    [MenuItem("Window/Continuum/Continuum Explorer")]
    public static void ShowWindow()
    {
        var w = GetWindow<ContinuumExplorerWindow>("Continuum Explorer");
        w.minSize = new Vector2(480, 400);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Continuum Explorer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(PythonHint, MessageType.Info);
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
        string py = FindPython();
        if (string.IsNullOrEmpty(py))
        {
            lastError = "Python not found on PATH.";
            return;
        }
        string scriptDir = Path.Combine(Application.dataPath, "..", "Scripts");
        string args = $"-m unified_semantic_archiver.cli.query_db --db \"{dbPath}\" --table {table}";
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
        string py = FindPython();
        if (string.IsNullOrEmpty(py))
        {
            lastError = "Python not found on PATH.";
            return;
        }
        string scriptDir = Path.Combine(Application.dataPath, "..", "Scripts");
        string tmpFile = Path.Combine(Path.GetTempPath(), "continuum_query_" + Guid.NewGuid().ToString("N") + ".sql");
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
