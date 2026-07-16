#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;

/// <summary>
/// Polls for deeplink file written by continuuuum API (POST /api/deeplink) or system-drawer:// URL stub.
/// Parses window and episodeId; opens Continuuuum Explorer, Episodes, Lemma Properties, or Lemma Build.
/// File path: ~/.continuuuum-deeplink.json or CONTINUUUUM_DEEPLINK_PATH.
/// </summary>
[InitializeOnLoad]
public static class DeepLinkHandler
{
    private const string DefaultPath = ".continuuuum-deeplink.json";
    private static double _lastPollTime;

    static DeepLinkHandler()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private static string GetDeeplinkPath()
    {
        var env = Environment.GetEnvironmentVariable("CONTINUUUUM_DEEPLINK_PATH");
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

    /// <summary>Applies a deeplink JSON payload (also used by Edit Mode integration tests).</summary>
    public static void HandleDeeplink(string json)
    {
        try
        {
            var window = DeepLinkContract.ParseJsonString(json, "window");
            var episodeId = DeepLinkContract.ParseJsonString(json, "episodeId");
            var target = DeepLinkContract.ResolveTarget(window, episodeId);

            switch (target)
            {
                case DeepLinkContract.Target.Explorer:
                {
                    var dbPath = ContinuuuumSettings.GetDbPath();
                    var py = ContinuuuumSettings.GetPythonPath();
                    var tenant = ContinuuuumSettings.GetTenant();
                    var sql = string.IsNullOrEmpty(episodeId)
                        ? "SELECT * FROM episodes LIMIT 100"
                        : $"SELECT * FROM work_orders WHERE episode_id = '{episodeId.Replace("'", "''")}' LIMIT 50";
                    ContinuuuumExplorerWindow.ShowAndRunQuery(dbPath, py, tenant, sql);
                    break;
                }
                case DeepLinkContract.Target.Episodes:
                    ContinuuuumEpisodesWindow.ShowWindow();
                    break;
                case DeepLinkContract.Target.LemmaBuild:
                    OpenLemmaBuildWindow(DeepLinkContract.ExtractJsonObject(json, "form"));
                    break;
                case DeepLinkContract.Target.LemmaProperties:
                    OpenLemmaPropertiesWindow(DeepLinkContract.ParseJsonString(json, "entryId"));
                    break;
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[DeepLinkHandler] Parse error: {ex.Message}");
        }
    }

    // Continuuuum.Editor is a separate asmdef; resolve via reflection so Assembly-CSharp-Editor
    // still compiles when that assembly is rebuilding or missing a reference.
    static Type GetLemmaPropertyWindowType()
        => Type.GetType("VocabularyLemmaPropertyEditorWindow, Continuuuum.Editor");

    static void OpenLemmaPropertiesWindow(string entryId)
    {
        var windowType = GetLemmaPropertyWindowType();
        if (windowType == null)
        {
            UnityEngine.Debug.LogWarning("[DeepLinkHandler] Continuuuum.Editor assembly not found; cannot open Lemma Properties.");
            return;
        }

        var open = windowType.GetMethod("OpenWithEntryId", BindingFlags.Public | BindingFlags.Static);
        if (open == null)
        {
            UnityEngine.Debug.LogWarning("[DeepLinkHandler] OpenWithEntryId not found on VocabularyLemmaPropertyEditorWindow.");
            return;
        }

        open.Invoke(null, new object[] { entryId ?? "" });
    }

    static void OpenLemmaBuildWindow(string formJson)
    {
        var windowType = GetLemmaPropertyWindowType();
        if (windowType == null)
        {
            UnityEngine.Debug.LogWarning("[DeepLinkHandler] Continuuuum.Editor assembly not found; cannot open Lemma Build.");
            return;
        }

        var open = windowType.GetMethod("OpenOnLemmaBuildTabWithForm", BindingFlags.Public | BindingFlags.Static);
        if (open == null)
        {
            var fallback = windowType.GetMethod("OpenOnLemmaBuildTab", BindingFlags.Public | BindingFlags.Static);
            fallback?.Invoke(null, null);
            return;
        }

        open.Invoke(null, new object[] { formJson ?? "" });
    }
}
#endif
