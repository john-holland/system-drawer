#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;

/// <summary>
/// Polls for deeplink file written by continuum API (POST /api/deeplink) or system-drawer:// URL stub.
/// Parses window and episodeId; opens Continuum Explorer or Episodes window.
/// File path: ~/.continuum-deeplink.json or CONTINUUM_DEEPLINK_PATH.
/// </summary>
[InitializeOnLoad]
public static class DeepLinkHandler
{
    private const string DefaultPath = ".continuum-deeplink.json";
    private static double _lastPollTime;

    static DeepLinkHandler()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private static string GetDeeplinkPath()
    {
        var env = Environment.GetEnvironmentVariable("CONTINUUM_DEEPLINK_PATH");
        if (!string.IsNullOrEmpty(env))
            return env;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, DefaultPath);
    }

    private static void OnEditorUpdate()
    {
        if (EditorApplication.isPlaying)
            return;
        var now = EditorApplication.timeSinceStartup;
        if (now - _lastPollTime < 0.5)
            return;
        _lastPollTime = now;
        var path = GetDeeplinkPath();
        if (!File.Exists(path))
            return;
        try
        {
            var json = File.ReadAllText(path);
            File.Delete(path);
            HandleDeeplink(json);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[DeepLinkHandler] Failed to read/delete {path}: {ex.Message}");
        }
    }

    private static void HandleDeeplink(string json)
    {
        try
        {
            var window = "";
            var episodeId = "";
            if (json.Contains("\"window\""))
            {
                var start = json.IndexOf("\"window\"");
                var valStart = json.IndexOf(':', start) + 1;
                var valEnd = json.IndexOfAny(new[] { '"', ',', '}' }, valStart);
                if (valEnd > valStart)
                    window = json.Substring(valStart, valEnd - valStart).Trim('"', ' ');
            }
            if (json.Contains("\"episodeId\""))
            {
                var start = json.IndexOf("\"episodeId\"");
                var valStart = json.IndexOf(':', start) + 1;
                var valEnd = json.IndexOfAny(new[] { '"', ',', '}' }, valStart);
                if (valEnd > valStart)
                    episodeId = json.Substring(valStart, valEnd - valStart).Trim('"', ' ');
            }
            if (string.IsNullOrEmpty(window) && string.IsNullOrEmpty(episodeId))
                return;
            if (window.IndexOf("Explorer", StringComparison.OrdinalIgnoreCase) >= 0 || !string.IsNullOrEmpty(episodeId))
            {
                var dbPath = ContinuumSettings.GetDbPath();
                var py = ContinuumSettings.GetPythonPath();
                var tenant = ContinuumSettings.GetTenant();
                var sql = string.IsNullOrEmpty(episodeId)
                    ? "SELECT * FROM episodes LIMIT 100"
                    : $"SELECT * FROM work_orders WHERE episode_id = '{episodeId.Replace("'", "''")}' LIMIT 50";
                ContinuumExplorerWindow.ShowAndRunQuery(dbPath, py, tenant, sql);
            }
            else if (window.IndexOf("Episodes", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ContinuumEpisodesWindow.ShowWindow();
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[DeepLinkHandler] Parse error: {ex.Message}");
        }
    }
}
#endif
