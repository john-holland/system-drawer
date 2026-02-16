#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Continuum Library window: search by lat/long or address and distance (no map).
/// Uses Cave server API when base URL is set; otherwise falls back to Python CLI.
/// </summary>
public class ContinuumLibraryWindow : EditorWindow
{
    [SerializeField] private string caveBaseUrl = "http://localhost:3000";
    [SerializeField] private string dbPath = "";
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
    private List<LibraryDocument> results = new List<LibraryDocument>();
    private static readonly string[] DistanceOptions = { "Infinite", "0 (same bucket)", "10 mi", "100 mi", "500 mi", "1000 mi", "5000 mi", "24000 mi" };
    private static readonly float[] DistanceMiles = { -1f, 0f, 10f, 100f, 500f, 1000f, 5000f, 24000f };
    private static readonly string[] DocumentTypes = { "All", "video", "document", "audio", "image", "program", "data" };

    private class LibraryDocument
    {
        public int id;
        public string document_type;
        public string url;
        public string type_metadata;
        public double? lat;
        public double? lon;
        public string blob_ref;
    }

    [MenuItem("Window/Continuum/Continuum Library")]
    public static void ShowWindow()
    {
        var w = GetWindow<ContinuumLibraryWindow>("Continuum Library");
        w.minSize = new Vector2(520, 420);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Continuum Library", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Search by location (lat/lon or address) and distance. Uses Cave server when Base URL is set.", MessageType.Info);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Backend", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cave Base URL", GUILayout.Width(100));
        caveBaseUrl = EditorGUILayout.TextField(caveBaseUrl);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("DB Path (fallback)", GUILayout.Width(100));
        dbPath = EditorGUILayout.TextField(dbPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string p = EditorUtility.OpenFilePanel("Select continuum.db", string.IsNullOrEmpty(dbPath) ? Application.dataPath : Path.GetDirectoryName(dbPath), "db");
            if (!string.IsNullOrEmpty(p)) dbPath = p;
        }
        EditorGUILayout.EndHorizontal();

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
        distanceIndex = EditorGUILayout.Popup(distanceIndex, DistanceOptions);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Type", GUILayout.Width(60));
        documentTypeIndex = EditorGUILayout.Popup(documentTypeIndex, DocumentTypes);
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
            try
            {
                if (!string.IsNullOrEmpty(doc.type_metadata))
                {
                    var jo = Newtonsoft.Json.Linq.JObject.Parse(doc.type_metadata);
                    var title = jo["title"]?.ToString() ?? jo["author"]?.ToString();
                    if (!string.IsNullOrEmpty(title)) label = title + " (" + doc.document_type + ")";
                }
            }
            catch { }
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

    private void GeocodeAddress()
    {
        if (string.IsNullOrWhiteSpace(searchAddress)) { lastError = "Enter an address."; return; }
        if (string.IsNullOrWhiteSpace(caveBaseUrl)) { lastError = "Set Cave Base URL to use geocode."; return; }
        lastError = "";
        try
        {
            var uri = new Uri(new Uri(caveBaseUrl.TrimEnd('/')), "/api/geocode?address=" + Uri.EscapeDataString(searchAddress.Trim()));
            using (var client = new HttpClient())
            {
                var resp = client.GetAsync(uri).GetAwaiter().GetResult();
                var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode) { lastError = json; return; }
                var jo = Newtonsoft.Json.Linq.JObject.Parse(json);
                var latTok = jo["lat"];
                var lonTok = jo["lon"];
                if (latTok != null && latTok.Type != Newtonsoft.Json.Linq.JTokenType.Null)
                    searchLat = latTok.ToString();
                if (lonTok != null && lonTok.Type != Newtonsoft.Json.Linq.JTokenType.Null)
                    searchLon = lonTok.ToString();
            }
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
            var q = new List<string>();
            if (!string.IsNullOrWhiteSpace(searchQuery)) q.Add("q=" + Uri.EscapeDataString(searchQuery));
            if (documentTypeIndex > 0) q.Add("document_type=" + Uri.EscapeDataString(DocumentTypes[documentTypeIndex]));
            if (!string.IsNullOrWhiteSpace(searchLat)) q.Add("lat=" + Uri.EscapeDataString(searchLat));
            if (!string.IsNullOrWhiteSpace(searchLon)) q.Add("lon=" + Uri.EscapeDataString(searchLon));
            q.Add("distance_mi=" + (distanceIndex < DistanceMiles.Length && DistanceMiles[distanceIndex] >= 0
                ? (DistanceMiles[distanceIndex] == 0 ? "0" : DistanceMiles[distanceIndex].ToString("0"))
                : "infinite"));
            var uri = new Uri(new Uri(caveBaseUrl.TrimEnd('/')), "/api/library/search?" + string.Join("&", q));
            using (var client = new HttpClient())
            {
                var resp = client.GetAsync(uri).GetAwaiter().GetResult();
                var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode) { lastError = json; results.Clear(); return; }
                ParseSearchResults(json);
            }
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
            results.Clear();
        }
    }

    private void SearchViaPython()
    {
        string py = FindPython();
        if (string.IsNullOrEmpty(py)) { lastError = "Python not found."; return; }
        string scriptDir = Path.Combine(Application.dataPath, "..", "Scripts");
        var sb = new StringBuilder();
        sb.Append("-m unified_semantic_archiver.cli.query_db --db \"").Append(dbPath.Replace("\"", "\\\"")).Append("\" --table library_documents");
        if (!string.IsNullOrWhiteSpace(searchQuery)) sb.Append(" -q \"").Append(searchQuery.Replace("\"", "\\\"")).Append("\"");
        if (documentTypeIndex > 0) sb.Append(" --document_type ").Append(DocumentTypes[documentTypeIndex]);
        if (!string.IsNullOrWhiteSpace(searchLat)) sb.Append(" --lat ").Append(searchLat);
        if (!string.IsNullOrWhiteSpace(searchLon)) sb.Append(" --lon ").Append(searchLon);
        if (distanceIndex < DistanceMiles.Length)
        {
            sb.Append(" --distance_mi ");
            if (distanceIndex == 0) sb.Append("infinite");
            else if (DistanceMiles[distanceIndex] == 0) sb.Append("0");
            else sb.Append(DistanceMiles[distanceIndex].ToString("0"));
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
                ParseSearchResults(stdout);
            }
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
            results.Clear();
        }
    }

    private void ParseSearchResults(string json)
    {
        results.Clear();
        selectedIndex = -1;
        try
        {
            var arr = Newtonsoft.Json.Linq.JArray.Parse(json);
            foreach (var tok in arr)
            {
                var obj = tok as Newtonsoft.Json.Linq.JObject;
                if (obj == null) continue;
                var doc = new LibraryDocument
                {
                    id = obj["id"] != null ? (int)obj["id"] : 0,
                    document_type = obj["document_type"]?.ToString() ?? "",
                    url = obj["url"]?.ToString(),
                    type_metadata = obj["type_metadata"]?.ToString(),
                    blob_ref = obj["blob_ref"]?.ToString()
                };
                if (obj["lat"] != null && obj["lat"].Type != Newtonsoft.Json.Linq.JTokenType.Null && obj["lat"].Type != Newtonsoft.Json.Linq.JTokenType.Undefined)
                    doc.lat = (double)obj["lat"];
                if (obj["lon"] != null && obj["lon"].Type != Newtonsoft.Json.Linq.JTokenType.Null && obj["lon"].Type != Newtonsoft.Json.Linq.JTokenType.Undefined)
                    doc.lon = (double)obj["lon"];
                results.Add(doc);
            }
        }
        catch (Exception ex)
        {
            lastError = "Parse: " + ex.Message;
        }
    }

    private void DownloadDocument(LibraryDocument doc)
    {
        if (!string.IsNullOrEmpty(doc.url))
        {
            Application.OpenURL(doc.url);
            return;
        }
        if (string.IsNullOrWhiteSpace(caveBaseUrl)) { lastError = "Set Cave Base URL to download."; return; }
        var url = caveBaseUrl.TrimEnd('/') + "/api/library/documents/" + doc.id + "/download";
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
