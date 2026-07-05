#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Continuuuum.Library;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Continuuuum Library window: search by lat/long or address and distance (no map).
/// Uses Cave server API when base URL is set; otherwise falls back to Python CLI.
/// </summary>
public class ContinuuuumLibraryWindow : EditorWindow
{
    [SerializeField] private string caveBaseUrl = "http://localhost:3000";
    [SerializeField] private string dbPath = "";
    [SerializeField] private string pythonPath = ""; // optional: for CLI fallback; empty = use PATH
    [SerializeField] private string tenantId = ""; // empty = use Scripts/continuuuum_tenant.txt or "default"
    [SerializeField] private string searchLat = "";
    [SerializeField] private string searchLon = "";
    [SerializeField] private string searchAddress = "";
    [SerializeField] private int distanceIndex = 0; // 0=Infinite, 1=0 (same bucket), then 10,100,500,...,24000
    [SerializeField] private int documentTypeIndex = 0; // 0=All, then video, document, audio, image, program, data
    [SerializeField] private string searchQuery = "";
    [SerializeField] private string lastError = "";
    [SerializeField] private Vector2 scroll;
    [SerializeField] private Vector2 listScroll;
    [SerializeField] private int selectedIndex = -1;
    private readonly List<ContinuuuumLibraryDocument> results = new List<ContinuuuumLibraryDocument>();

    [MenuItem("Window/Continuuuum/Continuuuum Library")]
    public static void ShowWindow()
    {
        var w = GetWindow<ContinuuuumLibraryWindow>("Continuuuum Library");
        w.minSize = new Vector2(520, 420);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Continuuuum Library", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Search by location (lat/lon or address) and distance. Set Base URL to Continuuuum Library server (e.g. http://localhost:5050) or leave empty to use Python CLI with DB path.", MessageType.Info);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Backend", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Base URL", GUILayout.Width(100));
        caveBaseUrl = EditorGUILayout.TextField(caveBaseUrl);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tenant", GUILayout.Width(100));
        tenantId = EditorGUILayout.TextField(tenantId);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Python path (optional)", GUILayout.Width(100));
        pythonPath = EditorGUILayout.TextField(pythonPath);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("DB Path (fallback)", GUILayout.Width(100));
        dbPath = EditorGUILayout.TextField(dbPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string p = EditorUtility.OpenFilePanel("Select continuuuum.db", string.IsNullOrEmpty(dbPath) ? Application.dataPath : Path.GetDirectoryName(dbPath), "db");
            if (!string.IsNullOrEmpty(p)) dbPath = p;
        }
        EditorGUILayout.EndHorizontal();

        string effectiveTenant = string.IsNullOrWhiteSpace(tenantId) ? ContinuuuumSettings.GetTenant() : tenantId.Trim();
        if (string.IsNullOrEmpty(tenantId))
            EditorGUILayout.LabelField("Effective tenant: " + effectiveTenant + " (from file or default)", EditorStyles.miniLabel);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Location search", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Lat", GUILayout.Width(28));
        searchLat = EditorGUILayout.TextField(searchLat);
        EditorGUILayout.LabelField("Lon", GUILayout.Width(28));
        searchLon = EditorGUILayout.TextField(searchLon);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Address", GUILayout.Width(60));
        searchAddress = EditorGUILayout.TextField(searchAddress);
        if (GUILayout.Button("Geocode", GUILayout.Width(60)))
            GeocodeAddress();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Distance", GUILayout.Width(60));
        distanceIndex = EditorGUILayout.Popup(distanceIndex, ContinuuuumLibraryQuery.DistanceOptionLabels);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Type", GUILayout.Width(60));
        documentTypeIndex = EditorGUILayout.Popup(documentTypeIndex, ContinuuuumLibraryQuery.DocumentTypes);
        EditorGUILayout.LabelField("Text", GUILayout.Width(32));
        searchQuery = EditorGUILayout.TextField(searchQuery);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Search"))
            DoSearch();

        if (!string.IsNullOrEmpty(lastError))
            EditorGUILayout.HelpBox(lastError, MessageType.Error);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField($"Results ({results.Count})", EditorStyles.boldLabel);
        listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.Height(180));
        for (int i = 0; i < results.Count; i++)
        {
            var doc = results[i];
            bool sel = i == selectedIndex;
            string label = $"#{doc.id} {doc.document_type}";
            var title = ContinuuuumLibraryJson.TryGetDisplayTitle(doc);
            if (!string.IsNullOrEmpty(title))
                label = title + " (" + doc.document_type + ")";
            if (GUILayout.Toggle(sel, label, EditorStyles.foldoutHeader) && !sel)
            {
                selectedIndex = i;
                Repaint();
            }
        }
        EditorGUILayout.EndScrollView();

        if (selectedIndex >= 0 && selectedIndex < results.Count)
        {
            var doc = results[selectedIndex];
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Metadata", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Type: " + doc.document_type);
            if (doc.lat.HasValue && doc.lon.HasValue)
                EditorGUILayout.LabelField($"Location: {doc.lat.Value:F4}, {doc.lon.Value:F4}");
            if (!string.IsNullOrEmpty(doc.type_metadata))
                EditorGUILayout.TextArea(doc.type_metadata, GUILayout.Height(60));
            if (GUILayout.Button("Download"))
                DownloadDocument(doc);
        }

        EditorGUILayout.EndScrollView();
    }

    private string GetEffectiveTenant()
    {
        return string.IsNullOrWhiteSpace(tenantId) ? ContinuuuumSettings.GetTenant() : tenantId.Trim();
    }

    private void GeocodeAddress()
    {
        if (string.IsNullOrWhiteSpace(searchAddress)) { lastError = "Enter an address."; return; }
        if (string.IsNullOrWhiteSpace(caveBaseUrl)) { lastError = "Set Cave Base URL to use geocode."; return; }
        lastError = "";
        try
        {
            var url = ContinuuuumLibraryHttp.BuildGeocodeUrl(caveBaseUrl.TrimEnd('/'), searchAddress.Trim());
            if (!ContinuuuumLibraryHttp.TryGetJsonSync(url, GetEffectiveTenant(), null, out var json, out var err))
            {
                lastError = err;
                return;
            }

            if (!ContinuuuumLibraryJson.TryParseGeocode(json, out var latStr, out var lonStr))
            {
                lastError = json;
                return;
            }

            if (!string.IsNullOrEmpty(latStr))
                searchLat = latStr;
            if (!string.IsNullOrEmpty(lonStr))
                searchLon = lonStr;
        }
        catch (Exception ex) { lastError = ex.Message; }
    }

    private void DoSearch()
    {
        lastError = "";
        if (!string.IsNullOrWhiteSpace(caveBaseUrl))
            SearchViaCave();
        else if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
            SearchViaPython();
        else
            lastError = "Set Cave Base URL or DB path.";
    }

    private void SearchViaCave()
    {
        try
        {
            var url = ContinuuuumLibraryHttp.BuildSearchUrl(
                caveBaseUrl.TrimEnd('/'),
                searchQuery,
                documentTypeIndex,
                searchLat,
                searchLon,
                distanceIndex);

            if (!ContinuuuumLibraryHttp.TryGetJsonSync(url, GetEffectiveTenant(), null, out var json, out var err))
            {
                lastError = err;
                results.Clear();
                return;
            }

            results.Clear();
            selectedIndex = -1;
            results.AddRange(ContinuuuumLibraryJson.ParseSearchResults(json));
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
            results.Clear();
        }
    }

    private void SearchViaPython()
    {
        string py = !string.IsNullOrWhiteSpace(pythonPath) ? pythonPath.Trim() : FindPython();
        if (string.IsNullOrEmpty(py)) { lastError = "Python not found. Set Python path or ensure python is on PATH (with USC installed)."; return; }
        string scriptDir = Path.Combine(Application.dataPath, "..", "Scripts");
        var sb = new StringBuilder();
        string tenant = GetEffectiveTenant();
        sb.Append("-m unified_semantic_archiver.cli.query_db --db \"").Append(dbPath.Replace("\"", "\\\"")).Append("\" --table library_documents --tenant \"").Append(tenant.Replace("\"", "\\\"")).Append("\"");
        if (!string.IsNullOrWhiteSpace(searchQuery)) sb.Append(" -q \"").Append(searchQuery.Replace("\"", "\\\"")).Append("\"");
        if (documentTypeIndex > 0) sb.Append(" --document_type ").Append(ContinuuuumLibraryQuery.DocumentTypes[documentTypeIndex]);
        if (!string.IsNullOrWhiteSpace(searchLat)) sb.Append(" --lat ").Append(searchLat);
        if (!string.IsNullOrWhiteSpace(searchLon)) sb.Append(" --lon ").Append(searchLon);
        if (distanceIndex < ContinuuuumLibraryQuery.DistanceMiles.Length)
        {
            sb.Append(" --distance_mi ");
            if (distanceIndex == 0) sb.Append("infinite");
            else if (ContinuuuumLibraryQuery.DistanceMiles[distanceIndex] == 0) sb.Append("0");
            else sb.Append(ContinuuuumLibraryQuery.DistanceMiles[distanceIndex].ToString("0"));
        }
        string argsStr = sb.ToString();
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = py,
                Arguments = argsStr,
                WorkingDirectory = Path.GetFullPath(scriptDir),
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            using (var p = System.Diagnostics.Process.Start(psi))
            {
                if (p == null) { lastError = "Failed to start Python."; return; }
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(10000);
                if (p.ExitCode != 0) { lastError = stderr.Length > 0 ? stderr : stdout; results.Clear(); return; }
                results.Clear();
                selectedIndex = -1;
                results.AddRange(ContinuuuumLibraryJson.ParseSearchResults(stdout));
            }
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
            results.Clear();
        }
    }

    private void DownloadDocument(ContinuuuumLibraryDocument doc)
    {
        if (!string.IsNullOrEmpty(doc.url))
        {
            Application.OpenURL(doc.url);
            return;
        }
        if (string.IsNullOrWhiteSpace(caveBaseUrl)) { lastError = "Set Cave Base URL to download."; return; }
        var url = ContinuuuumLibraryHttp.BuildDownloadUrl(caveBaseUrl.TrimEnd('/'), GetEffectiveTenant(), doc.id);
        Application.OpenURL(url);
    }

    private static string FindPython()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python",
                Arguments = "--version",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var p = System.Diagnostics.Process.Start(psi))
            {
                p?.WaitForExit(3000);
                if (p != null && p.ExitCode == 0) return "python";
            }
        }
        catch { }
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python3",
                Arguments = "--version",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var p = System.Diagnostics.Process.Start(psi))
            {
                p?.WaitForExit(3000);
                if (p != null && p.ExitCode == 0) return "python3";
            }
        }
        catch { }
        return null;
    }
}
#endif
